using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParticleExporter.Editor
{
    internal sealed class ParticleExportPlan
    {
        private const string AssetsPrefix = "Assets/";
        private const string PackagesPrefix = "Packages/";
        private const string PrefabExtension = ".prefab";
        private const string ScriptExtension = ".cs";
        private const string PackageExtension = ".unitypackage";
        private const string AssemblyDefinitionExtension = ".asmdef";
        private const string AssemblyReferenceExtension = ".asmref";

        public IReadOnlyList<string> PrefabPaths { get; }
        public IReadOnlyList<string> AssetPaths { get; }
        public IReadOnlyList<string> PackageDependencies { get; }
        public IReadOnlyList<string> Warnings { get; }

        private ParticleExportPlan(
            IReadOnlyList<string> prefabPaths,
            IReadOnlyList<string> assetPaths,
            IReadOnlyList<string> packageDependencies,
            IReadOnlyList<string> warnings)
        {
            PrefabPaths = prefabPaths;
            AssetPaths = assetPaths;
            PackageDependencies = packageDependencies;
            Warnings = warnings;
        }

        public static ParticleExportPlan Create(IEnumerable<GameObject> prefabs)
        {
            if (prefabs == null)
            {
                throw new ArgumentNullException(nameof(prefabs));
            }

            var prefabPaths = prefabs
                .Where(prefab => prefab != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (prefabPaths.Length == 0)
            {
                throw new InvalidOperationException("Select at least one particle prefab asset.");
            }

            var warnings = new List<string>();
            foreach (var path in prefabPaths)
            {
                ValidatePrefab(path, warnings);
            }

            var dependencies = AssetDatabase.GetDependencies(prefabPaths, true);
            var assetPaths = new HashSet<string>(StringComparer.Ordinal);
            var packages = new HashSet<string>(StringComparer.Ordinal);

            foreach (var path in dependencies)
            {
                if (path.StartsWith(AssetsPrefix, StringComparison.Ordinal) &&
                    !AssetDatabase.IsValidFolder(path))
                {
                    assetPaths.Add(path);
                }
                else if (path.StartsWith(PackagesPrefix, StringComparison.Ordinal))
                {
                    var packageName = path.Substring(PackagesPrefix.Length).Split('/')[0];
                    packages.Add(packageName);
                }
            }

            foreach (var path in prefabPaths)
            {
                assetPaths.Add(path);
            }

            AddScriptAssemblyFiles(assetPaths);

            return new ParticleExportPlan(
                prefabPaths,
                assetPaths.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                packages.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                warnings);
        }

        public void Export(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentException("Choose a package file path.", nameof(packagePath));
            }

            if (!string.Equals(Path.GetExtension(packagePath), PackageExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The output must be a .unitypackage file.", nameof(packagePath));
            }

            // The plan already contains the complete dependency closure. IncludeDependencies
            // would let Unity add assets that were not shown in the preview.
            AssetDatabase.ExportPackage(AssetPaths.ToArray(), packagePath, ExportPackageOptions.Default);
        }

        private static void ValidatePrefab(string path, ICollection<string> warnings)
        {
            if (!path.StartsWith(AssetsPrefix, StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(path), PrefabExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Only prefab assets under Assets are supported: {path}");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                throw new InvalidOperationException($"The prefab could not be loaded: {path}");
            }

            if (prefab.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                throw new InvalidOperationException($"The prefab has no ParticleSystem: {path}");
            }

            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (transform.GetComponents<Component>().Any(component => component == null))
                {
                    warnings.Add($"Missing script on {path} / {transform.name}");
                }
            }
        }

        private static void AddScriptAssemblyFiles(ISet<string> assetPaths)
        {
            var scriptPaths = assetPaths
                .Where(path => string.Equals(Path.GetExtension(path), ScriptExtension, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var scriptPath in scriptPaths)
            {
                var folder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
                for (; !string.IsNullOrEmpty(folder) &&
                       (folder == "Assets" || folder.StartsWith(AssetsPrefix, StringComparison.Ordinal));
                     folder = Path.GetDirectoryName(folder)?.Replace('\\', '/'))
                {
                    var assemblyFiles = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { folder })
                        .Concat(AssetDatabase.FindAssets("t:AssemblyDefinitionReferenceAsset", new[] { folder }))
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(path => string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), folder, StringComparison.Ordinal))
                        .Where(path => path.EndsWith(AssemblyDefinitionExtension, StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(AssemblyReferenceExtension, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (assemblyFiles.Length > 0)
                    {
                        foreach (var assemblyFile in assemblyFiles)
                        {
                            assetPaths.Add(assemblyFile);
                        }

                        break;
                    }
                }
            }
        }
    }
}
