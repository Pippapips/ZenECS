// #if UNITY_EDITOR
// #nullable enable
// using System;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;
// using ZenECS.Adapter.Unity;
// using ZenECS.Core;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.EditorWindows
// {
//     /// <summary>
//     /// ZenECS 메인 Explorer 창.
//     /// - World 선택 및 상단 툴바
//     /// - 좌측: System 트리 (partial: SystemTree)
//     /// - 우측: 선택된 System / Entity / Singleton 디테일 (partial: EntityPanel)
//     /// </summary>
//     public sealed partial class ZenEcsExplorerWindow : EditorWindow
//     {
//         private IWorld? _currentWorld;
//
//         // SelectEntity 브리지용 상태
//         string _entityIdText   = string.Empty;
//         string _entityGenText  = "0";
//         int?   _findEntityId;
//         int?   _findEntityGen;
//         bool   _findMode;
//         Entity _foundEntity;
//         bool   _foundValid;
//
//         // 상단 툴바 레이아웃용
//         private const float ToolbarHeight = 22f;
//         private const float MinLeftWidth  = 260f;
//         private const float LeftRatio     = 0.38f;
//
//         // Fixed / Variable / Presentation 구분
//         enum PhaseKind
//         {
//             Unknown,
//             Deterministic,
//             NonDeterministic,
//         }
//
//         [MenuItem("ZenECS/Explorer", priority = 10)]
//         public static void Open()
//         {
//             var window = GetWindow<ZenEcsExplorerWindow>();
//             window.titleContent = new GUIContent("ZenECS Explorer");
//             window.Show();
//         }
//
//         private void OnEnable()
//         {
//             minSize = new Vector2(900, 420);
//         }
//
//         private void OnGUI()
//         {
//             var kernel = ZenEcsUnityBridge.Kernel;
//             if (kernel == null || !kernel.GetAllWorld().Any())
//             {
//                 DrawKernelNotReadyOverlay();
//                 return;
//             }
//
//             // 현재 World 가져오기
//             var world = kernel.CurrentWorld ?? kernel.GetAllWorld().FirstOrDefault();
//             if (!ReferenceEquals(world, _currentWorld))
//             {
//                 _currentWorld = world;
//                 OnWorldChanged(_currentWorld);
//             }
//
//             // 상단 툴바
//             using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
//             {
//                 DrawTopToolbar(kernel);
//             }
//
//             EditorGUILayout.Space(2);
//
//             if (_currentWorld == null)
//             {
//                 EditorGUILayout.HelpBox("No current World is active.", MessageType.Info);
//                 return;
//             }
//
//             // 메인 레이아웃: 좌측 System 트리 / 우측 디테일
//             var rect = GUILayoutUtility.GetRect(
//                 position.width,
//                 position.height - ToolbarHeight - 10f,
//                 GUILayout.ExpandWidth(true),
//                 GUILayout.ExpandHeight(true));
//
//             var leftWidth = Mathf.Clamp(rect.width * LeftRatio, MinLeftWidth, rect.width - 320f);
//             var leftRect  = new Rect(rect.x, rect.y, leftWidth, rect.height);
//             var rightRect = new Rect(rect.x + leftWidth + 4f, rect.y, rect.width - leftWidth - 4f, rect.height);
//
//             // 좌: System 트리
//             GUILayout.BeginArea(leftRect);
//             try
//             {
//                 DrawSystemTreeArea(_currentWorld);
//             }
//             finally
//             {
//                 GUILayout.EndArea();
//             }
//
//             // 우: 디테일 패널
//             GUILayout.BeginArea(rightRect);
//             try
//             {
//                 DrawRightPanel(_currentWorld);
//             }
//             finally
//             {
//                 GUILayout.EndArea();
//             }
//         }
//
//         /// <summary>
//         /// 상단 툴바.
//         /// - World 선택 드롭다운
//         /// - 간단한 액션 버튼들(Refresh 등)
//         /// </summary>
//         private void DrawTopToolbar(IKernel kernel)
//         {
//             // World 선택
//             var worlds = kernel.GetAllWorld().ToArray();
//             int currentIndex = -1;
//             if (_currentWorld != null)
//             {
//                 for (int i = 0; i < worlds.Length; i++)
//                 {
//                     if (worlds[i].Id == _currentWorld.Id)
//                     {
//                         currentIndex = i;
//                         break;
//                     }
//                 }
//             }
//
//             using (new EditorGUI.DisabledScope(worlds.Length == 0))
//             {
//                 var labels = worlds.Select(w => new GUIContent(w.Name ?? $"World #{w.Id}")).ToArray();
//                 int newIndex = EditorGUILayout.Popup(
//                     currentIndex,
//                     labels,
//                     GUILayout.MaxWidth(260));
//
//                 if (newIndex != currentIndex && newIndex >= 0 && newIndex < worlds.Length)
//                 {
//                     kernel.SetCurrentWorld(worlds[newIndex]);
//                     _currentWorld = worlds[newIndex];
//                     OnWorldChanged(_currentWorld);
//                 }
//             }
//
//             GUILayout.FlexibleSpace();
//
//             if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72)))
//             {
//                 RebuildSystemTree(_currentWorld);
//                 Repaint();
//             }
//         }
//
//         /// <summary>
//         /// Kernel 혹은 World가 준비되지 않았을 때 오버레이.
//         /// </summary>
//         private void DrawKernelNotReadyOverlay()
//         {
//              var titleStyle = new GUIStyle(EditorStyles.boldLabel)
//              {
//                  alignment = TextAnchor.MiddleCenter,
//                  fontSize = EditorStyles.boldLabel.fontSize + 2,
//                  wordWrap = true
//              };
//              var bodyStyle = new GUIStyle(EditorStyles.label)
//              {
//                  alignment = TextAnchor.MiddleCenter,
//                  wordWrap = true
//              };
//
//              GUILayout.FlexibleSpace();
//              using (new EditorGUILayout.HorizontalScope())
//              {
//                  GUILayout.FlexibleSpace();
//                  using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
//                  {
//                      GUILayout.Label("ZenECS Kernel is not active yet.", titleStyle);
//                      GUILayout.Space(8);
//                      GUILayout.Label(
//                          "Enter Play Mode to initialize the EcsDriver and Kernel.\n\n" +
//                          "When the Kernel becomes active, you can inspect Systems and Entities\n" +
//                          "through the ZenECS Explorer.",
//                          bodyStyle);
//                  }
//
//                  GUILayout.FlexibleSpace();
//              }
//
//              GUILayout.FlexibleSpace();
//         }
//
//         /// <summary>
//         /// World가 변경되었을 때 각 partial에 알려주기 위한 훅.
//         /// </summary>
//         private void OnWorldChanged(IWorld? world)
//         {
//             RebuildSystemTree(world);
//             OnWorldChangedPartial(world);
//             Repaint();
//         }
//
//         /// <summary>
//         /// 다른 partial에서 World 변경에 반응하고 싶을 때 구현.
//         /// (구현이 없으면 컴파일러가 제거)
//         /// </summary>
//         partial void OnWorldChangedPartial(IWorld? world);
//
//         /// <summary>
//         /// 외부에서 Explorer로 "이 엔티티를 선택해라"를 요청할 때 호출됨.
//         /// (ZenEcsExplorerBridge에서 사용)
//         /// </summary>
//         public void SelectEntity(IWorld world, int entityId, int entityGen)
//         {
//             var kernel = ZenEcsUnityBridge.Kernel;
//             if (kernel == null) return;
//
//             // World 동기화
//             var currentWorld = kernel.CurrentWorld;
//             if (currentWorld == null || currentWorld.Id != world.Id)
//             {
//                 kernel.SetCurrentWorld(world);
//                 _currentWorld = world;
//                 OnWorldChanged(_currentWorld);
//             }
//
//             _findEntityId  = entityId;
//             _findEntityGen = entityGen;
//             _entityIdText  = entityId.ToString();
//             _entityGenText = entityGen.ToString();
//
//             _foundValid = world.IsAlive(entityId, entityGen);
//             if (_foundValid)
//             {
//                 _foundEntity = new Entity(entityId, entityGen);
//                 _findMode    = true;
//             }
//
//             Repaint();
//         }
//     }
// }
// #endif
