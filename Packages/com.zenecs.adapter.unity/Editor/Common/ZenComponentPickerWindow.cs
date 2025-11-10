// Assets/.../Editor/ZEN/ZenComponentPickerWindow.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;

namespace ZenECS.EditorCommon
{
    /// <summary>
    /// 검색 가능한 컴포넌트 타입 선택 팝업 (바인더 픽커와 동일한 UX)
    /// - 목록 아이템을 클릭하면 즉시 추가
    /// - 이미 포함된 항목은 회색 처리 & 선택 불가
    /// - 네임스페이스 드롭다운 + 검색창 유지
    /// 사용: ZenComponentPickerWindow.Show(allTypes, disabledSet, onPick, activatorRect?, title?, mode?)
    /// </summary>
    public sealed class ZenComponentPickerWindow : EditorWindow
    {
        public enum PickerOpenMode { DropDown, UtilityFixedWidth }

        const float PICKER_FIXED_W = 560f;
        const float PICKER_MIN_H   = 320f;
        const float PICKER_MAX_H   = 2000f;
        const float PICKER_INIT_H  = 680f;
        const float ROW_H          = 22f;

        PickerOpenMode _openMode = PickerOpenMode.DropDown;
        bool _closeOnLostFocus   = true;

        List<string> _nsOptions;
        int _nsIndex = 0;

        static GUIStyle _nameStyle;
        static GUIStyle _nsStyle;

        static GUIStyle NameStyle
        {
            get
            {
                if (_nameStyle == null)
                {
                    _nameStyle = new GUIStyle(EditorStyles.label)
                    {
                        clipping  = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft,
                        richText  = true
                    };
                }
                return _nameStyle;
            }
        }

