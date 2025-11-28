#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.EditorCommon
{
    /// <summary>
    /// Picker window for IWorldSingletonComponent structs.
    /// </summary>
    public sealed class ZenSingletonPickerWindow : EditorWindow
    {
        private const float ROW_HEIGHT = 22f;
        private const float PADDING = 6f;

        private string _search = "";
        private Vector2 _scroll;
        private List<Type> _all = new();
        private HashSet<Type> _disabled = new();
        private Action<Type>? _onPick;
        private Action? _onCancel;

        private int _hover = -1;
        private GUIStyle? _rowStyle;
        private GUIStyle? _searchStyle;
        private string _title = "Add Singleton";

        private bool _picked;

        public static void Show(
            IEnumerable<Type> allSingletonTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add Singleton",
            Action? onCancel = null)
        {
            var win = CreateInstance<ZenSingletonPickerWindow>();
            win.titleContent = new GUIContent(title);
            win._title = title;
            win._onPick = onPick;
            win._onCancel = onCancel;

            // Only non-abstract structs implementing IWorldSingletonComponent
            win._all = allSingletonTypes
                .Where(t =>
                    t != null &&
                    !t.IsAbstract &&
                    t.IsValueType &&
                    typeof(IWorldSingletonComponent).IsAssignableFrom(t))
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            win._disabled = disabled ?? new HashSet<Type>();

            // Position similar to ZenSystemPickerWindow / Blueprint picker
            var width = 520f;
            var height = 420f;

            var topLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

            var anchorRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f, // a bit below the button
                activatorRectGui.width,
                activatorRectGui.height
            );

            win.ShowAsDropDown(anchorRect, new Vector2(width, height));
            win.Focus();
        }

        private void OnEnable()
        {
            // Row style
            var baseLabel = EditorStyles.label;
            _rowStyle = new GUIStyle(baseLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true,
                padding = new RectOffset(4, 4, 0, 0)
            };

            // Search field style
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);

            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            DrawSearchBar();
            EditorGUILayout.Space(2);

            var filtered = Filtered().ToList();

            if (filtered.Count == 0)
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
                        "No matching singleton components found.\n" +
                        "Check search text or implement IWorldSingletonComponent structs.",
                        msgStyle,
                        GUILayout.MaxWidth(400));

                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

            var evt = Event.current;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                for (int i = 0; i < filtered.Count; i++)
                {
                    var t = filtered[i];
                    bool isDisabled = _disabled.Contains(t);

                    var rowRect = GUILayoutUtility.GetRect(
                        0, ROW_HEIGHT,
                        GUILayout.ExpandWidth(true));

                    if (evt.type == EventType.Repaint)
                    {
                        if (rowRect.Contains(evt.mousePosition))
                            _hover = i;

                        // Hover background
                        if (_hover == i && !isDisabled)
                        {
                            EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.38f, 0.56f, 0.35f));
                        }

                        var rowStyle = _rowStyle ?? EditorStyles.label;

                        string typeName = t.Name;
                        string ns = t.Namespace ?? "";
                        string labelText;

                        if (isDisabled)
                        {
                            // grey + info: already added
                            labelText =
                                $"<color=#888888><b>{typeName}</b></color>  " +
                                $"<color=#999999>({ns}) — already added</color>";
                        }
                        else
                        {
                            labelText =
                                $"<b>{typeName}</b>  <color=#999999>({ns})</color>";
                        }

                        using (new EditorGUI.DisabledScope(isDisabled))
                        {
                            rowStyle.Draw(rowRect, new GUIContent(labelText), false, false, false, false);
                        }
                    }

                    // Click handling (only when not disabled)
                    if (!isDisabled && evt.type == EventType.MouseDown && evt.button == 0 && rowRect.Contains(evt.mousePosition))
                    {
                        _onPick?.Invoke(t);
                        _picked = true;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            HandleKeyboard(filtered);
        }

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_title, EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                GUI.SetNextControlName("SingletonSearchField");
                _search = EditorGUILayout.TextField(_search, _searchStyle ?? EditorStyles.toolbarSearchField,
                    GUILayout.MinWidth(120));

                if (Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.Escape)
                {
                    _search = "";
                    Repaint();
                }
            }

            // Auto-focus search field
            if (Event.current.type == EventType.Repaint)
            {
                if (GUI.GetNameOfFocusedControl() == "")
                {
                    GUI.FocusControl("SingletonSearchField");
                }
            }
        }

        private IEnumerable<Type> Filtered()
        {
            if (string.IsNullOrEmpty(_search))
                return _all;

            string lower = _search.ToLowerInvariant();
            return _all.Where(t =>
                (t.Name?.ToLowerInvariant().Contains(lower) ?? false) ||
                (t.FullName?.ToLowerInvariant().Contains(lower) ?? false));
        }

        private void HandleKeyboard(IReadOnlyList<Type> filtered)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown) return;

            if (evt.keyCode == KeyCode.Escape)
            {
                _onCancel?.Invoke();
                Close();
                GUIUtility.ExitGUI();
                return;
            }

            if (filtered.Count == 0) return;

            if (evt.keyCode == KeyCode.UpArrow)
            {
                _hover = Mathf.Clamp(_hover - 1, 0, filtered.Count - 1);
                evt.Use();
                Repaint();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                _hover = Mathf.Clamp(_hover + 1, 0, filtered.Count - 1);
                evt.Use();
                Repaint();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_hover >= 0 && _hover < filtered.Count)
                {
                    var t = filtered[_hover];
                    if (!_disabled.Contains(t))
                    {
                        _onPick?.Invoke(t);
                        _picked = true;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (!_picked)
            {
                _onCancel?.Invoke();
            }
        }
    }
}
#endif
