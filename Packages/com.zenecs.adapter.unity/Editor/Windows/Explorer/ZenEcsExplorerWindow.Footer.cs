#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        private void DrawFooter()
        {
            if (_kernel == null) return;

            GUILayout.Space(4);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Pause button
                using (new EditorGUI.DisabledScope(false))
                {
                    var isPaused = _kernel.IsPaused;

                    // 툴바 라인 높이에 맞춘 Rect
                    var rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                    var pauseRect = GUILayoutUtility.GetRect(24f, rowHeight, GUILayout.Width(24f));
                    
                    // System 리스트에서 쓰는 것과 동일한 버튼 영역 보정
                    var btnRect = new Rect(
                        pauseRect.x,
                        pauseRect.y + 1f,
                        pauseRect.width,
                        pauseRect.height - 2f
                    );

                    var oldBg = GUI.backgroundColor;
                    var oldCont = GUI.contentColor;

                    if (isPaused)
                    {
                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                            ? new Color(0.24f, 0.48f, 0.90f, 1f) // Dark Skin
                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                        GUI.contentColor = Color.white;
                    }

                    if (GUI.Button(btnRect, ZenGUIContents.IconPause()))
                    {
                        _kernel.TogglePause();
                    }

                    GUI.backgroundColor = oldBg;
                    GUI.contentColor = oldCont;
                }

                GUILayout.Space(4);

                // Left - Since running
                var elapsed = _kernel.SimulationAccumulatorSeconds;
                GUILayout.Label(ZenStringTable.GetSinceRunning(elapsed), ZenGUIStyles.LabelLCNormal10);
                
                // Right - Find Menu
                GUILayout.FlexibleSpace();
                DrawFindMenu();
            }
        }
    }
}