        static GUIStyle NsStyle
        {
            get
            {
                if (_nsStyle == null)
                {
                    _nsStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        clipping  = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return _nsStyle;
            }
        }

        List<Type>     _all;
        HashSet<Type>  _disabled;
        Action<Type>   _onPick;
        string         _title;
        string         _filter = "";
        Vector2        _scroll;
        int            _hoverIndex = -1;

        void OnEnable()
        {
            minSize = new Vector2(PICKER_FIXED_W, PICKER_MIN_H);
            maxSize = new Vector2(PICKER_FIXED_W, PICKER_MAX_H);

            // 포커스 초기화: 검색창에 커서
            EditorApplication.delayCall += () =>
            {
                Focus();
                EditorGUI.FocusTextInControl("ZC_SEARCH");
            };
        }

        void OnLostFocus()
        {
            if (_closeOnLostFocus) Close();
        }

        void OnGUI()
        {
            // 유틸리티 모드 가로 고정
            if (_openMode == PickerOpenMode.UtilityFixedWidth &&
                !Mathf.Approximately(position.width, PICKER_FIXED_W))
            {
                position = new Rect(position.x, position.y, PICKER_FIXED_W, position.height);
            }

            // ESC로 닫기
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
            }

            titleContent = new GUIContent(_title);

            using (new EditorGUILayout.VerticalScope())
            {
                // ── Toolbar : Namespace dropdown + Search
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (_nsOptions != null && _nsOptions.Count > 0)
                    {
                        var oldIdx = _nsIndex;
                        _nsIndex = EditorGUILayout.Popup(
                            _nsIndex,
                            _nsOptions.ToArray(),
                            EditorStyles.toolbarPopup,
                            GUILayout.ExpandWidth(true)
                        );
                        if (_nsIndex != oldIdx) Repaint();
                    }

                    GUILayout.Space(6f);

                    GUI.SetNextControlName("ZC_SEARCH");
                    var newFilter = GUILayout.TextField(
                        _filter,
                        GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField,
                        GUILayout.MinWidth(140), GUILayout.MaxWidth(220)
                    );

                    if (newFilter != _filter)
                    {
                        _filter = newFilter;
                        _hoverIndex = -1;
                        Repaint();
                    }

                    if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22)))
                    {
                        _filter = "";
                        GUI.FocusControl(null);
                        _hoverIndex = -1;
                        Repaint();
                    }
                }

                EditorGUILayout.Space(2);

                // ── 리스트
                var list = BuildFilteredList();

                using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = sv.scrollPosition;

                    if (list.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No matching components.", MessageType.Info);
                    }
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var t = list[i];
                            bool isDisabled = _disabled.Contains(t);

                            var r = GUILayoutUtility.GetRect(10, ROW_H, GUILayout.ExpandWidth(true));

                            // Hover 처리
                            if (r.Contains(Event.current.mousePosition))
                            {
                                _hoverIndex = i;
                                if (Event.current.type == EventType.MouseMove) Repaint();
                            }

                            if (i == _hoverIndex)
                                EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.15f));

                            // 표시 문자열: (All)일 때 "TypeName — Namespace", 그 외 "TypeName"
                            string nsStr = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;
                            var typeGc = new GUIContent(t.Name, t.FullName);

                            // 이름
                            var textRect = new Rect(r.x + 4, r.y, r.width - 8, r.height);
                            string label = (_nsIndex == 0)
                                ? $"{t.Name} <color=#888888>— {nsStr}</color>"
                                : t.Name;

                            if (isDisabled)
                                label = $"<color=#888888>{label}  (already added)</color>";

                            EditorGUI.LabelField(textRect, new GUIContent(label, t.FullName), NameStyle);

                            // 클릭 → 즉시 추가 (비활성은 무시)
                            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                            {
                                if (!isDisabled)
                                {
                                    _onPick?.Invoke(t);
                                    Close();
                                    GUIUtility.ExitGUI();
                                }
                                Event.current.Use();
                            }
                        }
                    }
                }

                // 키보드 네비게이션 (↑/↓/Enter)
                HandleKeyboard(list);
            }
        }

        List<Type> BuildFilteredList()
        {
            IEnumerable<Type> q = _all;

            if (_nsOptions != null && _nsIndex > 0)
            {
                string sel = _nsOptions[_nsIndex];
                if (sel == "(global)") q = q.Where(t => string.IsNullOrEmpty(t?.Namespace));
                else q = q.Where(t => string.Equals(t?.Namespace, sel, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(_filter))
            {
                q = q.Where(t =>
                    t.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (t.FullName?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            return q.ToList();
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
                _hoverIndex = Mathf.Clamp(_hoverIndex + 1, 0, Mathf.Max(0, list.Count - 1));
                e.Use();
                Repaint();
                return;
            }

            if (e.keyCode == KeyCode.UpArrow)
            {
                _hoverIndex = Mathf.Clamp(_hoverIndex - 1, 0, Mathf.Max(0, list.Count - 1));
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
                        _onPick?.Invoke(t);
                        Close();
                    }
                    e.Use();
                }
            }
        }

        // ZenComponentAttribute 달린 타입 검색(공용)
        public static IEnumerable<Type> FindAllZenComponents()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try { ts = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { ts = ex.Types.Where(x => x != null).ToArray(); }

                foreach (var t in ts)
                {
                    if (t == null || t.IsAbstract || t.IsGenericType) continue;
                    if (!t.IsClass && !t.IsValueType) continue;
                    if (t.GetCustomAttribute<ZenComponentAttribute>() == null) continue;
                    yield return t;
                }
            }
        }

        /// <summary>
        /// Show API (바인더 픽커와 동일한 사용성)
        /// </summary>
        public static void Show(
            IEnumerable<Type> allTypes,
            IEnumerable<Type> disabled,
            Action<Type> onPick,
            Rect? activatorRectGui = null,
            string title = "Add Component",
            PickerOpenMode mode = PickerOpenMode.DropDown)
        {
            var win = CreateInstance<ZenComponentPickerWindow>();
            win._all = allTypes.Distinct().OrderBy(t => t.FullName).ToList();
            win._disabled = new HashSet<Type>(disabled ?? Array.Empty<Type>());
            win._onPick = onPick;
            win._title = title;
            win._openMode = mode;
            win._closeOnLostFocus = true;

            // 네임스페이스 목록
            var nsSet = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in win._all) nsSet.Add(t?.Namespace ?? "(global)");
            win._nsOptions = new List<string> { "(All)" };
            if (nsSet.Remove("(global)")) win._nsOptions.Add("(global)");
            win._nsOptions.AddRange(nsSet);

            float initH = Mathf.Clamp(PICKER_INIT_H, PICKER_MIN_H, PICKER_MAX_H);

            // 기준 Anchor rect
            Rect anchorScr;
            if (activatorRectGui.HasValue && activatorRectGui.Value.width > 0f)
                anchorScr = GUIToScreenRect(activatorRectGui.Value);
            else
            {
                var mp = Event.current != null
                    ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                    : new Vector2(Screen.currentResolution.width * 0.5f, Screen.currentResolution.height * 0.5f);
                anchorScr = new Rect(mp.x, mp.y, 1f, 1f);
            }

            var editorRect = GetEditorScreenRect();
            float x = anchorScr.xMin;
            if (x + PICKER_FIXED_W > editorRect.xMax) x = anchorScr.xMax - PICKER_FIXED_W;
            x = Mathf.Clamp(x, editorRect.xMin + 6f, editorRect.xMax - PICKER_FIXED_W - 6f);

            float y;
            float spaceBelow = editorRect.yMax - (anchorScr.yMax + 6f);
            float spaceAbove = (anchorScr.yMin - 6f) - editorRect.yMin;
            if (spaceBelow >= initH) y = anchorScr.yMax + 2f;
            else if (spaceAbove >= initH) y = anchorScr.yMin - initH - 2f;
            else
            {
                float maxH = Mathf.Clamp(Mathf.Max(spaceBelow, spaceAbove), PICKER_MIN_H, PICKER_MAX_H);
                initH = maxH;
                y = (spaceBelow >= spaceAbove)
                    ? Mathf.Clamp(anchorScr.yMax + 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f)
                    : Mathf.Clamp(anchorScr.yMin - initH - 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f);
            }

            if (mode == PickerOpenMode.DropDown)
            {
                win.ShowAsDropDown(anchorScr, new Vector2(PICKER_FIXED_W, initH));
                win.Focus();
                return;
            }

            win.position = new Rect(x, y, PICKER_FIXED_W, initH);
            win.ShowUtility();
            win.Focus();
        }

        static Rect GUIToScreenRect(Rect guiRect)
        {
            var tl = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.xMin, guiRect.yMin));
            return new Rect(tl.x, tl.y, guiRect.width, guiRect.height);
        }

        static Rect GetEditorScreenRect()
        {
            return EditorGUIUtility.GetMainWindowPosition();
        }
    }
}
#endif
