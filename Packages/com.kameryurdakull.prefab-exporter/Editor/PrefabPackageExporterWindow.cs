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
        private const string PrefabIcon = "Prefab Icon";
        private const string WindowTitle = "Prefab Exporter";
        private const float ContentPadding = 14f;
        private const float ActionWidth = 72f;
        [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();
        private PrefabExportPlan _plan;
        private GameObject _pendingPrefab;
        private Vector2 _scrollPosition;
        private string _error;
        private bool _showAssets = true;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _exportStyle;
        private bool _isDarkSkin;

        [MenuItem(WindowMenu)]
        private static void Open()
        {
            OpenWithPrefabs(PrefabAssetSelection.GetSelectedPrefabs());
        }

        public static void OpenWithPrefabs(IEnumerable<GameObject> prefabs)
        {
            var window = GetWindow<PrefabPackageExporterWindow>();
            window.titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent(PrefabIcon).image);
            window.minSize = new Vector2(560f, 480f);
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
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            Selection.selectionChanged -= Repaint;
        }

        private void OnProjectChanged()
        {
            _prefabs.RemoveAll(prefab => prefab == null);
            InvalidatePlan();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(ContentPadding);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(ContentPadding);
                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawInput();
                            GUILayout.Space(10f);
                            DrawCollection();
                            GUILayout.Space(10f);
                            DrawPlan();
                        }
                        GUILayout.Space(ContentPadding);
                    }
                    GUILayout.Space(ContentPadding);
                }
            }
            DrawFooter();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null && _isDarkSkin == EditorGUIUtility.isProSkin)
            {
                return;
            }

            _isDarkSkin = EditorGUIUtility.isProSkin;
            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 21, fixedHeight = 30f };
            _subtitleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                fontSize = 12,
                normal = { textColor = _isDarkSkin ? new Color(0.7f, 0.73f, 0.78f) : new Color(0.35f, 0.38f, 0.42f) }
            };
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };
            _exportStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        }

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0f, 94f, GUILayout.ExpandWidth(true));
            var background = _isDarkSkin ? new Color(0.15f, 0.17f, 0.2f) : new Color(0.87f, 0.9f, 0.93f);
            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(0.25f, 0.65f, 0.9f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 18f, 32f, 32f), EditorGUIUtility.IconContent(PrefabIcon));
            GUI.Label(new Rect(rect.x + 60f, rect.y + 12f, rect.width - 78f, 32f), WindowTitle, _titleStyle);
            GUI.Label(new Rect(rect.x + 60f, rect.y + 49f, rect.width - 78f, 32f),
                "Collect prefabs. Review dependencies. Export one package.", _subtitleStyle);
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                var status = _prefabs.Count == 0 ? "Add a prefab to get started."
                    : _plan == null ? $"{_prefabs.Count} prefabs selected · Dependencies checked on export"
                    : $"{_plan.PrefabPaths.Count} prefabs · {_plan.AssetPaths.Count} assets · {_plan.PackageDependencies.Count} package dependencies";
                EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
                using (new EditorGUI.DisabledScope(_prefabs.Count == 0 || (_plan != null && _plan.Warnings.Count > 0)))
                {
                    var previousColor = GUI.backgroundColor;
                    try
                    {
                        GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
                        if (GUILayout.Button("Export .unitypackage", _exportStyle, GUILayout.Height(36f)))
                        {
                            PrefabExportWorkflow.ExportWithDialog(_prefabs, out _error);
                            _plan = null;
                        }
                    }
                    finally
                    {
                        GUI.backgroundColor = previousColor;
                    }
                }
            }
        }

        private void DrawInput()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField("01  /  Add prefabs", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Choose an asset or collect the current Project selection.", _subtitleStyle);
                EditorGUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _pendingPrefab = (GameObject)EditorGUILayout.ObjectField("Add prefab", _pendingPrefab, typeof(GameObject), false);
                    using (new EditorGUI.DisabledScope(_pendingPrefab == null))
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(ActionWidth)) && AddPrefab(_pendingPrefab))
                        {
                            _pendingPrefab = null;
                        }
                    }
                }
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
        }

        private void DrawCollection()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"02  /  Collection ({_prefabs.Count})", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(ActionWidth)))
                        {
                            _prefabs.Clear();
                            InvalidatePlan();
                        }
                    }
                }
                if (_prefabs.Count == 0)
                {
                    EditorGUILayout.Space(12f);
                    EditorGUILayout.LabelField("Your collection is empty", EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.LabelField("Add prefabs from any Assets folder. Duplicate entries are ignored.", _subtitleStyle);
                    EditorGUILayout.Space(12f);
                }
                for (var index = 0; index < _prefabs.Count; index++)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var prefab = _prefabs[index];
                        var remove = DrawPrefabRow(prefab);
                        if (remove)
                        {
                            _prefabs.RemoveAt(index);
                            InvalidatePlan();
                            index--;
                        }
                    }
                }
            }
        }

        private static bool DrawPrefabRow(GameObject prefab)
        {
            var remove = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                remove = GUILayout.Button("Remove", GUILayout.Width(ActionWidth));
            }
            var path = AssetDatabase.GetAssetPath(prefab);
            EditorGUILayout.LabelField(new GUIContent(path, path), EditorStyles.miniLabel);
            return remove;
        }

        private void DrawPlan()
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField("03  /  Package preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Review the assets and required Unity packages before exporting.", _subtitleStyle);
                EditorGUILayout.Space(8f);
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
                    EditorGUILayout.LabelField("Analyze your collection to preview package contents.", _subtitleStyle);
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
