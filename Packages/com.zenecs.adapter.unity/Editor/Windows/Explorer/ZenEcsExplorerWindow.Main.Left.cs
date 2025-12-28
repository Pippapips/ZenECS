// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerWindow.Main.Left.cs
// Purpose: Left panel implementation for ZenECS Explorer window main layout,
//          containing the system tree view.
// Key concepts:
//   • System tree panel: displays hierarchical system view.
//   • Partial class: part of ZenEcsExplorerWindow split across multiple files.
//   • Editor-only: compiled out in player builds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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
        /// <summary>
        /// ViewModel for the left system tree panel.
        /// </summary>
        [Serializable]
        sealed class ExplorerSystemTreeState
        {
            // Selected system
            public int SelectedSystemIndex = -1;
            public int SelectedSystemEntityCount;

            // Scroll
            public Vector2 Scroll;

            // System Tree Foldout
            public readonly Dictionary<SystemGroup, bool> GroupFold = new();
            public readonly Dictionary<(SystemGroup group, PhaseKind phase), bool> PhaseFold = new();

            public bool DeterministicFold = true;
            public bool NonDeterministicFold = true;
            public bool BeginFold = true;
            public bool LateFold = true;
            public bool UnknownFold = true;
            public bool SingletonsFold = true;

            /// <summary>
            /// Reset selection and counters.
            /// </summary>
            public void ClearSelection()
            {
                SelectedSystemIndex = -1;
                SelectedSystemEntityCount = 0;
            }

            /// <summary>
            /// Clear cached tree foldouts.
            /// </summary>
            public void ClearTreeFoldouts()
            {
                GroupFold.Clear();
                PhaseFold.Clear();
            }
        }

        private void DrawLeftSystemTreePanel()
        {
            if (_world == null) return;
            
            using var sv = new EditorGUILayout.ScrollViewScope(_systemTree.Scroll, GUILayout.Width(300));
            _systemTree.Scroll = sv.scrollPosition;

            EditorGUILayout.Space(4);

            // Header + ClearSelection button
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Systems", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                var buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.LowerCenter
                };

                if (GUILayout.Button(new GUIContent("R", "Clear Selection"),
                        buttonStyle, GUILayout.Width(24), GUILayout.Height(24)))
                {
                    _systemTree.ClearSelection();
                    _entityPanel.ClearSelection();
                }
            }

            var systems = _world.GetAllSystems();
            if (systems.Count == 0)
            {
                EditorGUILayout.HelpBox("No systems registered.", MessageType.Info);
            }
            else
            {
                DrawSystemTree(systems, _world);
            }

            EditorGUILayout.Space(4);
        }
    }
}