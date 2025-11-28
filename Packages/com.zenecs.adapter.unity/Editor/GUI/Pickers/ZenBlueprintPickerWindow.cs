#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Blueprints;

namespace ZenECS.EditorCommon
{
    public sealed class ZenBlueprintPickerWindow : EditorWindow
    {
        private const float ROW_HEIGHT = 22f;
        private const float PADDING = 6f;

        private string _search = "";
        private Vector2 _scroll;
        private List<EntityBlueprint> _all = new();
        private Action<EntityBlueprint> _onPick = _ => { };
        private int _hover = -1;
        private GUIStyle? _rowStyle;
        private GUIStyle? _searchStyle;
        private string _title = "Select Entity Blueprint";
        private string? _hoverPath;

        public static void Show(
            Rect activatorRectGui,
            Action<EntityBlueprint> onPick,
            string title = "Select Entity Blueprint",
            Vector2? size = null)
        {
            var win = CreateInstance<ZenBlueprintPickerWindow>();
            win.titleContent = new GUIContent(title);
            win._title = title;
            win._onPick = onPick ?? (_ => { });

            // 프로젝트 내 모든 EntityBlueprint SO 검색
            var guids = AssetDatabase.FindAssets("t:EntityBlueprint");
            var list = new List<EntityBlueprint>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var bp = AssetDatabase.LoadAssetAtPath<EntityBlueprint>(path);
                if (bp != null)
                    list.Add(bp);
            }

            win._all = list;

            var w = size?.x ?? 500f;
            var h = size?.y ?? 360f;

// activatorRectGui: ZenECS Explorer 내부 GUI 좌표계의 + 버튼 rect
// ➜ 화면 좌표로 변환
            var topLeft = GUIUtility.GUIToScreenPoint(new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

// 버튼 바로 아래(y)에서 팝업 시작하도록 bottomLeft.y 사용
            var screenRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f,      // 살짝 아래로 2픽셀 띄워줌 (겹치지 않게)
                activatorRectGui.width,
                activatorRectGui.height
            );

            win.ShowAsDropDown(screenRect, new Vector2(w, h));
            win.Focus();
        }

        private void OnEnable()
        {
            // Row 스타일: 기본 Label에서 복사
            var baseLabel = EditorStyles.label;

            _rowStyle = new GUIStyle(baseLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true,
                padding = new RectOffset(4, 4, 0, 0)
            };

            // 검색창 스타일: EditorStyles.toolbarSearchField 기반
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);

            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            DrawSearchBar();
            EditorGUILayout.Space(2);

            var filtered = Filtered().ToList();

            // 👇 Blueprint가 하나도 없을 때 전용 메시지
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
                        "No EntityBlueprint assets found.\n" +
                        "Create an EntityBlueprint ScriptableObject to use this picker.",
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

            var contentRect = new Rect(0, 0, viewRect.width - 16, filtered.Count * ROW_HEIGHT + PADDING * 2);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
            var y = PADDING;
            var i = 0;

            foreach (var bp in filtered)
            {
                var r = new Rect(PADDING, y, contentRect.width - PADDING * 2, ROW_HEIGHT);

                // Hover 배경 (원하면 유지)
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

                var name = bp.name;
                var path = AssetDatabase.GetAssetPath(bp);

                // 길면 뒤쪽만 남기기
                const int maxLen = 60;
                string shortPath = path;
                if (!string.IsNullOrEmpty(path) && path.Length > maxLen)
                {
                    shortPath = "…" + path.Substring(path.Length - maxLen);
                }

                // 화면에 보이는 텍스트
                var displayText = $"{name}   <size=9><color=#888888>{shortPath}</color></size>";

                // 🔥 tooltip에 전체 경로 넣기
                var content = new GUIContent(displayText, path);

                if (GUI.Button(r, content, _rowStyle!))
                {
                    _onPick(bp);
                    Close();
                }

                y += ROW_HEIGHT;
                i++;
            }

            GUI.EndScrollView();
            
            //HandleKeyboard(filtered);
            if (Event.current.type == EventType.MouseMove) Repaint();

            // if (showTooltip && !string.IsNullOrEmpty(GUI.tooltip) && Event.current.type == EventType.Repaint)
            // {
            //     var guiContent = new GUIContent(GUI.tooltip);
            //     var size = EditorStyles.helpBox.CalcSize(guiContent);
            //
            //     var mouse = Event.current.mousePosition;
            //     var rect = new Rect(
            //         mouse.x + 16,
            //         mouse.y + 20,
            //         size.x + 8,
            //         size.y + 4);
            //
            //     // 🔹 1) 오른쪽으로 넘치면 왼쪽으로 접기
            //     float margin = 4f;
            //     float maxX = position.width - size.x - margin;
            //     if (rect.x > maxX)
            //         rect.x = Mathf.Max(margin, maxX);
            //
            //     // 🔹 2) 아래로 넘치면 위로 올리기
            //     float maxY = position.height - rect.height - margin;
            //     if (rect.y > maxY)
            //         rect.y = Mathf.Max(margin, mouse.y - rect.height - 8f);
            //
            //     GUI.Box(rect, guiContent, EditorStyles.helpBox);
            // }

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

                // 🔥 항상 한 번은 그리되, 내용만 바뀜 (Layout/ Repaint 균일)
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
                GUI.SetNextControlName("ZenBlueprintPickerSearch");
                _search = GUILayout.TextField(_search, _searchStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    _search = "";
                    GUI.FocusControl("ZenBlueprintPickerSearch");
                }
            }
        }

        private IEnumerable<EntityBlueprint> Filtered()
        {
            IEnumerable<EntityBlueprint> src = _all;
            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.Trim();
                src = src.Where(bp =>
                {
                    var name = bp != null ? bp.name : "";
                    var path = bp != null ? AssetDatabase.GetAssetPath(bp) : "";
                    return (!string.IsNullOrEmpty(name) &&
                            name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                           || (!string.IsNullOrEmpty(path) &&
                               path.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
                });
            }

            return src.OrderBy(bp => bp.name);
        }

        private void HandleKeyboard(List<EntityBlueprint> filtered)
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
                        var bp = filtered[_hover];
                        _onPick(bp);
                        Close();
                        e.Use();
                    }

                    break;
                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }
    }
}
#endif
