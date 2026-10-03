using UnityEditor;

namespace PrefabExporter.Editor
{
    internal static class PrefabExporterContextMenu
    {
        private const string ExportMenu = "Assets/Prefab Exporter/Export Selected Prefabs...";
        private const string AddMenu = "Assets/Prefab Exporter/Add to Export Window";
        private const int MenuPriority = 2000;

        [MenuItem(ExportMenu, false, MenuPriority)]
        private static void ExportSelected()
        {
            PrefabExportWorkflow.ExportSelection();
        }

        [MenuItem(AddMenu, false, MenuPriority + 1)]
        private static void AddSelected()
        {
            PrefabPackageExporterWindow.OpenWithPrefabs(PrefabAssetSelection.GetSelectedPrefabs());
        }

        [MenuItem(ExportMenu, true)]
        [MenuItem(AddMenu, true)]
        private static bool ValidateSelection()
        {
            return PrefabAssetSelection.GetSelectedPrefabs().Length > 0;
        }
    }
}
