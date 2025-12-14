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
        /// Gets the search/ping icon content with optional tooltip
        /// </summary>
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
        
        public static GUIContent IconPause()
        {
            var icon = EditorGUIUtility.IconContent("PauseButton");
            if (icon == null || icon.image == null)
                icon = EditorGUIUtility.TrTextContent("⏸");
            return icon;
        }
        
        public static GUIContent IconPlus()
        {
            // Unity 기본 검색 아이콘
            var gc = EditorGUIUtility.IconContent("d_CreateAddNew");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("CreateAddNew");

            if (gc == null)
                gc = new GUIContent("+");

            return gc;
        }

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
