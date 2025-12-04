#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    public static class ZenGUIStyles
    {
        public static Rect GetSingleLineRect()
        {
            return GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
        }
        
        public static Rect GetIndentedSingleLineRect()
        {
            // Indent 반영
            return EditorGUI.IndentedRect(GetSingleLineRect());
        }

        public static void GetLeftSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetLeftRects(GetSingleLineRect(), width, gap, ref rects);
        }

        public static void GetRightSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetRightRects(GetSingleLineRect(), width, gap, ref rects);
        }
        
        public static void GetLeftIndentedSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetLeftRects(GetIndentedSingleLineRect(), width, gap, ref rects);
        }
        
        public static void TryGetRightIndentedSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetRightRects(GetIndentedSingleLineRect(), width, gap, ref rects);
        }

        public static void GetLeftRects(Rect rowRect, float width, float gap, ref Rect[] rects)
        {
            if (rects.Length == 0) return;

            int count = rects.Length;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    rects[i] = new Rect(rowRect.x, rowRect.y, width, rowRect.height);
                }
                else
                {
                    var rect = rects[i - 1];
                    rects[i] = new Rect(rect.x + gap + width, rowRect.y, width, rowRect.height);
                }
            }
        }

        public static void GetRightRects(Rect rowRect, float width, float gap, ref Rect[] rects)
        {
            if (rects.Length == 0) return;

            int count = rects.Length;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    rects[i] = new Rect(rowRect.xMax - width, rowRect.y, width, rowRect.height);
                }
                else
                {
                    var rect = rects[i - 1];
                    rects[i] = new Rect(rect.x - gap - width, rowRect.y, width, rowRect.height);
                }
            }
        }
        
        public readonly struct LabelScope : IDisposable
        {
            private readonly GUIStyle _backupStyle;
            private readonly float _backupLabelWidth;
            private readonly bool _hasCustomWidth;

            public LabelScope(GUIStyle style, float? labelWidth = null)
            {
                _backupStyle = new GUIStyle(EditorStyles.label);
                _backupLabelWidth = EditorGUIUtility.labelWidth;
                _hasCustomWidth = labelWidth.HasValue;

                ApplyStyle(style);

                if (labelWidth.HasValue)
                    EditorGUIUtility.labelWidth = labelWidth.Value;
            }

            private static void ApplyStyle(GUIStyle src)
            {
                EditorStyles.label.font = src.font;
                EditorStyles.label.fontSize = src.fontSize;
                EditorStyles.label.fontStyle = src.fontStyle;
                EditorStyles.label.alignment = src.alignment;
                EditorStyles.label.normal.textColor = src.normal.textColor;
                EditorStyles.label.richText = src.richText;
            }

            public void Dispose()
            {
                ApplyStyle(_backupStyle);
                if (_hasCustomWidth)
                    EditorGUIUtility.labelWidth = _backupLabelWidth;
            }
        }
        
        private static GUIStyle? _linkLabel;
        public static GUIStyle LinkLabel
        {
            get
            {
                if (_linkLabel == null)
                {
                    _linkLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        richText = false,
                    };

                    _linkLabel.normal.textColor = Color.lightGray;
                    _linkLabel.hover.textColor  = Color.orangeRed;
                    _linkLabel.active.textColor = Color.orangeRed;   // 클릭 중
                    _linkLabel.focused.textColor = Color.orangeRed;  // 키보드 focus
                    _linkLabel.alignment = TextAnchor.MiddleLeft;

                    // 배경은 없애서 “버튼처럼” 안 보이게
                    _linkLabel.normal.background  = null;
                    _linkLabel.hover.background   = null;
                    _linkLabel.active.background  = null;
                    _linkLabel.focused.background = null;
                }

                return _linkLabel;
            }
        }
        
        private static GUIStyle? _titleStyle;
        public static GUIStyle TitleStyle
        {
            get
            {
                _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = EditorStyles.boldLabel.fontSize + 2,
                    wordWrap = true
                };
                return _titleStyle;
            }
        }
        
        private static GUIStyle? _bodyStyle;
        public static GUIStyle BodyStyle
        {
            get
            {
                _bodyStyle ??= new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                return _bodyStyle;
            }
        }
        
        private static GUIStyle? _buttonMLNormal10;
        public static GUIStyle ButtonMLNormal10
        {
            get
            {
                _buttonMLNormal10 ??= new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };
                return _buttonMLNormal10;
            }
        }
        
        private static GUIStyle? _buttonMCNormal10;
        public static GUIStyle ButtonMCNormal10
        {
            get
            {
                _buttonMCNormal10 ??= new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };
                return _buttonMCNormal10;
            }
        }

        private static GUIStyle? _labelBold14;
        public static GUIStyle LabelBold14
        {
            get
            {
                _labelBold14 ??= new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    richText = true
                };
                return _labelBold14;
            }
        }

        private static GUIStyle? _labelLCNormal10;
        public static GUIStyle LabelLCNormal10
        {
            get
            {
                _labelLCNormal10 ??= new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontStyle = FontStyle.Normal,
                    fontSize = 10,
                    richText = true
                };
                return _labelLCNormal10;
            }
        }

        private static GUIStyle? _labelMLNormal10;
        public static GUIStyle LabelMLNormal10
        {
            get
            {
                _labelMLNormal10 ??= new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 10,
                    richText = true
                };
                return _labelMLNormal10;
            }
        }

        private static GUIStyle? _labelMLNormal9;
        public static GUIStyle LabelMLNormal9
        {
            get
            {
                _labelMLNormal9 ??= new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 9,
                    richText = true,
                };
                return _labelMLNormal9;
            }
        }

        private static GUIStyle? _labelMLNormal9Gray;
        public static GUIStyle LabelMLNormal9Gray
        {
            get
            {
                _labelMLNormal9Gray ??= new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 9,
                    normal = new GUIStyleState { textColor = Color.darkGray },
                };
                return _labelMLNormal9Gray;
            }
        }

        private static GUIStyle? _textFieldLFNormal10;
        public static GUIStyle TextFieldLFNormal10
        {
            get
            {
                _textFieldLFNormal10 ??= new GUIStyle(GUI.skin.textField)
                {
                    alignment = TextAnchor.LowerLeft,
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };
                return _textFieldLFNormal10;
            }
        }
        
        private static GUIStyle? _foldoutNormal;
        public static GUIStyle FoldoutNormal
        {
            get
            {
                _foldoutNormal ??= new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    margin = new RectOffset(1, 0, 0, 0),
                    contentOffset = new Vector2(4, 0),
                };
                return _foldoutNormal;
            }
        }

        private static GUIStyle? _systemFoldout10;
        public static GUIStyle SystemFoldout10
        {
            get
            {
                _systemFoldout10 ??= new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Normal,
                    fontSize = 10,
                    richText = true,
                    alignment = TextAnchor.MiddleLeft,
                    normal = new GUIStyleState { textColor = Color.lightGray },
                    focused = new GUIStyleState { textColor = Color.lightBlue },
                    hover = new GUIStyleState { textColor = Color.lightBlue },
                    active = new GUIStyleState { textColor = Color.lightBlue },
                };
                return _systemFoldout10;
            }
        }

        private static GUIStyle? _systemFoldout;
        public static GUIStyle SystemFoldout
        {
            get
            {
                _systemFoldout ??= new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Normal,
                    fontSize = 11,
                    richText = true,
                    alignment = TextAnchor.MiddleLeft,
                    
                    normal = new GUIStyleState { textColor = Color.lightGray },
                    focused = new GUIStyleState { textColor = Color.lightBlue },
                    hover = new GUIStyleState { textColor = Color.lightBlue },
                    active = new GUIStyleState { textColor = Color.lightBlue },
                };
                return _systemFoldout;
            }
        }

        private static GUIStyle? _buttonPadding;
        public static GUIStyle ButtonPadding
        {
            get
            {
                _buttonPadding = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(3, 3, 3, 3),
                    margin = new RectOffset(0, 0, 0, 0)
                };
                return _buttonPadding;
            }
        }
    }
}
