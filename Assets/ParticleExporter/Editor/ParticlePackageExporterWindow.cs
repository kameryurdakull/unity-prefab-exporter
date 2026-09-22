using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParticleExporter.Editor
{
    internal sealed class ParticlePackageExporterWindow : EditorWindow
    {
        private const string PrefabExtension = ".prefab";

        private readonly List<GameObject> _prefabs = new List<GameObject>();
        private ParticleExportPlan _plan;
        private GameObject _pendingPrefab;
        private Vector2 _scrollPosition;
        private string _error;
        private bool _showAssets = true;

        [MenuItem("Tools/Particle Package Exporter")]
        private static void Open()
        {
            var window = GetWindow<ParticlePackageExporterWindow>();
            window.titleContent = new GUIContent("Particle Exporter");
            window.minSize = new Vector2(520f, 360f);
            window.AddSelection();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Particle Package Exporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose prefab assets containing ParticleSystem components. The package contains those prefabs and their referenced project assets only.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            _pendingPrefab = (GameObject)EditorGUILayout.ObjectField("Add prefab", _pendingPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(_pendingPrefab == null))
            {
                if (GUILayout.Button("Add", GUILayout.Width(72f)))
                {
                    AddPrefab(_pendingPrefab);
                    _pendingPrefab = null;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add selected prefabs"))
            {
                AddSelection();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Selected prefabs ({_prefabs.Count})", EditorStyles.boldLabel);
            for (var index = 0; index < _prefabs.Count; index++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(_prefabs[index], typeof(GameObject), false);
                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                {
                    _prefabs.RemoveAt(index);
                    InvalidatePlan();
                    index--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6f);
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
                EditorGUILayout.HelpBox(
                    "These Unity packages are referenced but cannot be embedded in a .unitypackage. Install them in the destination project: " +
                    string.Join(", ", _plan.PackageDependencies),
                    MessageType.Warning);
            }

            foreach (var warning in _plan.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            _showAssets = EditorGUILayout.Foldout(_showAssets, "Included asset paths", true);
            if (_showAssets)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(80f));
                foreach (var path in _plan.AssetPaths)
                {
                    EditorGUILayout.SelectableLabel(path, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }

                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(_plan.Warnings.Count > 0))
            {
                if (GUILayout.Button("Export .unitypackage", GUILayout.Height(28f)))
                {
                    Export();
                }
            }
        }

        private void AddSelection()
        {
            foreach (var selected in Selection.objects.OfType<GameObject>())
            {
                AddPrefab(selected);
            }
        }

        private void AddPrefab(GameObject candidate)
        {
            var path = AssetDatabase.GetAssetPath(candidate);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(PrefabExtension, StringComparison.OrdinalIgnoreCase))
            {
                _error = "Select a prefab asset from the Project window.";
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                _error = "The selected prefab has no ParticleSystem component.";
                return;
            }

            if (_prefabs.Any(existing => AssetDatabase.GetAssetPath(existing) == path))
            {
                return;
            }

            _prefabs.Add(prefab);
            InvalidatePlan();
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
                _plan = ParticleExportPlan.Create(_prefabs);
                _error = null;
            }
            catch (Exception exception)
            {
                _plan = null;
                _error = exception.Message;
            }
        }

        private void Export()
        {
            var defaultName = _plan.PrefabPaths.Count == 1
                ? Path.GetFileNameWithoutExtension(_plan.PrefabPaths[0])
                : "ParticlePrefabs";
            var path = EditorUtility.SaveFilePanel(
                "Export particle package",
                Path.GetDirectoryName(Application.dataPath),
                defaultName,
                "unitypackage");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                // Rebuild immediately before export so changes since the preview cannot be missed.
                _plan = ParticleExportPlan.Create(_prefabs);
                if (_plan.Warnings.Count > 0)
                {
                    _error = "Resolve missing scripts before exporting.";
                    return;
                }

                _plan.Export(path);
                _error = null;
                EditorUtility.DisplayDialog("Particle package exported", path, "OK");
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception);
            }
        }
    }
}
