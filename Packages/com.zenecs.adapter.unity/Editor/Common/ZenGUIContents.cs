// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenGUIContents.cs
// Purpose: Reusable GUIContent instances and drawing helpers for ZenECS
//          editor windows and inspectors.
// Key concepts:
//   • Icon content: ping, pause, plus icons with fallbacks.
//   • Drawing utilities: line separators, indented controls.
//   • Editor-only: safe for use in custom PropertyDrawers and EditorWindows.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    public static class ZenGUIContents
    {
        /// <summary>
        /// Gets the search/ping icon content.
        /// </summary>
        /// <param name="tooltip">Optional tooltip text.</param>
        /// <returns>
        /// A <see cref="GUIContent"/> containing the Unity default search icon.
        /// If the icon cannot be found, it is replaced with an emoji (🔍).
        /// </returns>
        public static GUIContent IconPing(string? tooltip = null)
        {
            // Unity default search icon
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || !gc.image)
                gc = EditorGUIUtility.IconContent("Search Icon");

            // Fallback to text if icon not found
            gc ??= new GUIContent("🔍");
            
            if (!string.IsNullOrEmpty(tooltip))
                gc.tooltip = tooltip;
                
            return gc;
        }
        
        /// <summary>
        /// Gets the pause icon content.
        /// </summary>
        /// <returns>
        /// A <see cref="GUIContent"/> containing the Unity default pause button icon.
        /// If the icon cannot be found, it is replaced with an emoji (⏸).
        /// </returns>
        public static GUIContent IconPause()
        {
            var icon = EditorGUIUtility.IconContent("PauseButton");
            if (icon == null || icon.image == null)
                icon = EditorGUIUtility.TrTextContent("⏸");
            return icon;
        }
        
        /// <summary>
        /// Gets the add/create icon content.
        /// </summary>
        /// <returns>
        /// A <see cref="GUIContent"/> containing the Unity default add/create icon.
        /// If the icon cannot be found, it is replaced with "+" text.
        /// </returns>
        public static GUIContent IconPlus()
        {
            // Unity default search icon
            var gc = EditorGUIUtility.IconContent("d_CreateAddNew");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("CreateAddNew");

            if (gc == null)
                gc = new GUIContent("+");

            return gc;
        }

        /// <summary>
        /// Draws a horizontal separator line.
        /// </summary>
        /// <param name="height">The height of the line. Default is 1 pixel.</param>
        /// <param name="color">
        /// The color of the line. If <c>null</c>, uses default gray (0.3, 0.3, 0.3).
        /// </param>
        /// <remarks>
        /// <para>
        /// Draws a horizontal line with the specified height and color at the current layout position.
        /// The line is drawn in an area where indent level is applied.
        /// </para>
        /// </remarks>
        public static void DrawLine(float height = 1, Color? color = null)
        {
            var lineRect = EditorGUILayout.GetControlRect(false, height);
            lineRect = EditorGUI.IndentedRect(lineRect);
            if (color != null)
            {
                EditorGUI.DrawRect(lineRect, color.Value);
            }
            else
            {
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            }
        }
    }
}
