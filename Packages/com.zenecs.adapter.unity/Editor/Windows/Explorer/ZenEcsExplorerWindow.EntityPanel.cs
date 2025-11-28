// #if UNITY_EDITOR
// #nullable enable
// using System;
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
//         /// <summary>
//         /// 오른쪽 패널 전체.
//         /// </summary>
//         private void DrawRightPanel(IWorld? world)
//         {
//             if (world == null)
//             {
//                 EditorGUILayout.HelpBox("World is null.", MessageType.Info);
//                 return;
//             }
//
//             DrawFindBar(world);
//
//             EditorGUILayout.Space(4);
//
//             if (_findMode && _foundValid)
//             {
//                 DrawEntityDetail(world, _foundEntity);
//             }
//             else if (_selectedSystem != null && _selectedSystemType != null)
//             {
//                 DrawSystemDetail(world, _selectedSystem, _selectedSystemType);
//             }
//             else if (_selectedSingletonOwner.HasValue)
//             {
//                 DrawEntityDetail(world, _selectedSingletonOwner.Value);
//             }
//             else
//             {
//                 EditorGUILayout.HelpBox("Select a system or singleton from the left tree.", MessageType.Info);
//             }
//         }
//
//         /// <summary>
//         /// 엔티티 Find 모드 입력 바.
//         /// </summary>
//         private void DrawFindBar(IWorld world)
//         {
//             using (new EditorGUILayout.HorizontalScope())
//             {
//                 GUILayout.Label("Entity", GUILayout.Width(48));
//
//                 _entityIdText  = EditorGUILayout.TextField("ID",  _entityIdText, GUILayout.Width(160));
//                 _entityGenText = EditorGUILayout.TextField("Gen", _entityGenText, GUILayout.Width(120));
//
//                 if (GUILayout.Button("Find", GUILayout.Width(60)))
//                 {
//                     if (int.TryParse(_entityIdText, out var id) &&
//                         int.TryParse(_entityGenText, out var gen))
//                     {
//                         _findEntityId  = id;
//                         _findEntityGen = gen;
//
//                         _foundValid = world.IsAlive(id, gen);
//                         if (_foundValid)
//                         {
//                             _foundEntity = new Entity(id, gen);
//                             _findMode    = true;
//                         }
//                         else
//                         {
//                             _findMode = false;
//                         }
//                     }
//                 }
//
//                 if (GUILayout.Button("Clear", GUILayout.Width(60)))
//                 {
//                     _findMode      = false;
//                     _findEntityId  = null;
//                     _findEntityGen = null;
//                 }
//
//                 GUILayout.FlexibleSpace();
//
//                 GUILayout.Label(world.Name ?? $"World #{world.Id}", EditorStyles.miniBoldLabel);
//             }
//         }
//
//         /// <summary>
//         /// 매우 간단한 Entity 디테일: ID/GEN, 살아있는지, 컴포넌트 개수 정도로만 표시.
//         /// (필요 시 기존 Explorer의 상세 컴포넌트 GUI를 붙여넣기)
//         /// </summary>
//         private void DrawEntityDetail(IWorld world, Entity e)
//         {
//             EditorGUILayout.LabelField(
//                 $"Entity {e.Id}:{e.Gen}",
//                 EditorStyles.boldLabel);
//
//             bool alive = world.IsAlive(e.Id, e.Gen);
//             EditorGUILayout.LabelField("Alive", alive.ToString());
//
//             if (!alive) return;
//
//             // 컴포넌트 타입 나열 (간단 버전)
//             try
//             {
//                 var components = world.GetAllComponents(e).ToList();
//                 EditorGUILayout.LabelField("Components", components.Count.ToString());
//
//                 EditorGUI.indentLevel++;
//                 foreach (var c in components)
//                 {
//                     if (c.boxed == null) continue;
//                     var t = c.type;
//                     EditorGUILayout.LabelField(t.Name, EditorStyles.label);
//                 }
//                 EditorGUI.indentLevel--;
//             }
//             catch (Exception ex)
//             {
//                 EditorGUILayout.HelpBox($"Failed to read components: {ex.Message}", MessageType.Warning);
//             }
//         }
//
//         /// <summary>
//         /// 선택된 System 타입에 대한 간단 정보.
//         /// </summary>
//         private void DrawSystemDetail(IWorld world, ISystem sys, Type tSys)
//         {
//             EditorGUILayout.LabelField("System", EditorStyles.boldLabel);
//             EditorGUI.indentLevel++;
//
//             EditorGUILayout.LabelField("Type", tSys.FullName ?? tSys.Name);
//             EditorGUILayout.LabelField("World", world.Name ?? $"World #{world.Id}");
//
//             // 그룹/페이즈 표시
//             ResolveGroupAndPhase(tSys, out var group, out var phase);
//             EditorGUILayout.LabelField("Group", group.ToString());
//             EditorGUILayout.LabelField("Phase", phase.ToString());
//
//             EditorGUI.indentLevel--;
//
//             EditorGUILayout.Space(6);
//
//             EditorGUILayout.HelpBox(
//                 "This is a simplified System detail panel.\n" +
//                 "You can extend it to show system-specific debug info.",
//                 MessageType.None);
//         }
//
//         // World 변경 시 RightPanel 쪽에서 특별히 할 일은 현재 없음.
//         partial void OnWorldChangedPartial(IWorld? world) { }
//     }
// }
// #endif
