using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace PrefabExporter.Editor
{
    [MovedFrom(true, "ParticleExporter.Editor", null, "ParticlePackageExporterWindow")]
    internal sealed class PrefabPackageExporterWindow : EditorWindow
    {
        private const string WindowMenu = "Tools/Prefab Package Exporter";
        private const string InvalidPrefabMessage = "Choose a prefab asset under Assets from the Project window.";
        private const float ActionWidth = 72f;
        [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();
        private PrefabExportPlan _plan;
        private GameObject _pendingPrefab;
        private Vector2 _scrollPosition;
        private string _error;
        private bool _showAssets = true;

        [MenuItem(WindowMenu)]
        private static void Open()
        {
            OpenWithPrefabs(PrefabAssetSelection.GetSelectedPrefabs());
        }

        public static void OpenWithPrefabs(IEnumerable<GameObject> prefabs)
        {
            var window = GetWindow<PrefabPackageExporterWindow>();
            window.titleContent = new GUIContent("Prefab Exporter");
            window.minSize = new Vector2(520f, 360f);
            foreach (var prefab in prefabs)
            {
                window.AddPrefab(prefab);
            }
            window.Show();
            window.Focus();
            window.Repaint();
        }

        private void OnEnable()
        {
            _prefabs.RemoveAll(prefab => prefab == null);
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            _prefabs.RemoveAll(prefab => prefab == null);
            InvalidatePlan();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(PrefabExportWorkflow.Title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Collect prefab assets from any folder. Use Project > right click > Prefab Exporter " +
                                    "to export or add multiple prefabs with their referenced assets.", MessageType.Info);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawInput();
            DrawCollection();
            DrawPlan();
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
            {
                if (GUILayout.Button("Export .unitypackage", GUILayout.Height(28f)))
                {
                    PrefabExportWorkflow.ExportWithDialog(_prefabs, out _error);
                    _plan = null;
                }
            }
        }

        private void DrawInput()
        {
            EditorGUILayout.BeginHorizontal();
            _pendingPrefab = (GameObject)EditorGUILayout.ObjectField("Add prefab", _pendingPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(_pendingPrefab == null))
            {
                if (GUILayout.Button("Add", GUILayout.Width(ActionWidth)) && AddPrefab(_pendingPrefab))
                {
                    _pendingPrefab = null;
                }
            }
            EditorGUILayout.EndHorizontal();
            var selection = PrefabAssetSelection.GetSelectedPrefabs();
            using (new EditorGUI.DisabledScope(selection.Length == 0))
            {
                if (GUILayout.Button("Add selected prefabs"))
                {
                    foreach (var prefab in selection)
                    {
                        AddPrefab(prefab);
                    }
                }
            }
        }

        private void DrawCollection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Collected prefabs ({_prefabs.Count})", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(ActionWidth)))
                {
                    _prefabs.Clear();
                    InvalidatePlan();
                }
            }
            EditorGUILayout.EndHorizontal();
            for (var index = 0; index < _prefabs.Count; index++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(_prefabs[index], typeof(GameObject), false);
                if (GUILayout.Button("Remove", GUILayout.Width(ActionWidth)))
                {
                    _prefabs.RemoveAt(index);
                    InvalidatePlan();
                    index--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPlan()
        {
            using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
            {
                if (GUILayout.Button("Analyze dependencies"))
                {
                    Analyze();
                }
            }
            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }
            if (_plan == null)
            {
                return;
            }
            EditorGUILayout.LabelField($"Package contents: {_plan.AssetPaths.Count} assets", EditorStyles.boldLabel);
            if (_plan.PackageDependencies.Count > 0)
            {
                EditorGUILayout.HelpBox("Install these Unity packages in the destination project: " +
                                        string.Join(", ", _plan.PackageDependencies), MessageType.Warning);
            }
            foreach (var warning in _plan.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
            _showAssets = EditorGUILayout.Foldout(_showAssets, "Included asset paths", true);
            if (_showAssets)
            {
                foreach (var path in _plan.AssetPaths)
                {
                    EditorGUILayout.SelectableLabel(path, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }

        private bool AddPrefab(GameObject candidate)
        {
            if (!PrefabAssetSelection.IsSupported(candidate))
            {
                _error = InvalidPrefabMessage;
                return false;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(candidate));
            _error = null;
            if (!_prefabs.Contains(prefab))
            {
                _prefabs.Add(prefab);
                InvalidatePlan();
            }
            return true;
        }

        private void InvalidatePlan()
        {
            _plan = null;
            _error = null;
        }

        private void Analyze()
        {
            try
            {
                _plan = PrefabExportPlan.Create(_prefabs);
                _error = null;
            }
            catch (Exception exception)
            {
                _plan = null;
                _error = exception.Message;
            }
        }
    }
}
