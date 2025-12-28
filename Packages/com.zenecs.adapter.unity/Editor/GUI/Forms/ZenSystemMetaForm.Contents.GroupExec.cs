// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSystemMetaForm.Contents.GroupExec.cs
// Purpose: System group and execution phase editing implementation for
//          ZenSystemMetaForm partial class.
// Key concepts:
//   • Group editing: modify system group assignment.
//   • Execution phase: display deterministic vs non-deterministic phase.
//   • Partial class: part of ZenSystemMetaForm split across multiple files.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenSystemMetaForm
    {
        private static void drawContentGroupExec(Type? t, float labelWidth = 70)
        {
            if (t == null) return;
            
            // Group & Phase (Fixed/Variable/Presentation)
            ZenUtil.ResolveSystemGroupAndPhase(t, out var group, out var phase);

            var groupLabel = group.ToString();

            // Execution Group + representative interface
            var execLabel = "Unknown";

            if (group is SystemGroup.FrameInput or SystemGroup.FrameSync or SystemGroup.FrameView or SystemGroup.FrameUI)
            {
                execLabel = "Non-deterministic";
            }
            else if (group is SystemGroup.FixedInput or SystemGroup.FixedDecision or SystemGroup.FixedSimulation or SystemGroup.FixedPost)
            {
                execLabel = "Deterministic";
            }
            
            // Top: System name + Namespace + Ping icon
            var ns = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Display name + namespace as one block aligned to the left
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(t.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"[{ns}]", ZenGUIStyles.LabelMLNormal9Gray);
                }

                var line = EditorGUIUtility.singleLineHeight;
                var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));
                var marginRight = new Rect(r.xMax - 20, r.y, 20, r.height);
                
                var searchContent = ZenGUIContents.IconPing();
                if (GUI.Button(marginRight, searchContent, EditorStyles.iconButton))
                {
                    ZenUtil.PingType(t);
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(groupLabel, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Execution", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(execLabel, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif