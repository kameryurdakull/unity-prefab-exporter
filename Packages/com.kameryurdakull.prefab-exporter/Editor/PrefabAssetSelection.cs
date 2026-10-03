using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrefabExporter.Editor
{
    internal static class PrefabAssetSelection
    {
        private const string AssetsPrefix = "Assets/";
        private const string PrefabExtension = ".prefab";

        public static bool IsSupported(GameObject candidate)
        {
            if (candidate == null || !EditorUtility.IsPersistent(candidate))
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(candidate);
            return path.StartsWith(AssetsPrefix, StringComparison.Ordinal) &&
                   string.Equals(Path.GetExtension(path), PrefabExtension, StringComparison.OrdinalIgnoreCase) &&
                   PrefabUtility.GetPrefabAssetType(candidate) != PrefabAssetType.NotAPrefab;
        }

        public static GameObject[] GetSelectedPrefabs()
        {
            return Selection.objects.OfType<GameObject>()
                .Where(IsSupported)
                .Select(candidate => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(candidate)))
                .Distinct()
                .ToArray();
        }
    }
}
