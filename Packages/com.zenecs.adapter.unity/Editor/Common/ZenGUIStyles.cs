// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenGUIStyles.cs
// Purpose: Centralized GUIStyle definitions and layout helpers for ZenECS
//          editor tooling, providing consistent visual styling.
// Key concepts:
//   • Style definitions: labels, buttons, foldouts, text fields with variants.
//   • Layout helpers: single-line rects, indented controls, left/right alignment.
//   • Lazy initialization: styles created on first access and cached.
//   • Editor-only: safe for use in custom PropertyDrawers and EditorWindows.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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
        /// <summary>
        /// Gets a layout Rect with single-line height.
        /// </summary>
        /// <returns>
        /// A <see cref="Rect"/> with single-line height and expandable width.
        /// </returns>
        public static Rect GetSingleLineRect()
        {
            return GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
        }
        
        /// <summary>
        /// Gets a layout Rect with single-line height and indent applied.
        /// </summary>
        /// <returns>
        /// A <see cref="Rect"/> with single-line height where the current indent level is applied.
        /// </returns>
        public static Rect GetIndentedSingleLineRect()
        {
            // Reflect indent
            return EditorGUI.IndentedRect(GetSingleLineRect());
        }

        /// <summary>
        /// Calculates multiple Rects on a single line aligned to the left.
        /// </summary>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. Rects are calculated for the length of the array.
        /// </param>
        /// <remarks>
        /// <para>
        /// Rects are arranged sequentially from left to right.
        /// </para>
        /// </remarks>
        public static void GetLeftSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetLeftRects(GetSingleLineRect(), width, gap, ref rects);
        }

        /// <summary>
        /// Calculates multiple Rects on a single line aligned to the right.
        /// </summary>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. Rects are calculated for the length of the array.
        /// </param>
        /// <remarks>
        /// <para>
        /// Rects are arranged sequentially from right to left.
        /// </para>
        /// </remarks>
        public static void GetRightSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetRightRects(GetSingleLineRect(), width, gap, ref rects);
        }
        
        /// <summary>
        /// Calculates multiple Rects on an indented single line aligned to the left.
        /// </summary>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. Rects are calculated for the length of the array.
        /// </param>
        public static void GetLeftIndentedSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetLeftRects(GetIndentedSingleLineRect(), width, gap, ref rects);
        }
        
        /// <summary>
        /// Calculates multiple Rects on an indented single line aligned to the right.
        /// </summary>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. Rects are calculated for the length of the array.
        /// </param>
        public static void TryGetRightIndentedSingleLineRects(float width, float gap, ref Rect[] rects)
        {
            GetRightRects(GetIndentedSingleLineRect(), width, gap, ref rects);
        }

        /// <summary>
        /// Calculates multiple Rects aligned to the left within the specified row Rect.
        /// </summary>
        /// <param name="rowRect">The row area where Rects will be placed.</param>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. If the array is empty, no operation is performed.
        /// </param>
        /// <remarks>
        /// <para>
        /// The first Rect is placed at the left edge of the row, and subsequent Rects are arranged sequentially to the right.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Calculates multiple Rects aligned to the right within the specified row Rect.
        /// </summary>
        /// <param name="rowRect">The row area where Rects will be placed.</param>
        /// <param name="width">The width of each Rect.</param>
        /// <param name="gap">The spacing between Rects.</param>
        /// <param name="rects">
        /// An array to fill with calculated Rects. If the array is empty, no operation is performed.
        /// </param>
        /// <remarks>
        /// <para>
        /// The first Rect is placed at the right edge of the row, and subsequent Rects are arranged sequentially to the left.
        /// </para>
        /// </remarks>
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
        
        /// <summary>
        /// Provides a scope for temporarily changing label style and width.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use with a <see cref="using"/> statement to temporarily change label style and width,
        /// and automatically restore to the original state when the scope exits.
        /// </para>
        /// </remarks>
        public readonly struct LabelScope : IDisposable
        {
            private readonly GUIStyle _backupStyle;
            private readonly float _backupLabelWidth;
            private readonly bool _hasCustomWidth;

            /// <summary>
            /// Initializes a new instance of <see cref="LabelScope"/>.
            /// </summary>
            /// <param name="style">The label style to apply.</param>
            /// <param name="labelWidth">The label width to apply. If <c>null</c>, the width is not changed.</param>
            public LabelScope(GUIStyle style, float? labelWidth = null)
            {
                _backupStyle = new GUIStyle(EditorStyles.label);
                _backupLabelWidth = EditorGUIUtility.labelWidth;
                _hasCustomWidth = labelWidth.HasValue;

                ApplyStyle(style);

                if (labelWidth.HasValue)
                    EditorGUIUtility.labelWidth = labelWidth.Value;
            }

            /// <summary>
            /// Applies the label style.
            /// </summary>
            /// <param name="src">The source style to apply.</param>
            private static void ApplyStyle(GUIStyle src)
            {
                EditorStyles.label.font = src.font;
                EditorStyles.label.fontSize = src.fontSize;
                EditorStyles.label.fontStyle = src.fontStyle;
                EditorStyles.label.alignment = src.alignment;
                EditorStyles.label.normal.textColor = src.normal.textColor;
                EditorStyles.label.richText = src.richText;
            }

            /// <summary>
            /// Ends the scope and restores the label style and width to their original state.
            /// </summary>
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
                    _linkLabel.active.textColor = Color.orangeRed;   // While clicking
                    _linkLabel.focused.textColor = Color.orangeRed;  // Keyboard focus
                    _linkLabel.alignment = TextAnchor.MiddleLeft;

                    // Remove background so it doesn't look like a button
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
