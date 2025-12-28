// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: EntityLinkHierarchyMarker.cs
// Purpose: Unity Hierarchy window marker that displays a visual indicator
//          on GameObjects with EntityLink components.
// Key concepts:
//   • Hierarchy decoration: shows "E" marker for EntityLink components.
//   • Visual indicator: helps identify ECS-linked GameObjects in hierarchy.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.Editor.Hierarchy
{
    /// <summary>
    /// Displays a prominent "E" mark on the right end of GameObject with EntityLink in Hierarchy.
    /// </summary>
    [InitializeOnLoad]
    public static class EntityLinkHierarchyMarker
    {
        static EntityLinkHierarchyMarker()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (go == null) return;

            if (!go.TryGetComponent<EntityLink>(out _))
                return;

            const float paddingRight = 4f;
            const float width = 22f;
            const float height = 14f;

            // Calculate small badge area based on right end
            var r = new Rect(
                selectionRect.xMax - width - paddingRight,
                selectionRect.y + (selectionRect.height - height) * 0.5f,
                width,
                height
            );

            // Slightly different color depending on dark/light skin
            Color bg = EditorGUIUtility.isProSkin
                ? new Color(0.29f, 0.22f, 1f, 0.9f)    // Pro: Bright blue
                : new Color(0.18f, 0.11f, 0.9f, 0.95f); // Light: Slightly darker blue

            Color border = new Color(0f, 0f, 0f, 0.6f);
            Color text = Color.white;

            // Background (draw twice for slightly rounded feel)
            var bgRect = r;
            bgRect.xMin += 0.5f;
            bgRect.xMax -= 0.5f;
            bgRect.yMin += 0.5f;
            bgRect.yMax -= 0.5f;

            // Outer border
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMin - 1, bgRect.width + 2, 1), border);
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMax,     bgRect.width + 2, 1), border);
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMin, 1, bgRect.height),       border);
            EditorGUI.DrawRect(new Rect(bgRect.xMax,     bgRect.yMin, 1, bgRect.height),       border);

            // Inner fill
            EditorGUI.DrawRect(bgRect, bg);

            // Text style
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = text },
                clipping = TextClipping.Clip
            };

            GUI.Label(bgRect, "E", style);
        }
    }
}
#endif
