using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrefabExporter.Editor
{
    internal static class PrefabExportWorkflow
    {
        internal const string Title = "Prefab Package Exporter";
        private const string PackageExtension = "unitypackage";
        private const string DefaultPackageName = "Prefabs";
        private const string ConfirmButton = "OK";

        public static bool ExportWithDialog(IEnumerable<GameObject> prefabs, out string error)
        {
            error = null;
            try
            {
                var candidates = prefabs.ToArray();
                var plan = PrefabExportPlan.Create(candidates);
                if (!CanExport(plan, out error))
                {
                    return false;
                }

                if (plan.PackageDependencies.Count > 0 && !EditorUtility.DisplayDialog(
                        Title,
                        "These Unity packages must be installed in the destination project:\n\n" +
                        string.Join("\n", plan.PackageDependencies), "Continue", "Cancel"))
                {
                    return false;
                }

                var defaultName = plan.PrefabPaths.Count == 1
                    ? Path.GetFileNameWithoutExtension(plan.PrefabPaths[0])
                    : DefaultPackageName;
                var path = EditorUtility.SaveFilePanel(Title, Path.GetDirectoryName(Application.dataPath),
                    defaultName, PackageExtension);
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                // Rebuild after the dialogs to catch assets changed since the preview.
                plan = PrefabExportPlan.Create(candidates);
                if (!CanExport(plan, out error))
                {
                    return false;
                }

                plan.Export(path);
                EditorUtility.DisplayDialog(Title, "Package exported:\n" + path, ConfirmButton);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogException(exception);
                return false;
            }
        }

        public static void ExportSelection()
        {
            ExportWithDialog(PrefabAssetSelection.GetSelectedPrefabs(), out var error);
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog(Title, error, ConfirmButton);
            }
        }

        private static bool CanExport(PrefabExportPlan plan, out string error)
        {
            error = plan.Warnings.Count > 0
                ? "Resolve missing scripts before exporting:\n\n" + string.Join("\n", plan.Warnings)
                : null;
            return error == null;
        }
    }
}
