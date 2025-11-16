#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZenECS.EditorCommon
{
    /// <summary>
    /// Dropdown picker window for selecting a SystemsPreset ScriptableObject.
    /// - Looks and behaves similar to ZenSystemPickerWindow / ZenBlueprintPickerWindow.
    /// - Searches all assets of type "SystemsPreset" (by name).
    /// - Returns the picked preset via the provided callback.
    /// </summary>
    public sealed class ZenSystemPresetPickerWindow : EditorWindow
    {
        const float ROW_HEIGHT = 22f;
        const float PADDING = 6f;

        string _title = "Add System Preset";
        Action<ScriptableObject>? _onPick;
        List<ScriptableObject> _all = new();
        string _search = "";
        Vector2 _scroll;
        int _hover = -1;

        GUIStyle? _rowStyle;
        GUIStyle? _searchStyle;

        /// <summary>
        /// Shows the preset picker as a dropdown near the given activator rect.
        /// </summary>
        /// <param name="activatorRectGui">Rect of the button/control in GUI space.</param>
        /// <param name="onPick">Callback when a preset is chosen.</param>
        /// <param name="title">Window title.</param>
        public static void Show(
            Rect activatorRectGui,
            Action<ScriptableObject> onPick,
            string title = "Add System Preset")
        {
            var w = CreateInstance<ZenSystemPresetPickerWindow>();
            w._title = title;
            w._onPick = onPick;
            w._all = LoadAllPresets();

            var width = 520f;
            var height = 400f;

            var topLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

            var anchorRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f,
                activatorRectGui.width,
                activatorRectGui.height
            );

            w.ShowAsDropDown(anchorRect, new Vector2(width, height));
            w.titleContent = new GUIContent(title);
            w.Focus();
        }

        /// <summary>
        /// Finds all ScriptableObject assets of type "SystemsPreset".
        /// (Matches by type name string, so it doesn't require a hard reference to the C# type.)
        /// </summary>
        static List<ScriptableObject> LoadAllPresets()
        {
            var res = new List<ScriptableObject>(32);

            // Only assets whose type name is "SystemsPreset".
            foreach (var guid in AssetDatabase.FindAssets("t:SystemsPreset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so != null)
                    res.Add(so);
            }

            return res.OrderBy(a => a.name).ToList();
        }

        void OnEnable()
        {
            var baseLabel = EditorStyles.label;
            _rowStyle = new GUIStyle(baseLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true,
                padding = new RectOffset(4, 4, 0, 0)
            };

            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            wantsMouseMove = true;
        }

        void OnGUI()
        {
            DrawSearchBar();
            EditorGUILayout.Space(2);

            var list = Filtered().ToList();

            if (list.Count == 0)
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    var msgStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };

                    EditorGUILayout.LabelField(
                        "No SystemsPreset assets found.\n" +
                        "Create a SystemsPreset ScriptableObject first.",
                        msgStyle,
                        GUILayout.MaxWidth(400));

                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

            var viewRect = GUILayoutUtility.GetRect(
                0, 100000,
                0, 100000,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            var contentRect = new Rect(
                0, 0,
                viewRect.width - 16,
                list.Count * ROW_HEIGHT + PADDING * 2);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

            var y = PADDING;
            _hover = Mathf.Clamp(_hover, -1, list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                var asset = list[i];
                var r = new Rect(PADDING, y, contentRect.width - PADDING * 2, ROW_HEIGHT);

                if (r.Contains(Event.current.mousePosition))
                    _hover = i;

                if (_hover == i)
                {
                    var bg = EditorGUIUtility.isProSkin
                        ? new Color(1, 1, 1, 0.06f)
                        : new Color(0, 0, 0, 0.06f);
                    EditorGUI.DrawRect(r, bg);
                }

                var path = AssetDatabase.GetAssetPath(asset);
                var displayText =
                    $"<b>{asset.name}</b>   <size=9><color=#888888>[{path}]</color></size>";
                var content = new GUIContent(displayText, path);

                if (GUI.Button(r, content, _rowStyle!))
                {
                    _onPick?.Invoke(asset);
                    Close();
                }

                y += ROW_HEIGHT;
            }

            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseMove)
                Repaint();

            HandleKeyboard(list);

            // Bottom tooltip / status bar
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var tipStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };

                string tipText = GUI.tooltip ?? string.Empty;
                var tipContent = new GUIContent(tipText);

                var rect = GUILayoutUtility.GetRect(
                    tipContent,
                    tipStyle,
                    GUILayout.ExpandWidth(true));

                EditorGUI.LabelField(rect, tipContent, tipStyle);
            }
        }

        void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                GUI.SetNextControlName("ZenSystemPresetPickerSearch");
                _search = GUILayout.TextField(_search, _searchStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    _search = "";
                    GUI.FocusControl("ZenSystemPresetPickerSearch");
                }
            }
        }

        IEnumerable<ScriptableObject> Filtered()
        {
            IEnumerable<ScriptableObject> src = _all;
            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.Trim();
                src = src.Where(a =>
                {
                    if (a == null) return false;
                    var name = a.name ?? "";
                    var path = AssetDatabase.GetAssetPath(a) ?? "";
                    return name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                           || path.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
                });
            }

            return src.OrderBy(a => a.name);
        }

        void HandleKeyboard(List<ScriptableObject> list)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    _hover = Mathf.Clamp((_hover < 0 ? list.Count : _hover) - 1, 0,
                        Mathf.Max(0, list.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.DownArrow:
                    _hover = Mathf.Clamp(_hover + 1, 0, Mathf.Max(0, list.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_hover >= 0 && _hover < list.Count)
                    {
                        var asset = list[_hover];
                        _onPick?.Invoke(asset);
                        Close();
                    }
                    e.Use();
                    break;

                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }

        void OnLostFocus()
        {
            // Keep dropdown-ish UX: close when focus is lost.
            Close();
        }
    }
}
#endif
