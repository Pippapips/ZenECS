#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        void DrawMainLayout()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftSystemTreePanel();
                DrawVerticalSeparator();
                DrawRightEntityPanel();
            }
        }
        
        /// <summary>
        /// Draws a vertical separator line that stretches to fill the available height.
        /// </summary>
        void DrawVerticalSeparator(float width = 1f, float alpha = 0.2f)
        {
            var sepRect = GUILayoutUtility.GetRect(
                width, width,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(width));

            var c = new Color(0f, 0f, 0f, alpha);
            EditorGUI.DrawRect(sepRect, c);
        }
    }
}