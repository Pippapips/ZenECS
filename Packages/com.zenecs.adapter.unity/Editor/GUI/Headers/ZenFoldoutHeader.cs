// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenFoldoutHeader.cs
// Purpose: Unified foldable header component with arrow, title, namespace,
//          and optional right-side button slots for ZenECS editor UIs.
// Key concepts:
//   • Foldable header: collapsible sections with consistent styling.
//   • Two-line labels: title and namespace display with inline or stacked layout.
//   • Right button area: reserved space for action buttons.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Static utility class that provides a unified foldable header (arrow + title + right button slot).
    /// </summary>
    public static class ZenFoldoutHeader
    {
        static GUIStyle _foldout;

        /// <summary>
        /// Gets the foldout style.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Foldout style with bold font applied. Created on first access and then cached.
        /// </para>
        /// </remarks>
        public static GUIStyle FoldoutStyle
        {
            get
            {
                if (_foldout == null)
                {
                    _foldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold, // Keep title bold
                    };
                }

                return _foldout;
            }
        }

        static GUIStyle _boldLabel;

        /// <summary>
        /// Gets the bold label style.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bold label style with left alignment, clipping, and rich text enabled.
        /// Created on first access and then cached.
        /// </para>
        /// </remarks>
        public static GUIStyle BoldLabel
        {
            get
            {
                if (_boldLabel == null)
                {
                    _boldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        richText = true
                    };
                }

                return _boldLabel;
            }
        }

        static GUIStyle _miniLabel;

        /// <summary>
        /// Gets the mini label style.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Small label style with left alignment and clipping applied.
        /// Created on first access and then cached.
        /// </para>
        /// </remarks>
        public static GUIStyle MiniLabel
        {
            get
            {
                if (_miniLabel == null)
                {
                    _miniLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                }

                return _miniLabel;
            }
        }

        static readonly float _rowH = EditorGUIUtility.singleLineHeight;

        /// <summary>
        /// Structure that represents the scope of a foldout header.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use with a <see cref="using"/> statement to define the body area of the header.
        /// Automatically ends the vertical layout when the scope exits.
        /// </para>
        /// </remarks>
        public struct Scope : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of <see cref="Scope"/>.
            /// </summary>
            /// <param name="opened">Whether the header is open. Currently unused.</param>
            public Scope(bool opened)
            {
                /* noop, currently only finishes layout */
            }

            /// <summary>
            /// Ends the scope and finishes the vertical layout.
            /// </summary>
            public void Dispose() => EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Begins drawing a foldout header.
        /// </summary>
        /// <param name="isOpen">
        /// The open/close state of the header. This value is updated based on user interaction.
        /// </param>
        /// <param name="title">The title to display on the header.</param>
        /// <param name="drawRightButtons">
        /// Action that draws buttons on the right side of the header. If <c>null</c>, the button area is empty.
        /// </param>
        /// <param name="foldable">
        /// Whether the header can be folded. If <c>false</c>, only a bold label is displayed without an arrow,
        /// and <paramref name="isOpen"/> is forced to <c>false</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Scope"/> structure that can be used with a <see cref="using"/> statement.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Draws the header, and the body should be drawn immediately after within the returned <see cref="Scope"/>.
        /// </para>
        /// </remarks>
        public static Scope Begin(ref bool isOpen, string title, Action drawRightButtons = null, bool foldable = true)
        {
            EditorGUILayout.BeginVertical("box");
            var rHead = GUILayoutUtility.GetRect(10, _rowH + 4f);

            var left = new Rect(rHead.x + 2, rHead.y + 2, rHead.width - RightButtonsWidth - 4, _rowH);
            var right = new Rect(rHead.xMax - RightButtonsWidth, rHead.y + 2, RightButtonsWidth, _rowH);

            if (foldable)
            {
                isOpen = EditorGUI.Foldout(left, isOpen, title, true, FoldoutStyle);
            }
            else
            {
                // Not foldable: label only without icon, always false for expansion
                isOpen = false;
                EditorGUI.LabelField(left, title, BoldLabel);
            }

            // Right button area
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUILayout.BeginArea(right);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                drawRightButtons?.Invoke();
            }

            GUILayout.EndArea();
            EditorGUI.indentLevel = oldIndent;

            return new Scope(isOpen);
        }

        /// <summary>
        /// Draws a toggle button.
        /// </summary>
        /// <param name="state">The current state of the button.</param>
        /// <param name="label">The label to display on the button.</param>
        /// <param name="width">The width of the button. Default is 60 pixels.</param>
        /// <returns>The new state of the button.</returns>
        public static bool ToggleButton(bool state, string label, float width = 60f)
            => GUILayout.Toggle(state, label, "Button", GUILayout.Width(width));

        /// <summary>
        /// Draws a small button.
        /// </summary>
        /// <param name="label">The label to display on the button.</param>
        /// <param name="width">The width of the button. Default is 56 pixels.</param>
        /// <returns>Returns <c>true</c> if the button was clicked.</returns>
        public static bool SmallButton(string label, float width = 56f)
            => GUILayout.Button(label, GUILayout.Width(width));

        // === Absolute coordinate version (for list elements) =========================================
        
        /// <summary>
        /// The width of the button area on the right side of the header.
        /// </summary>
        public const float RightButtonsWidth = 186f;

        /// <summary>
        /// Draws a header using the list/absolute coordinate version.
        /// </summary>
        /// <param name="isOpen">
        /// The open/close state of the header. This value is updated based on user interaction.
        /// </param>
        /// <param name="fullRect">The full area where the header will be drawn.</param>
        /// <param name="title">The title to display on the header.</param>
        /// <param name="nameSpace">The namespace to display on the header.</param>
        /// <param name="drawRightButtons">
        /// Action that draws buttons on the right side of the header. If <c>null</c>, the button area is empty.
        /// </param>
        /// <param name="foldable">
        /// Whether the header can be folded. If <c>false</c>, only a label is drawn without an arrow.
        /// </param>
        /// <param name="noMarginTitle">Whether to omit margins around the title.</param>
        /// <remarks>
        /// <para>
        /// Used when drawing a header arranged like a list element using absolute coordinates.
        /// </para>
        /// </remarks>
        public static void DrawRow(ref bool isOpen, Rect fullRect, string title, string nameSpace,
            Action<Rect> drawRightButtons = null,
            bool foldable = true, bool noMarginTitle = true)
        {
            var rowH = EditorGUIUtility.singleLineHeight;
            var left = new Rect(fullRect.x + 2, fullRect.y + 1, fullRect.width - RightButtonsWidth - 4, rowH);
            var right = new Rect(fullRect.xMax - RightButtonsWidth, fullRect.y + 1, RightButtonsWidth, rowH);

            if (foldable)
            {
                // Separate arrow area and label area
                float arrowW = noMarginTitle ? 0 : 16;
                var arrowRect = new Rect(left.x, left.y, arrowW, left.height);
                var labelRect = new Rect(left.x + arrowW + 2f, left.y, left.width - arrowW - 2f, left.height);

                // Set to false so label click doesn't toggle
                isOpen = EditorGUI.Foldout(arrowRect, isOpen, GUIContent.none, false, FoldoutStyle);

                if (!string.IsNullOrEmpty(nameSpace))
                {
                    if (noMarginTitle)
                    {
                        var nameGc = new GUIContent(title);
                        var nsGc = new GUIContent($"[{nameSpace}]");

                        var bold = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            richText = true
                        };
                        var mini = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            normal =
                            {
                                textColor = EditorGUIUtility.isProSkin
                                    ? new Color(0.75f, 0.75f, 0.75f, 1)
                                    : new Color(0.35f, 0.35f, 0.35f, 1)
                            }
                        };

                        var gap = 6;
                        DrawInlineLabels(labelRect, nameGc, nsGc, gap, bold, mini);
                    }
                    else
                    {
                        var nameGc = new GUIContent(title);
                        var nsGc = new GUIContent($"[{nameSpace}]");
                        DrawTwoLineLabels(labelRect, nameGc, nsGc);
                        GUILayoutUtility.GetRect(1, 16);
                    }
                }
                else
                {
                    EditorGUI.LabelField(labelRect, title, BoldLabel);
                }
            }
            else
            {
                isOpen = false;
                float arrowW = noMarginTitle ? 0 : 16;
                var arrowRect = new Rect(left.x, left.y, arrowW, left.height);
                var labelRect = new Rect(left.x + arrowW + 2f, left.y, left.width - arrowW - 2f, left.height);
                var nameGc = new GUIContent(title);
                var nsGc = new GUIContent($"[{nameSpace}]");
                DrawTwoLineLabels(labelRect, nameGc, nsGc);
                GUILayoutUtility.GetRect(1, 16);
                //EditorGUI.LabelField(left, title, BoldLabel);
            }

            drawRightButtons?.Invoke(right);
        }

        /// <summary>
        /// Draws a two-line label.
        /// </summary>
        /// <param name="rect">The area where the label will be drawn.</param>
        /// <param name="line1">The content to display on the first line.</param>
        /// <param name="line2">The content to display on the second line.</param>
        /// <param name="style1">The style to use for the first line. If <c>null</c>, uses the default label style.</param>
        /// <param name="style2">The style to use for the second line. If <c>null</c>, uses the default mini label style.</param>
        /// <param name="vGap">The vertical spacing between the two lines. Default is 2 pixels.</param>
        public static void DrawTwoLineLabels(Rect rect,
            GUIContent line1, GUIContent line2,
            GUIStyle style1 = null, GUIStyle style2 = null,
            float vGap = 2f)
        {
            style1 ??= EditorStyles.label;
            style2 ??= EditorStyles.miniLabel;
            style2.normal.textColor = new Color(0.55f, 0.55f, 0.55f, 1);

            float h1 = style1.CalcHeight(line1, rect.width);
            float h2 = style2.CalcHeight(line2, rect.width);

            var r1 = new Rect(rect.x, rect.y, rect.width, h1);
            var r2 = new Rect(rect.x, r1.yMax + vGap, rect.width, h2);

            EditorGUI.LabelField(r1, line1, style1);
            EditorGUI.LabelField(r2, line2, style2);
        }
        
        static void DrawInlineLabels(Rect lineRect,
            GUIContent left, GUIContent right,
            float gap = 6f,
            GUIStyle leftStyle = null, GUIStyle rightStyle = null)
        {
            leftStyle ??= EditorStyles.label; // Specify EditorStyles.boldLabel etc. if needed
            rightStyle ??= EditorStyles.miniLabel; // Good for light text like namespace

            // 1) Calculate actual required width of left label (before clipping)
            var leftSize = leftStyle.CalcSize(left);
            float leftW = Mathf.Min(leftSize.x, lineRect.width);

            // 2) Calculate left/right Rects
            var rLeft = new Rect(lineRect.x, lineRect.y, leftW, lineRect.height);
            var rRight = new Rect(rLeft.xMax + gap, lineRect.y,
                Mathf.Max(0, lineRect.xMax - (rLeft.xMax + gap)),
                lineRect.height);

            // 3) Enable clipping for the right side (cleanly cuts off if overflow)
            var rightClipped = new GUIStyle(rightStyle) { clipping = TextClipping.Clip };

            // 4) Draw
            EditorGUI.LabelField(rLeft, left, leftStyle);
            if (rRight.width > 1f)
                EditorGUI.LabelField(rRight, right, rightClipped);
        }
    }
}
#endif