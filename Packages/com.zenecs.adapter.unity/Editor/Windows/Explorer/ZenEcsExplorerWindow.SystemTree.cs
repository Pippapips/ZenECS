// #if UNITY_EDITOR
// #nullable enable
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;
// using ZenECS.Core;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.EditorWindows
// {
//     public sealed partial class ZenEcsExplorerWindow
//     {
//         // 트리 상태
//         bool _deterministicFold   = true;
//         bool _nonDeterministicFold = true;
//         bool _unknownFold         = true;
//         bool _singletonsFold      = true;
//         bool _beginFold           = true;
//         bool _lateFold            = true;
//
//         readonly Dictionary<SystemGroup, bool> _groupFold = new();
//
//         string  _systemFilter = string.Empty;
//         Vector2 _systemScroll;
//
//         // 선택 상태
//         ISystem? _selectedSystem;
//         Type?    _selectedSystemType;
//         Entity?  _selectedSingletonOwner;
//
//         /// <summary>
//         /// World 변경 시 System 트리 캐시를 초기화한다.
//         /// </summary>
//         private void RebuildSystemTree(IWorld? world)
//         {
//             // 이 예제 버전에서는 별도 캐시는 두지 않고, 매 프레임 world에서 systems를 가져와 사용.
//             // (성능 문제가 생기면 여기에서 캐싱 구조를 붙이면 됨)
//             _selectedSystem          = null;
//             _selectedSystemType      = null;
//             _selectedSingletonOwner  = null;
//         }
//
//         /// <summary>
//         /// 상단 툴바 + System 트리 전체 그리기.
//         /// </summary>
//         private void DrawSystemTreeArea(IWorld? world)
//         {
//             DrawSystemTreeToolbar();
//
//             using (var scroll = new EditorGUILayout.ScrollViewScope(_systemScroll))
//             {
//                 _systemScroll = scroll.scrollPosition;
//
//                 if (world == null)
//                 {
//                     EditorGUILayout.HelpBox("World is null.", MessageType.Info);
//                     return;
//                 }
//
//                 var systems = world.GetAllSystems();
//                 if (systems == null || systems.Count == 0)
//                 {
//                     EditorGUILayout.HelpBox("No systems registered in this World.", MessageType.Info);
//                     return;
//                 }
//
//                 DrawSystemTree(systems, world);
//             }
//         }
//
//         private void DrawSystemTreeToolbar()
//         {
//             using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
//             {
//                 GUILayout.Label("Systems", EditorStyles.boldLabel, GUILayout.Width(70f));
//
//                 GUILayout.Space(4f);
//
//                 var searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
//                 var cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.toolbarButton;
//
//                 GUI.SetNextControlName("SystemSearchField");
//
//                 var newFilter = GUILayout.TextField(_systemFilter, searchStyle);
//                 if (!string.Equals(newFilter, _systemFilter, StringComparison.Ordinal))
//                 {
//                     _systemFilter = newFilter;
//                 }
//
//                 if (GUILayout.Button("x", cancelStyle, GUILayout.Width(18f)))
//                 {
//                     _systemFilter = string.Empty;
//                     GUI.FocusControl(null);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// PhaseKind / SystemGroup 별로 시스템을 묶어서 트리 형태로 그리는 메인 메서드.
//         /// (원본 Explorer의 트리 로직을 간소화해서 옮겨온 버전)
//         /// </summary>
//         private void DrawSystemTree(IReadOnlyList<ISystem> systems, IWorld? world)
//         {
//             // PhaseKind(Deterministic / NonDeterministic / Unknown)
//             //  └ SystemGroup → (index, sys, type)
//             var groupTree =
//                 new Dictionary<PhaseKind, Dictionary<SystemGroup, List<(int index, ISystem sys, Type type)>>>();
//
//             for (int i = 0; i < systems.Count; i++)
//             {
//                 var sys = systems[i];
//                 if (sys == null) continue;
//
//                 var t = sys.GetType();
//                 ResolveGroupAndPhase(t, out var group, out var phase);
//
//                 if (!groupTree.TryGetValue(phase, out var phaseMap))
//                 {
//                     phaseMap = new Dictionary<SystemGroup, List<(int, ISystem, Type)>>();
//                     groupTree[phase] = phaseMap;
//                 }
//
//                 if (!phaseMap.TryGetValue(group, out var list))
//                 {
//                     list = new List<(int, ISystem, Type)>();
//                     phaseMap[group] = list;
//                 }
//
//                 list.Add((i, sys, t));
//             }
//
//             // 필터 텍스트 (소문자)
//             string filter = _systemFilter.Trim();
//             string filterLower = filter.ToLowerInvariant();
//             bool hasFilter = !string.IsNullOrEmpty(filter);
//
//             // Foldout 스타일
//             var foldStyle = new GUIStyle(EditorStyles.foldout)
//             {
//                 fontStyle = FontStyle.Normal,
//                 fontSize  = 11,
//                 richText  = false,
//                 alignment = TextAnchor.MiddleLeft
//             };
//
//             Color systemTreeTextColor = EditorStyles.label.normal.textColor;
//             foldStyle.focused.textColor    = systemTreeTextColor;
//             foldStyle.onFocused.textColor  = systemTreeTextColor;
//             foldStyle.hover.textColor      = systemTreeTextColor;
//             foldStyle.onHover.textColor    = systemTreeTextColor;
//             foldStyle.active.textColor     = systemTreeTextColor;
//             foldStyle.onActive.textColor   = systemTreeTextColor;
//             foldStyle.normal.textColor     = systemTreeTextColor;
//             foldStyle.onNormal.textColor   = systemTreeTextColor;
//
//             // ─────────────────────────────────────────
//             // 1. Deterministic (Fixed*)
//             // ─────────────────────────────────────────
//             if (!groupTree.TryGetValue(PhaseKind.Deterministic, out var detGroups) ||
//                 detGroups.Values.All(l => l == null || l.Count == 0))
//             {
//                 using (new EditorGUI.DisabledScope(true))
//                 {
//                     EditorGUILayout.Foldout(false, "Deterministic", true, foldStyle);
//                 }
//             }
//             else
//             {
//                 EditorGUI.indentLevel = 0;
//                 _deterministicFold = EditorGUILayout.Foldout(_deterministicFold, "Deterministic", true, foldStyle);
//
//                 if (_deterministicFold)
//                 {
//                     EditorGUI.indentLevel++;
//
//                     DrawGroupLeaf(SystemGroup.FixedInput,     "Fixed Input",     detGroups, world, foldStyle, hasFilter, filterLower);
//                     DrawGroupLeaf(SystemGroup.FixedDecision,  "Fixed Decision",  detGroups, world, foldStyle, hasFilter, filterLower);
//                     DrawGroupLeaf(SystemGroup.FixedSimulation,"Fixed Simulation",detGroups, world, foldStyle, hasFilter, filterLower);
//                     DrawGroupLeaf(SystemGroup.FixedPost,      "Fixed Post",      detGroups, world, foldStyle, hasFilter, filterLower);
//
//                     EditorGUI.indentLevel--;
//                 }
//             }
//
//             EditorGUILayout.Space(4);
//
//             // ─────────────────────────────────────────
//             // 2. Non-deterministic (Frame*)
//             // ─────────────────────────────────────────
//             if (!groupTree.TryGetValue(PhaseKind.NonDeterministic, out var nonDetGroups) ||
//                 nonDetGroups.Values.All(l => l == null || l.Count == 0))
//             {
//                 using (new EditorGUI.DisabledScope(true))
//                 {
//                     EditorGUILayout.Foldout(false, "Non-deterministic", true, foldStyle);
//                 }
//             }
//             else
//             {
//                 EditorGUI.indentLevel = 0;
//                 _nonDeterministicFold =
//                     EditorGUILayout.Foldout(_nonDeterministicFold, "Non-deterministic", true, foldStyle);
//
//                 if (_nonDeterministicFold)
//                 {
//                     EditorGUI.indentLevel++;
//
//                     // 2-1. Begin (Input + Sync)
//                     bool hasBegin = HasAny(nonDetGroups, SystemGroup.FrameInput, SystemGroup.FrameSync);
//                     if (hasBegin)
//                     {
//                         _beginFold = EditorGUILayout.Foldout(_beginFold, "Begin", true, foldStyle);
//                         if (_beginFold)
//                         {
//                             EditorGUI.indentLevel++;
//                             DrawGroupLeaf(SystemGroup.FrameInput, "Frame Input", nonDetGroups, world, foldStyle, hasFilter, filterLower);
//                             DrawGroupLeaf(SystemGroup.FrameSync,  "Frame Sync",  nonDetGroups, world, foldStyle, hasFilter, filterLower);
//                             EditorGUI.indentLevel--;
//                         }
//                     }
//
//                     // 2-2. Late (View + UI)
//                     bool hasLate = HasAny(nonDetGroups, SystemGroup.FrameView, SystemGroup.FrameUI);
//                     if (hasLate)
//                     {
//                         _lateFold = EditorGUILayout.Foldout(_lateFold, "Late", true, foldStyle);
//                         if (_lateFold)
//                         {
//                             EditorGUI.indentLevel++;
//                             DrawGroupLeaf(SystemGroup.FrameView, "Frame View", nonDetGroups, world, foldStyle, hasFilter, filterLower);
//                             DrawGroupLeaf(SystemGroup.FrameUI,   "Frame UI",   nonDetGroups, world, foldStyle, hasFilter, filterLower);
//                             EditorGUI.indentLevel--;
//                         }
//                     }
//
//                     // 2-3. 기타 Frame 그룹
//                     foreach (var kv in nonDetGroups.OrderBy(kv => kv.Key))
//                     {
//                         var g = kv.Key;
//                         if (g == SystemGroup.FrameInput ||
//                             g == SystemGroup.FrameSync  ||
//                             g == SystemGroup.FrameView  ||
//                             g == SystemGroup.FrameUI)
//                             continue;
//
//                         string label = g.ToString();
//                         DrawGroupLeaf(g, label, nonDetGroups, world, foldStyle, hasFilter, filterLower);
//                     }
//
//                     EditorGUI.indentLevel--;
//                 }
//             }
//
//             EditorGUILayout.Space(4);
//
//             // ─────────────────────────────────────────
//             // 3. Singletons
//             // ─────────────────────────────────────────
//             if (world != null)
//             {
//                 IEnumerable<(Type type, Entity owner)>? singletons = null;
//                 try
//                 {
//                     singletons = world.GetAllSingletons();
//                 }
//                 catch (Exception ex)
//                 {
//                     Debug.LogException(ex);
//                 }
//
//                 if (singletons != null)
//                 {
//                     var singletonList = singletons.ToList();
//                     if (singletonList.Count > 0)
//                     {
//                         EditorGUI.indentLevel = 0;
//                         _singletonsFold = EditorGUILayout.Foldout(_singletonsFold, "Singletons", true, foldStyle);
//                         if (_singletonsFold)
//                         {
//                             EditorGUI.indentLevel++;
//                             foreach (var (type, owner) in singletonList)
//                             {
//                                 DrawSingletonRow(type, owner, world, hasFilter, filterLower);
//                             }
//                             EditorGUI.indentLevel--;
//                         }
//                     }
//                     else
//                     {
//                         using (new EditorGUI.DisabledScope(true))
//                         {
//                             EditorGUILayout.Foldout(false, "Singletons", true, foldStyle);
//                         }
//                     }
//                 }
//             }
//
//             EditorGUILayout.Space(4);
//
//             // ─────────────────────────────────────────
//             // 4. Unknown
//             // ─────────────────────────────────────────
//             if (!groupTree.TryGetValue(PhaseKind.Unknown, out var unknownGroups) ||
//                 unknownGroups.Values.All(l => l == null || l.Count == 0))
//             {
//                 using (new EditorGUI.DisabledScope(true))
//                 {
//                     EditorGUILayout.Foldout(false, "Unknown", true, foldStyle);
//                 }
//             }
//             else
//             {
//                 EditorGUI.indentLevel = 0;
//                 _unknownFold = EditorGUILayout.Foldout(_unknownFold, "Unknown", true, foldStyle);
//
//                 if (_unknownFold)
//                 {
//                     EditorGUI.indentLevel++;
//
//                     if (unknownGroups.TryGetValue(SystemGroup.Unknown, out var list) && list != null)
//                     {
//                         foreach (var (index, sys, type) in list)
//                             DrawSystemRow(index, sys, type, world, hasFilter, filterLower);
//                     }
//                     else
//                     {
//                         // 혹시 다른 그룹 키로 들어온 경우가 있으면 전부 flatten 해서 출력
//                         foreach (var kv in unknownGroups)
//                         {
//                             var list2 = kv.Value;
//                             if (list2 == null) continue;
//                             foreach (var (index, sys, type) in list2)
//                                 DrawSystemRow(index, sys, type, world, hasFilter, filterLower);
//                         }
//                     }
//
//                     EditorGUI.indentLevel--;
//                 }
//             }
//
//             EditorGUI.indentLevel = 0;
//         }
//
//         private static bool HasAny(Dictionary<SystemGroup, List<(int, ISystem, Type)>> map, params SystemGroup[] groups)
//         {
//             foreach (var g in groups)
//             {
//                 if (map.TryGetValue(g, out var list) && list != null && list.Count > 0)
//                     return true;
//             }
//             return false;
//         }
//
//         private void DrawGroupLeaf(
//             SystemGroup group,
//             string label,
//             Dictionary<SystemGroup, List<(int index, ISystem sys, Type type)>> map,
//             IWorld? world,
//             GUIStyle foldStyle,
//             bool hasFilter,
//             string filterLower)
//         {
//             if (!map.TryGetValue(group, out var list) || list.Count == 0)
//                 return;
//
//             if (!_groupFold.TryGetValue(group, out var open))
//                 open = true;
//
//             open = EditorGUILayout.Foldout(open, label, true, foldStyle);
//             _groupFold[group] = open;
//             if (!open) return;
//
//             int prevIndent = EditorGUI.indentLevel;
//             EditorGUI.indentLevel++;
//
//             foreach (var (index, sys, type) in list)
//                 DrawSystemRow(index, sys, type, world, hasFilter, filterLower);
//
//             EditorGUI.indentLevel = prevIndent;
//         }
//
//         /// <summary>
//         /// System 타입 → Group/Phase 결정.
//         /// (원본의 SystemUtil.ResolveGroup + PhaseKind 매핑을 그대로 사용)
//         /// </summary>
//         private static void ResolveGroupAndPhase(Type t, out SystemGroup group, out PhaseKind phase)
//         {
//             group = SystemUtil.ResolveGroup(t);
//
//             switch (group)
//             {
//                 // 고정 틱 = Deterministic
//                 case SystemGroup.FixedInput:
//                 case SystemGroup.FixedDecision:
//                 case SystemGroup.FixedSimulation:
//                 case SystemGroup.FixedPost:
//                     phase = PhaseKind.Deterministic;
//                     break;
//
//                 // 프레임 기반 = Non-deterministic
//                 case SystemGroup.FrameInput:
//                 case SystemGroup.FrameSync:
//                 case SystemGroup.FrameView:
//                 case SystemGroup.FrameUI:
//                     phase = PhaseKind.NonDeterministic;
//                     break;
//
//                 // 미지정 = Unknown
//                 case SystemGroup.Unknown:
//                 default:
//                     phase = PhaseKind.Unknown;
//                     break;
//             }
//         }
//
//         private void DrawSystemRow(
//             int index,
//             ISystem sys,
//             Type tSys,
//             IWorld? world,
//             bool hasFilter,
//             string filterLower)
//         {
//             string typeName = tSys.Name;
//             string fullName = tSys.FullName ?? typeName;
//
//             if (hasFilter && !fullName.ToLowerInvariant().Contains(filterLower))
//                 return;
//
//             var rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
//
//             bool isSelected = _selectedSystem == sys;
//
//             if (Event.current.type == EventType.MouseDown &&
//                 rect.Contains(Event.current.mousePosition))
//             {
//                 if (Event.current.button == 0)
//                 {
//                     _selectedSystem     = sys;
//                     _selectedSystemType = tSys;
//                     _selectedSingletonOwner = null;
//                     Repaint();
//                     Event.current.Use();
//                 }
//             }
//
//             if (Event.current.type == EventType.Repaint)
//             {
//                 if (isSelected)
//                 {
//                     var selCol = EditorGUIUtility.isProSkin
//                         ? new Color(0.3f, 0.5f, 0.9f, 0.35f)
//                         : new Color(0.3f, 0.5f, 0.9f, 0.2f);
//                     EditorGUI.DrawRect(rect, selCol);
//                 }
//             }
//
//             rect.xMin += 4f;
//             GUI.Label(rect, $"{typeName}  <size=9>({index})</size>", EditorStyles.label);
//         }
//
//         private void DrawSingletonRow(
//             Type type,
//             Entity owner,
//             IWorld world,
//             bool hasFilter,
//             string filterLower)
//         {
//             string fullName = type.FullName ?? type.Name;
//             if (hasFilter && !fullName.ToLowerInvariant().Contains(filterLower))
//                 return;
//
//             var rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
//             bool isSelected = _selectedSingletonOwner.HasValue && _selectedSingletonOwner.Value.Id == owner.Id;
//
//             if (Event.current.type == EventType.MouseDown &&
//                 rect.Contains(Event.current.mousePosition))
//             {
//                 if (Event.current.button == 0)
//                 {
//                     _selectedSystem          = null;
//                     _selectedSystemType      = null;
//                     _selectedSingletonOwner  = owner;
//                     _findMode                = true;
//                     _findEntityId            = owner.Id;
//                     _findEntityGen           = owner.Gen;
//                     _entityIdText            = owner.Id.ToString();
//                     _entityGenText           = owner.Gen.ToString();
//                     _foundValid              = world.IsAlive(owner.Id, owner.Gen);
//                     if (_foundValid)
//                         _foundEntity = owner;
//                     Repaint();
//                     Event.current.Use();
//                 }
//             }
//
//             if (Event.current.type == EventType.Repaint && isSelected)
//             {
//                 var selCol = EditorGUIUtility.isProSkin
//                     ? new Color(0.3f, 0.8f, 0.3f, 0.35f)
//                     : new Color(0.3f, 0.8f, 0.3f, 0.2f);
//                 EditorGUI.DrawRect(rect, selCol);
//             }
//
//             rect.xMin += 4f;
//             GUI.Label(rect, $"{type.Name}  <size=9>(Entity {owner.Id}:{owner.Gen})</size>", EditorStyles.label);
//         }
//     }
// }
// #endif
