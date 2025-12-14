// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerWindow.SystemTree.cs
// Purpose: System tree panel implementation for ZenECS Explorer window,
//          displaying hierarchical view of registered systems grouped by phase.
// Key concepts:
//   • System hierarchy: foldable tree organized by system groups/phases.
//   • Selection handling: tracks selected system for entity filtering.
//   • Partial class: part of ZenEcsExplorerWindow split across multiple files.
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
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Left side systems tree panel of the ZenECS Explorer:
    /// - System group/phase tree
    /// - Unknown/Non-deterministic grouping
    /// - Integrates with Singletons section.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        // =====================================================================
        //  SYSTEM TREE HELPERS
        // =====================================================================

        void DrawSystemTree(IReadOnlyList<ISystem> systems, IWorld? world)
        {
            // PhaseKind(Deterministic / NonDeterministic / Unknown)
            //  └ SystemGroup → (index, sys, type)
            var groupTree =
                new Dictionary<PhaseKind, Dictionary<SystemGroup, List<(int index, ISystem sys, Type type)>>>();

            for (int i = 0; i < systems.Count; i++)
            {
                var sys = systems[i];
                if (sys == null) continue;

                var t = sys.GetType();
                ZenUtil.ResolveSystemGroupAndPhase(t, out var group, out var phase);

                if (!groupTree.TryGetValue(phase, out var phaseMap))
                {
                    phaseMap = new Dictionary<SystemGroup, List<(int, ISystem, Type)>>();
                    groupTree[phase] = phaseMap;
                }

                if (!phaseMap.TryGetValue(group, out var list))
                {
                    list = new List<(int, ISystem, Type)>();
                    phaseMap[group] = list;
                }

                list.Add((i, sys, t));
            }

            // ─────────────────────────────────────────
            // 1. Deterministic
            // ─────────────────────────────────────────
            if (!groupTree.TryGetValue(PhaseKind.Deterministic, out var detGroups) ||
                detGroups.Values.All(l => l == null || l.Count == 0))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Deterministic", true, ZenGUIStyles.SystemFoldout);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;

                _systemTree.DeterministicFold = EditorGUILayout.Foldout(_systemTree.DeterministicFold, "Deterministic", true, ZenGUIStyles.SystemFoldout10);
                if (_systemTree.DeterministicFold)
                {
                    EditorGUI.indentLevel++;

                    DrawGroupLeaf(SystemGroup.FixedInput, "Input", detGroups, world, ZenGUIStyles.SystemFoldout);
                    DrawGroupLeaf(SystemGroup.FixedDecision, "Decision", detGroups, world, ZenGUIStyles.SystemFoldout);
                    DrawGroupLeaf(SystemGroup.FixedSimulation, "Simulation", detGroups, world, ZenGUIStyles.SystemFoldout);
                    DrawGroupLeaf(SystemGroup.FixedPost, "Post", detGroups, world, ZenGUIStyles.SystemFoldout);

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // ─────────────────────────────────────────
            // 2. Non-deterministic
            // ─────────────────────────────────────────
            if (!groupTree.TryGetValue(PhaseKind.NonDeterministic, out var nonDetGroups) ||
                nonDetGroups.Values.All(l => l == null || l.Count == 0))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Non-deterministic", true, ZenGUIStyles.SystemFoldout);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;
                _systemTree.NonDeterministicFold =
                    EditorGUILayout.Foldout(_systemTree.NonDeterministicFold, "Non-deterministic", true, ZenGUIStyles.SystemFoldout);

                if (_systemTree.NonDeterministicFold)
                {
                    EditorGUI.indentLevel++;

                    // 2-1. Begin (Input + Sync)
                    bool hasBegin = HasAny(nonDetGroups, SystemGroup.FrameInput, SystemGroup.FrameSync);
                    if (hasBegin)
                    {
                        _systemTree.BeginFold = EditorGUILayout.Foldout(_systemTree.BeginFold, "Begin", true, ZenGUIStyles.SystemFoldout);
                        if (_systemTree.BeginFold)
                        {
                            EditorGUI.indentLevel++;
                            DrawGroupLeaf(SystemGroup.FrameInput, "Input", nonDetGroups, world, ZenGUIStyles.SystemFoldout);
                            DrawGroupLeaf(SystemGroup.FrameSync, "Sync", nonDetGroups, world, ZenGUIStyles.SystemFoldout);
                            EditorGUI.indentLevel--;
                        }
                    }

                    // 2-2. Late (View + UI)
                    bool hasLate = HasAny(nonDetGroups, SystemGroup.FrameView, SystemGroup.FrameUI);
                    if (hasLate)
                    {
                        _systemTree.LateFold = EditorGUILayout.Foldout(_systemTree.LateFold, "Late", true, ZenGUIStyles.SystemFoldout);
                        if (_systemTree.LateFold)
                        {
                            EditorGUI.indentLevel++;
                            DrawGroupLeaf(SystemGroup.FrameView, "View", nonDetGroups, world, ZenGUIStyles.SystemFoldout);
                            DrawGroupLeaf(SystemGroup.FrameUI, "UI", nonDetGroups, world, ZenGUIStyles.SystemFoldout);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);

            // ─────────────────────────────────────────
            // 3. Singletons
            // ─────────────────────────────────────────
            if (world != null)
            {
                IEnumerable<(Type type, Entity owner)>? singletons = null;
                try
                {
                    singletons = world.GetAllSingletons();
                }
                catch (Exception ex)
                {
                    // Defensive: prevent GetAllSingletons exception from affecting other UI
                    Debug.LogException(ex);
                }

                if (singletons != null)
                {
                    var singletonList = singletons.ToList();
                    if (singletonList.Count > 0)
                    {
                        EditorGUI.indentLevel = 0;
                        _systemTree.SingletonsFold = EditorGUILayout.Foldout(_systemTree.SingletonsFold, "Singletons", true, ZenGUIStyles.SystemFoldout);
                        if (_systemTree.SingletonsFold)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var (type, owner) in singletonList)
                            {
                                DrawSingletonRow(type, owner, world);
                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.Foldout(false, "Singletons", true, ZenGUIStyles.SystemFoldout);
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Foldout(false, "Singletons", true, ZenGUIStyles.SystemFoldout);
                    }
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Singletons", true, ZenGUIStyles.SystemFoldout);
                }
            }

            EditorGUILayout.Space(4);

            // ─────────────────────────────────────────
            // 4. Unknown (SystemGroup.Unknown)
            // ─────────────────────────────────────────
            if (!groupTree.TryGetValue(PhaseKind.Unknown, out var unknownGroups) ||
                unknownGroups.Values.All(l => l == null || l.Count == 0))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Unknown", true, ZenGUIStyles.SystemFoldout);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;
                _systemTree.UnknownFold = EditorGUILayout.Foldout(_systemTree.UnknownFold, "Unknown", true, ZenGUIStyles.SystemFoldout);

                if (_systemTree.UnknownFold)
                {
                    EditorGUI.indentLevel++;

                    // Unknown has one root: system list directly inside Unknown section
                    if (unknownGroups.TryGetValue(SystemGroup.Unknown, out var list) && list != null)
                    {
                        foreach (var (index, sys, type) in list)
                            DrawSystemRow(index, sys, type, world);
                    }
                    else
                    {
                        // If there are cases with different group keys, flatten all and output
                        foreach (var kv in unknownGroups)
                        {
                            var list2 = kv.Value;
                            if (list2 == null) continue;
                            foreach (var (index, sys, type) in list2)
                                DrawSystemRow(index, sys, type, world);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel = 0;
        }

        void DrawSystemRow(int index, ISystem sys, Type tSys, IWorld? world)
        {
            var typeName = tSys.Name;

            bool hasEnabled = sys is ISystemEnabledFlag;
            bool enabledValue = hasEnabled && ((ISystemEnabledFlag)sys).Enabled;

            // === Calculate one-line Rect ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Reflect indent
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float pauseW = 24f;
            const float iconW = 24f; // Ping / X common width
            const float gap = 1f;

            // Left: Pause
            var pauseRect = new Rect(rowRect.x, rowRect.y, pauseW, rowRect.height);

            // Right end: X
            var delRect = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);

            // Left of that: Ping
            var pingRect = new Rect(delRect.x - gap - iconW, rowRect.y, iconW, rowRect.height);

            // Middle: System button
            float sysX = pauseRect.xMax + gap;
            float sysRight = pingRect.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Pause (Enabled toggle) =====
            using (new EditorGUI.DisabledScope(!hasEnabled))
            {
                var btnRect = new Rect(
                    pauseRect.x,
                    pauseRect.y + 1f,
                    pauseRect.width,
                    pauseRect.height - 2f
                );

                var oldBg = GUI.backgroundColor;
                var oldCont = GUI.contentColor;

                if (hasEnabled && !enabledValue)
                {
                    GUI.backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.24f, 0.48f, 0.90f, 1f)
                        : new Color(0.20f, 0.45f, 0.90f, 1f);
                    GUI.contentColor = Color.white;
                }

                if (GUI.Button(btnRect, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    if (hasEnabled)
                    {
                        var flag = (ISystemEnabledFlag)sys;
                        flag.Enabled = !flag.Enabled;
                    }
                }

                GUI.backgroundColor = oldBg;
                GUI.contentColor = oldCont;
            }

            bool selected = _systemTree.SelectedSystemIndex == index;
            bool clicked = GUI.Toggle(sysRect, selected, typeName, ZenGUIStyles.ButtonMLNormal10);
            if (clicked && !selected)
            {
                ClearState(true, false);
                _systemTree.SelectedSystemIndex = index;
            }

            var pingBtnRect = new Rect(
                pingRect.x,
                pingRect.y + 1f,
                pingRect.width,
                pingRect.height - 2f
            );

            if (GUI.Button(pingBtnRect, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
            {
                ZenUtil.PingType(tSys);
            }

            // ===== X Delete button =====
            using (new EditorGUI.DisabledScope(!_coreState.EditMode))
            {
                var delBtnRect = new Rect(
                    delRect.x,
                    delRect.y + 1f,
                    delRect.width,
                    delRect.height - 2f
                );

                var delContent = new GUIContent("X", "Remove this System from the current World");
                if (GUI.Button(delBtnRect, delContent, ZenGUIStyles.ButtonMCNormal10) && world != null)
                {
                    var sysName = tSys.Name;
                    if (EditorUtility.DisplayDialog(
                            "Remove System",
                            $"Remove system '{sysName}' from the current World?",
                            "Remove",
                            "Cancel"))
                    {
                        world.RemoveSystem(tSys);

                        ClearState();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        
        bool HasAny(Dictionary<SystemGroup, List<(int, ISystem, Type)>> map, params SystemGroup[] groups)
        {
            foreach (var g in groups)
            {
                if (map.TryGetValue(g, out var list) && list is { Count: > 0 })
                    return true;
            }

            return false;
        }

        void DrawGroupLeaf(
            SystemGroup group,
            string label,
            Dictionary<SystemGroup, List<(int index, ISystem sys, Type type)>> map,
            IWorld? world,
            GUIStyle foldStyle)
        {
            if (!map.TryGetValue(group, out var list) || list.Count == 0)
                return;

            if (!_systemTree.GroupFold.TryGetValue(group, out var open))
                open = true;

            open = EditorGUILayout.Foldout(open, label, true, foldStyle);
            _systemTree.GroupFold[group] = open;
            if (!open) return;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            foreach (var (index, sys, type) in list)
                DrawSystemRow(index, sys, type, world);

            EditorGUI.indentLevel = prevIndent;
        }
    }
}
#endif
