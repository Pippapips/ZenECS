#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZenECS.EditorCommon
{
    public sealed class ZenSystemPickerWindow : EditorWindow
    {
        public static void Show(
            IEnumerable<Type> allSystemTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add System",
            Action? onCancel = null)
        {
            var w = CreateInstance<ZenSystemPickerWindow>();
            w._title = title;
            w._onPick = onPick;
            w._onCancel = onCancel;
            w._all = allSystemTypes
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();
            w._disabled = disabled ?? new HashSet<Type>();

            var screenRect = GUIUtility.GUIToScreenRect(activatorRectGui);
            var size = new Vector2(520, 420);
            w.position = new Rect(
                Mathf.Clamp(screenRect.x, 0, Screen.currentResolution.width - size.x),
                Mathf.Clamp(screenRect.yMax, 0, Screen.currentResolution.height - size.y),
                size.x, size.y);

            w.ShowAsDropDown(screenRect, size);
            w.Focus();
        }

        string _title = "Add System";
        Action<Type>? _onPick;
        Action? _onCancel;
        List<Type> _all = new();
        HashSet<Type> _disabled = new();

        string _search = "";
        Vector2 _scroll;
        int _hoverIndex = -1;
        bool _picked;

        void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                GUI.SetNextControlName("SB_SEARCH");
                var next = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                if (next != _search)
                {
                    _search = next;
                    _hoverIndex = -1;
                    Repaint();
                }
            }

            var list = Filtered(_all, _search).ToList();
            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    bool isDisabled = _disabled.Contains(t);
                    var r = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));

                    if (r.Contains(Event.current.mousePosition))
                    {
                        _hoverIndex = i;
                        if (Event.current.type == EventType.MouseMove) Repaint();
                    }

                    if (i == _hoverIndex)
                        EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.12f));

                    var ns = t.Namespace ?? "Global";
                    var name = t.Name;
                    var label = isDisabled
                        ? $"<color=#888888>{name}</color>  <color=#999999>({ns})</color>"
                        : $"<b>{name}</b>  <color=#888888>({ns})</color>";

                    var style = new GUIStyle(EditorStyles.label) { richText = true };
                    var content = new GUIContent(label, t.AssemblyQualifiedName);
                    EditorGUI.LabelField(r, content, style);

                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        if (!isDisabled)
                        {
                            _picked = true;
                            _onPick?.Invoke(t);
                            Close();
                            GUIUtility.ExitGUI();
                        }

                        Event.current.Use();
                    }
                }

                if (list.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("No results", EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            HandleKeyboard(list);
        }

        IEnumerable<Type> Filtered(IEnumerable<Type> src, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return src;
            keyword = keyword.Trim();
            return src.Where(t =>
                t.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (t.FullName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        void HandleKeyboard(List<Type> list)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.DownArrow)
            {
                _hoverIndex = Mathf.Clamp(_hoverIndex + 1, 0, Math.Max(0, list.Count - 1));
                e.Use();
                Repaint();
                return;
            }

            if (e.keyCode == KeyCode.UpArrow)
            {
                _hoverIndex = Mathf.Clamp(_hoverIndex - 1, 0, Math.Max(0, list.Count - 1));
                e.Use();
                Repaint();
                return;
            }

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (_hoverIndex >= 0 && _hoverIndex < list.Count)
                {
                    var t = list[_hoverIndex];
                    if (!_disabled.Contains(t))
                    {
                        _picked = true;
                        _onPick?.Invoke(t);
                        Close();
                    }

                    e.Use();
                }
            }
        }

        void OnEnable()
        {
            EditorApplication.delayCall += () =>
            {
                Focus();
                EditorGUI.FocusTextInControl("SB_SEARCH");
            };
        }

        void OnLostFocus() => Close();

        void OnDestroy()
        {
            if (!_picked) _onCancel?.Invoke();
        }
    }
}
#endif
