#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    public static class ZenGUIContents
    {
        public static GUIContent IconPing()
        {
            // Unity 기본 검색 아이콘
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || !gc.image)
                gc = EditorGUIUtility.IconContent("Search Icon");

            // 혹시 아이콘을 못 찾았을 경우 텍스트로 fallback
            gc ??= new GUIContent("🔍");
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
