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
        private string _title = "Add System";

        private bool _picked;

        public static void Show(
            IEnumerable<Type> allSystemTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add System",
            Action? onCancel = null)
        {
            var win = CreateInstance<ZenSystemPickerWindow>();
            win.titleContent = new GUIContent(title);
            win._title = title;
            win._onPick = onPick;
            win._onCancel = onCancel;

            win._all = allSystemTypes
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            win._disabled = disabled ?? new HashSet<Type>();

            // Blueprint Picker와 유사한 위치 계산
            var width = 520f;
            var height = 420f;

            var topLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

            var anchorRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f, // 버튼 아래로 약간 띄우기
                activatorRectGui.width,
                activatorRectGui.height
            );

            win.ShowAsDropDown(anchorRect, new Vector2(width, height));
            win.Focus();
        }

        private void OnEnable()
        {
            // Row 스타일: Blueprint Picker와 동일 컨셉
            var baseLabel = EditorStyles.label;
            _rowStyle = new GUIStyle(baseLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true,
                padding = new RectOffset(4, 4, 0, 0)
            };

            // 검색창 스타일
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
                        "No matching systems found.\n" +
                        "Check search text or implement ISystem types.",
                        msgStyle,
                        GUILayout.MaxWidth(400));

                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

            // 리스트 영역 & 스크롤
            var viewRect = GUILayoutUtility.GetRect(
                0, 100000,
                0, 100000,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            var contentRect = new Rect(
                0, 0,
                viewRect.width - 16,
                filtered.Count * ROW_HEIGHT + PADDING * 2);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

            var y = PADDING;
            _hover = Mathf.Clamp(_hover, -1, filtered.Count - 1);

            for (int i = 0; i < filtered.Count; i++)
            {
                var t = filtered[i];
                var r = new Rect(PADDING, y, contentRect.width - PADDING * 2, ROW_HEIGHT);

                // Hover 처리
                if (r.Contains(Event.current.mousePosition))
                {
                    _hover = i;
                }

                if (_hover == i)
                {
                    var bg = EditorGUIUtility.isProSkin
                        ? new Color(1, 1, 1, 0.06f)
                        : new Color(0, 0, 0, 0.06f);
                    EditorGUI.DrawRect(r, bg);
                }

                bool isDisabled = _disabled.Contains(t);
                var ns = t.Namespace ?? "Global";
                var typeName = t.Name;
                var fullName = t.FullName ?? typeName;

                // 화면에 보여줄 텍스트
                // SystemName   <size=9><color=#888888>Namespace / FullName</color></size>
                string secondary = $"{ns}";
                const int maxLen = 70;
                if (fullName.Length > maxLen)
                    secondary = "…" + fullName.Substring(fullName.Length - maxLen);

                string displayText = isDisabled
                    ? $"<color=#888888>{typeName}</color>   <size=9><color=#999999>{secondary}</color></size>"
                    : $"<b>{typeName}</b>   <size=9><color=#888888>{secondary}</color></size>";

                var content = new GUIContent(displayText, fullName);

                if (isDisabled)
                {
                    // Disabled는 클릭 불가 라벨
                    GUI.Label(r, content, _rowStyle!);
                }
                else
                {
                    if (GUI.Button(r, content, _rowStyle!))
                    {
                        _picked = true;
                        _onPick?.Invoke(t);
                        Close();
                    }
                }

                y += ROW_HEIGHT;
            }

            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseMove)
                Repaint();

            HandleKeyboard(filtered);

            // --- Tooltip / status bar ---
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

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                GUI.SetNextControlName("ZenSystemPickerSearch");
                _search = GUILayout.TextField(_search, _searchStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    _search = "";
                    GUI.FocusControl("ZenSystemPickerSearch");
                }
            }
        }

        private IEnumerable<Type> Filtered()
        {
            IEnumerable<Type> src = _all;
            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.Trim();
                src = src.Where(t =>
                {
                    var name = t.Name ?? "";
                    var ns = t.Namespace ?? "";
                    var full = t.FullName ?? "";
                    return (!string.IsNullOrEmpty(name) &&
                            name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                           || (!string.IsNullOrEmpty(ns) &&
                               ns.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                           || (!string.IsNullOrEmpty(full) &&
                               full.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
                });
            }

            return src.OrderBy(t => t.FullName);
        }

        private void HandleKeyboard(List<Type> filtered)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    _hover = Mathf.Clamp((_hover < 0 ? filtered.Count : _hover) - 1, 0,
                        Mathf.Max(0, filtered.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.DownArrow:
                    _hover = Mathf.Clamp(_hover + 1, 0, Mathf.Max(0, filtered.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_hover >= 0 && _hover < filtered.Count)
                    {
                        var t = filtered[_hover];
                        if (!_disabled.Contains(t))
                        {
                            _picked = true;
                            _onPick?.Invoke(t);
                            Close();
                        }
                        e.Use();
                    }
                    break;

                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }

        private void OnLostFocus()
        {
            // 드롭다운 스타일 유지 위해 포커스 잃으면 닫기
            Close();
        }

        private void OnDestroy()
        {
            if (!_picked)
                _onCancel?.Invoke();
        }
    }
}
#endif
