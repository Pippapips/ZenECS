#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;
using ZenECS.EditorUtils;

namespace ZenECS.EditorWindows
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

        static void ResolveGroupAndPhase(Type t, out SystemGroup group, out PhaseKind phase)
        {
            group = SystemUtil.ResolveGroup(t);

            switch (group)
            {
                // 고정 틱 = Deterministic
                case SystemGroup.FixedInput:
                case SystemGroup.FixedDecision:
                case SystemGroup.FixedSimulation:
                case SystemGroup.FixedPost:
                    phase = PhaseKind.Deterministic;
                    break;

                // 프레임 기반 = Non-deterministic
                case SystemGroup.FrameInput:
                case SystemGroup.FrameSync:
                case SystemGroup.FrameView:
                case SystemGroup.FrameUI:
                    phase = PhaseKind.NonDeterministic;
                    break;

                default:
                    // 혹시 그룹이 지정 안돼있으면 Non-deterministic 쪽에 묶어두기
                    phase = PhaseKind.Unknown;
                    break;
            }
        }

        void DrawSystemRow(int index, ISystem sys, Type tSys, IWorld? world)
        {
            var typeName = tSys.Name;

            bool hasEnabled = sys is ISystemEnabledFlag;
            bool enabledValue = hasEnabled && ((ISystemEnabledFlag)sys).Enabled;

            // === 한 줄 Rect 계산 ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Indent 반영
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float pauseW = 24f;
            const float iconW = 24f; // 돋보기 / X 공통 폭
            const float gap = 1f;

            // 왼쪽: Pause
            var pauseRect = new Rect(rowRect.x, rowRect.y, pauseW, rowRect.height);

            // 오른쪽 끝: X
            var delRect = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);

            // 그 왼쪽: 돋보기
            var pingRect = new Rect(delRect.x - gap - iconW, rowRect.y, iconW, rowRect.height);

            // 가운데: System 버튼
            float sysX = pauseRect.xMax + gap;
            float sysRight = pingRect.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Pause (Enabled 토글) =====
            using (new EditorGUI.DisabledScope(!hasEnabled))
            {
                var btnRect = new Rect(
                    pauseRect.x,
                    pauseRect.y + 1f,
                    pauseRect.width,
                    pauseRect.height - 2f
                );

                var pauseContent = EditorGUIUtility.IconContent("PauseButton");
                if (pauseContent == null || pauseContent.image == null)
                    pauseContent = EditorGUIUtility.TrTextContent("⏸");

                var pauseStyle = new GUIStyle("Button")
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(3, 3, 3, 3),
                    margin = new RectOffset(0, 0, 0, 0)
                };

                var oldBg = GUI.backgroundColor;
                var oldCont = GUI.contentColor;

                if (hasEnabled && !enabledValue)
                {
                    GUI.backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.24f, 0.48f, 0.90f, 1f)
                        : new Color(0.20f, 0.45f, 0.90f, 1f);
                    GUI.contentColor = Color.white;
                }

                if (GUI.Button(btnRect, pauseContent, pauseStyle))
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

            // ===== System 버튼 (Watched Count) =====
            var watchedCount = WatchQueryRunner.TryCountByWatch(sys, world);
            if (watchedCount > 0)
                typeName = $"{typeName} ({watchedCount})";

            GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.button);
            centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            centeredLabelStyle.fontStyle = FontStyle.Normal;
            centeredLabelStyle.fontSize = 10;

            bool selected = _systemTree.SelectedSystemIndex == index;
            bool clicked = GUI.Toggle(sysRect, selected, typeName, centeredLabelStyle);
            if (clicked && !selected)
            {
                ClearState();
                _systemTree.SelectedSystemIndex = index;
            }

            // ===== 돋보기 버튼 (Ping, Selection 변경 없음) =====
            {
                var pingBtnRect = new Rect(
                    pingRect.x,
                    pingRect.y + 1f,
                    pingRect.width,
                    pingRect.height - 2f
                );

                var searchContent = GetSearchIconContent("Ping system script asset");
                var iconStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(3, 3, 3, 3),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 10
                };

                if (GUI.Button(pingBtnRect, searchContent, iconStyle))
                {
                    PingSystemTypeNoSelect(tSys);
                }
            }

            // ===== X 삭제 버튼 =====
            using (new EditorGUI.DisabledScope(!_coreState.EditMode))
            {
                var delBtnRect = new Rect(
                    delRect.x,
                    delRect.y + 1f,
                    delRect.width,
                    delRect.height - 2f
                );

                var delContent = new GUIContent("X", "Remove this System from the current World");
                var delStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };

                if (GUI.Button(delBtnRect, delContent, delStyle) && world != null)
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

        void DrawGroupSection(
            SystemGroup group,
            string label,
            Dictionary<SystemGroup, Dictionary<PhaseKind, List<(int index, ISystem sys, Type type)>>> tree,
            IWorld? world)
        {
            GUIStyle systemTreeToggleStyle = new GUIStyle(EditorStyles.foldout);
            systemTreeToggleStyle.fontStyle = FontStyle.Normal;
            systemTreeToggleStyle.fontSize = 11;
            systemTreeToggleStyle.richText = false;
            systemTreeToggleStyle.alignment = TextAnchor.MiddleLeft;
            systemTreeToggleStyle.focused.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onFocused.textColor = systemTreeTextColor;
            systemTreeToggleStyle.hover.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onHover.textColor = systemTreeTextColor;
            systemTreeToggleStyle.active.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onActive.textColor = systemTreeTextColor;
            systemTreeToggleStyle.normal.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onNormal.textColor = systemTreeTextColor;

            if (!tree.TryGetValue(group, out var phaseMap) ||
                phaseMap.Values.All(l => l == null || l.Count == 0))
            {
                // 빈 그룹은 회색으로 비활성 Foldout 한 줄만
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, label, true, systemTreeToggleStyle);
                }

                return;
            }

            if (!_systemTree.GroupFold.TryGetValue(group, out var openGroup))
                openGroup = true;

            openGroup = EditorGUILayout.Foldout(openGroup, label, true, systemTreeToggleStyle);
            _systemTree.GroupFold[group] = openGroup;
            if (!openGroup) return;

            // 여기서는 "바로 위 Foldout 헤더"와 Leaf의 x를 맞추는 게 목표

            // if (group == SystemGroup.FrameView)
            // {
            //     // Presentation: 그룹 헤더와 System 버튼이 같은 x에서 시작하도록
            //     if (phaseMap.TryGetValue(PhaseKind.Presentation, out var list) && list.Count > 0)
            //     {
            //         foreach (var (index, sys, type) in list)
            //             DrawSystemRow(index, sys, type, world);
            //     }
            // }
            // else
            // {
            //     // FrameSetup / Simulation:
            //     //  - Group 헤더는 기본 indent
            //     //  - 그 안의 Phase 헤더(Variable/Fixed)와 System 버튼은 indent + 1로 동일
            //     EditorGUI.indentLevel++;
            //     DrawPhaseSection(group, PhaseKind.Variable, "Variable", phaseMap, world);
            //     DrawPhaseSection(group, PhaseKind.Fixed, "Fixed", phaseMap, world);
            //     EditorGUI.indentLevel--;
            // }
        }

        void DrawPhaseSection(
            SystemGroup group,
            PhaseKind phase,
            string label,
            Dictionary<PhaseKind, List<(int index, ISystem sys, Type type)>> phaseMap,
            IWorld? world)
        {
            if (!phaseMap.TryGetValue(phase, out var list) || list.Count == 0)
                return;

            GUIStyle systemTreeToggleStyle = new GUIStyle(EditorStyles.foldout);
            systemTreeToggleStyle.fontStyle = FontStyle.Normal;
            systemTreeToggleStyle.fontSize = 11;
            systemTreeToggleStyle.richText = false;
            systemTreeToggleStyle.alignment = TextAnchor.MiddleLeft;
            systemTreeToggleStyle.focused.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onFocused.textColor = systemTreeTextColor;
            systemTreeToggleStyle.hover.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onHover.textColor = systemTreeTextColor;
            systemTreeToggleStyle.active.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onActive.textColor = systemTreeTextColor;
            systemTreeToggleStyle.normal.textColor = systemTreeTextColor;
            systemTreeToggleStyle.onNormal.textColor = systemTreeTextColor;

            var key = (group, phase);
            if (!_systemTree.PhaseFold.TryGetValue(key, out var openPhase))
                openPhase = true;

            openPhase = EditorGUILayout.Foldout(openPhase, label, true, systemTreeToggleStyle);
            _systemTree.PhaseFold[key] = openPhase;
            if (!openPhase) return;

            // Phase 헤더와 System 버튼이 같은 x에서 시작하도록 추가 들여쓰기 제거
            foreach (var (index, sys, type) in list)
                DrawSystemRow(index, sys, type, world);
        }

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
                ResolveGroupAndPhase(t, out var group, out var phase);

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

            // Foldout 스타일
            GUIStyle foldStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 11,
                richText = false,
                alignment = TextAnchor.MiddleLeft
            };
            foldStyle.focused.textColor = systemTreeTextColor;
            foldStyle.onFocused.textColor = systemTreeTextColor;
            foldStyle.hover.textColor = systemTreeTextColor;
            foldStyle.onHover.textColor = systemTreeTextColor;
            foldStyle.active.textColor = systemTreeTextColor;
            foldStyle.onActive.textColor = systemTreeTextColor;
            foldStyle.normal.textColor = systemTreeTextColor;
            foldStyle.onNormal.textColor = systemTreeTextColor;

            // ─────────────────────────────────────────
            // 1. Deterministic
            // ─────────────────────────────────────────
            if (!groupTree.TryGetValue(PhaseKind.Deterministic, out var detGroups) ||
                detGroups.Values.All(l => l == null || l.Count == 0))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Deterministic", true, foldStyle);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;

                FoldoutHeader(ref _systemTree.DeterministicFold, "Deterministic", null, null, foldStyle);

                if (_systemTree.DeterministicFold)
                {
                    EditorGUI.indentLevel++;

                    DrawGroupLeaf(SystemGroup.FixedInput, "Input", detGroups, world, foldStyle);
                    DrawGroupLeaf(SystemGroup.FixedDecision, "Decision", detGroups, world, foldStyle);
                    DrawGroupLeaf(SystemGroup.FixedSimulation, "Simulation", detGroups, world, foldStyle);
                    DrawGroupLeaf(SystemGroup.FixedPost, "Post", detGroups, world, foldStyle);

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
                    EditorGUILayout.Foldout(false, "Non-deterministic", true, foldStyle);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;
                _systemTree.NonDeterministicFold =
                    EditorGUILayout.Foldout(_systemTree.NonDeterministicFold, "Non-deterministic", true, foldStyle);

                if (_systemTree.NonDeterministicFold)
                {
                    EditorGUI.indentLevel++;

                    // 2-1. Begin (Input + Sync)
                    bool hasBegin = HasAny(nonDetGroups, SystemGroup.FrameInput, SystemGroup.FrameSync);
                    if (hasBegin)
                    {
                        _systemTree.BeginFold = EditorGUILayout.Foldout(_systemTree.BeginFold, "Begin", true, foldStyle);
                        if (_systemTree.BeginFold)
                        {
                            EditorGUI.indentLevel++;
                            DrawGroupLeaf(SystemGroup.FrameInput, "Input", nonDetGroups, world, foldStyle);
                            DrawGroupLeaf(SystemGroup.FrameSync, "Sync", nonDetGroups, world, foldStyle);
                            EditorGUI.indentLevel--;
                        }
                    }

                    // 2-2. Late (View + UI)
                    bool hasLate = HasAny(nonDetGroups, SystemGroup.FrameView, SystemGroup.FrameUI);
                    if (hasLate)
                    {
                        _systemTree.LateFold = EditorGUILayout.Foldout(_systemTree.LateFold, "Late", true, foldStyle);
                        if (_systemTree.LateFold)
                        {
                            EditorGUI.indentLevel++;
                            DrawGroupLeaf(SystemGroup.FrameView, "View", nonDetGroups, world, foldStyle);
                            DrawGroupLeaf(SystemGroup.FrameUI, "UI", nonDetGroups, world, foldStyle);
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
                    // GetAllSingletons가 예외를 던져도 다른 UI에 영향을 주지 않도록 방어
                    Debug.LogException(ex);
                }

                if (singletons != null)
                {
                    var singletonList = singletons.ToList();
                    if (singletonList.Count > 0)
                    {
                        EditorGUI.indentLevel = 0;
                        _systemTree.SingletonsFold = EditorGUILayout.Foldout(_systemTree.SingletonsFold, "Singletons", true, foldStyle);
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
                            EditorGUILayout.Foldout(false, "Singletons", true, foldStyle);
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Foldout(false, "Singletons", true, foldStyle);
                    }
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Foldout(false, "Singletons", true, foldStyle);
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
                    EditorGUILayout.Foldout(false, "Unknown", true, foldStyle);
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;
                _systemTree.UnknownFold = EditorGUILayout.Foldout(_systemTree.UnknownFold, "Unknown", true, foldStyle);

                if (_systemTree.UnknownFold)
                {
                    EditorGUI.indentLevel++;

                    // Unknown 하위는 루트 하나: Unknown 섹션 안에 바로 시스템 리스트
                    if (unknownGroups.TryGetValue(SystemGroup.Unknown, out var list) && list != null)
                    {
                        foreach (var (index, sys, type) in list)
                            DrawSystemRow(index, sys, type, world);
                    }
                    else
                    {
                        // 혹시 다른 그룹 키로 들어온 경우가 있으면 전부 flatten 해서 출력
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

        bool HasAny(Dictionary<SystemGroup, List<(int, ISystem, Type)>> map, params SystemGroup[] groups)
        {
            foreach (var g in groups)
            {
                if (map.TryGetValue(g, out var list) && list != null && list.Count > 0)
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

        // =====================================================================
        //  SYSTEM PRESET PICKER (우측 상단 메뉴)
        // =====================================================================

        void ShowSystemPresetPicker(Rect activatorRectGui, IWorld world)
        {
            ZenSystemPresetPickerWindow.Show(
                activatorRectGui,
                onPick: preset =>
                {
                    var resolver = ZenEcsUnityBridge.SystemPresetResolver;
                    if (resolver == null)
                    {
                        EditorUtility.DisplayDialog(
                            "SystemPresetResolver missing",
                            "ZenEcsUnityBridge.SystemPresetResolver is null.\nPlease configure a SystemPresetResolver.",
                            "OK");
                        return;
                    }

                    try
                    {
                        // SystemsPreset.GetValidTypes() 리플렉션으로 호출
                        var mi = preset.GetType().GetMethod(
                            "GetValidTypes",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            Type.EmptyTypes,
                            null);

                        if (mi == null)
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid SystemsPreset",
                                $"SystemsPreset '{preset.name}' must define GetValidTypes() returning IEnumerable<Type>.",
                                "OK");
                            return;
                        }

                        var ret = mi.Invoke(preset, null);
                        if (ret is not IEnumerable<Type> validTypes)
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid SystemsPreset",
                                $"GetValidTypes() of '{preset.name}' must return IEnumerable<Type>.",
                                "OK");
                            return;
                        }

                        // ZenEcsUnityBridge.SystemPresetResolver?.InstantiateSystems(...)
                        var systems = resolver.InstantiateSystems(validTypes.ToList());
                        if (systems == null)
                        {
                            EditorUtility.DisplayDialog(
                                "InstantiateSystems returned null",
                                "SystemPresetResolver.InstantiateSystems returned null.",
                                "OK");
                            return;
                        }

                        // world.AddSystems(systems)로 일괄 등록
                        world.AddSystems(systems);

                        // 캐시 클리어 & UI 갱신
                        ClearState();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog(
                            "Add System Preset failed",
                            $"Failed to apply SystemsPreset '{preset.name}'.\nSee Console for details.",
                            "OK");
                    }
                },
                title: "Add System Preset"
            );
        }
    }
}
#endif
