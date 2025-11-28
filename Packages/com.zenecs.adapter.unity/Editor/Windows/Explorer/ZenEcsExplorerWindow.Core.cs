#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;
using ZenECS.EditorUtils;

namespace ZenECS.EditorWindows
{
    /// <summary>
    /// Core lifecycle, layout entry points, toolbar and shared helpers
    /// for the ZenECS Explorer window.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow : EditorWindow
    {
        // =====================================================================
        //  MENU & STATIC OPEN
        // =====================================================================

// =====================================================================
        //  MENU
        // =====================================================================

        [MenuItem("ZenECS/Tools/ZenECS Explorer")]
        public static void Open()
        {
            GetWindow<ZenEcsExplorerWindow>("ZenECS Explorer");
        }

        // =====================================================================
        //  STATE CLASSES
        // =====================================================================

        [Serializable]
        sealed class ExplorerFindState
        {
            public const string LabelEntityId = "Entity ID:GEN ";
            public const string BtnFind = "Find";
            public const string BtnClear = "Clear";

            public const string TipFind =
                "Show only the entity with this ID (no system switching).";

            public const string TipClear =
                "Exit single-entity view and show all entities.";

            // 입력 문자열
            public string EntityIdText = string.Empty;
            public string EntityGenText = "0";

            // 파싱된 타겟
            public int? EntityId;
            public int? EntityGen;

            // UI 상태
            public bool IsFindMode;
            public bool FoundValid;
            public bool WatchedSystemsFold;
            public bool EntityFoldBackup;

            public Entity FoundEntity;
        }

        [Serializable]
        sealed class ExplorerUiState
        {
            // 선택
            public int SelectedSystemIndex = -1;
            public int SelectedSystemEntityCount;
            public bool HasSelectedSingleton;
            public Entity SelectedSingletonEntity;
            public Type? SelectedSingletonType;

            // 스크롤
            public Vector2 LeftScroll;
            public Vector2 RightScroll;

            // 편집 모드
            public bool EditMode = true;

            // Watched / Foldout
            public readonly Dictionary<string, bool> WatchedFold = new();

            // System Tree Foldout
            public readonly Dictionary<SystemGroup, bool> GroupFold = new();
            public readonly Dictionary<(SystemGroup group, PhaseKind phase), bool> PhaseFold = new();

            public bool DeterministicFold = true;
            public bool NonDeterministicFold = true;
            public bool BeginFold = true;
            public bool LateFold = true;
            public bool UnknownFold = true;
            public bool SingletonsFold = true;

            // Entity / Component / Binder / Context Foldout
            public readonly Dictionary<Entity, bool> EntityFold = new();
            public readonly Dictionary<string, bool> ComponentFold = new();
            public readonly Dictionary<string, bool> BinderFold = new();
            public readonly Dictionary<string, bool> ContextFold = new();

            public void ClearSelection()
            {
                SelectedSystemIndex = -1;
                SelectedSystemEntityCount = 0;
                HasSelectedSingleton = false;
                SelectedSingletonEntity = default;
                SelectedSingletonType = null;

                EntityFold.Clear();
                BinderFold.Clear();
                ComponentFold.Clear();
                ContextFold.Clear();
                WatchedFold.Clear();
            }

            public void ClearTreeFoldouts()
            {
                GroupFold.Clear();
                PhaseFold.Clear();
            }
        }

        // 실제 상태 인스턴스
        readonly ExplorerFindState _find = new();
        readonly ExplorerUiState _ui = new();

        // =====================================================================
        //  기타 캐시 / enum
        // =====================================================================

        readonly List<Entity> _cache = new(256);
        double _nextRepaint;
        static GUIStyle? _bigPlusButton;
        static bool _bigPlusReady;

        // Fixed / Variable / Presentation 구분
        enum PhaseKind
        {
            Unknown,
            Deterministic,
            NonDeterministic,
        }

        // System.Enabled 리플렉션 캐시
        static readonly Dictionary<Type, PropertyInfo?> _systemEnabledPropCache = new();

        // =====================================================================
        //  FIELDS (발췌: 기존 ZenEcsExplorerWindow.cs 상단의 모든 필드/상수/enum)
        // =====================================================================

        // --- UI labels/tooltips (English) ---
        const string LABEL_ENTITY_ID = "Entity ID:GEN ";
        const string BTN_FIND = "Find";
        const string BTN_CLEAR_FILTER = "Clear";
        const string TIP_FIND = "Show only the entity with this ID (no system switching).";
        const string TIP_CLEAR = "Exit single-entity view and show all entities.";

        // --- Single-entity Find mode state ---
        string _entityIdText = "";
        int? _findEntityId = null; // current target ID (null => list mode)
        string _entityGenText = "0";
        int? _findEntityGen = null; // current target ID (null => list mode)
        private bool _findEntityFoldBackup;

        bool _findMode = false;   // single view mode on/off
        Entity _foundEntity;      // resolved entity
        bool _foundValid = false; // found in world?
        bool _findWatchedSystemsFold = false;

        // --- Other UI/layout state ---
        Vector2 _left, _right;
        int _selSystem = -1;


        // 현재 선택된 싱글톤 (좌측 Singletons 섹션)
        Entity _selectedSingletonEntity;
        Type? _selectedSingletonType;
        bool _hasSelectedSingleton = false;

        private int _selSysEntityCount;

        // 👇 Watched Components Foldout 상태 (시스템 타입별)
        readonly Dictionary<string, bool> _watchedFold = new();

        // 👇 System 트리용 Foldout 상태
        readonly Dictionary<SystemGroup, bool> _groupFold = new();
        readonly Dictionary<(SystemGroup group, PhaseKind phase), bool> _phaseFold = new();

        // Phase 상위/하위 섹션 Foldout 상태
        bool _deterministicFold = true;
        bool _nonDeterministicFold = true;
        bool _beginFold = true;
        bool _lateFold = true;
        bool _unknownFold = true;    // 👈 기존
        bool _singletonsFold = true; // Singletons 섹션 Foldout

        readonly Dictionary<Entity, bool> _entityFold = new();    // entityId → fold
        readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _binderFold = new();    // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _contextFold = new();   // $"{entityId}:{typeName}:CTX" → fold
        //bool _ui.EditMode = true;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            
            _ui.ClearSelection();
            _ui.ClearTreeFoldouts();

            _find.IsFindMode          = false;
            _find.EntityId            = null;
            _find.EntityGen           = null;
            _find.FoundValid          = false;
            _find.WatchedSystemsFold  = false;

            _nextRepaint = EditorApplication.timeSinceStartup + 0.25;            
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeReload;
        }

        void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup > _nextRepaint)
            {
                _nextRepaint = EditorApplication.timeSinceStartup + 0.25f;
                Repaint();
            }
        }

        void OnBeforeReload()
        {
            _ui.ClearSelection();
            _cache.Clear();

            _find.IsFindMode         = false;
            _find.EntityId           = null;
            _find.EntityGen          = null;
            _find.FoundValid         = false;
            _find.WatchedSystemsFold = false;

            _ui.ClearTreeFoldouts();
            
            // old
            _selSystem = -1;
            _hasSelectedSingleton = false;
            _selSysEntityCount = 0;
            _cache.Clear();
            _entityFold.Clear();
            _binderFold.Clear();
            _componentFold.Clear();
            _contextFold.Clear();
            _watchedFold.Clear();
            _findMode = false;
            _findEntityId = null;
            _foundValid = false;
            _findWatchedSystemsFold = false;

            // 👇 시스템 트리 Foldout 초기화
            _groupFold.Clear();
            _phaseFold.Clear();
        }

        void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                _ui.ClearSelection();
                _cache.Clear();

                _find.IsFindMode         = false;
                _find.EntityId           = null;
                _find.EntityGen          = null;
                _find.FoundValid         = false;
                _find.WatchedSystemsFold = false;

                _ui.ClearTreeFoldouts();

                Repaint();                
                
                // old
                
                _selSystem = -1;
                _hasSelectedSingleton = false;
                _selSysEntityCount = 0;
                _cache.Clear();
                _findMode = false;
                _findEntityId = null;
                _foundValid = false;
                _findWatchedSystemsFold = false;
                _entityFold.Clear();
                _binderFold.Clear();
                _componentFold.Clear();
                _contextFold.Clear();
                _watchedFold.Clear();

                // 👇 시스템 트리 Foldout 초기화
                _groupFold.Clear();
                _phaseFold.Clear();

                Repaint();
            }
        }

        // =====================================================================
        //  OnGUI (기존 버전 그대로 사용)
        // =====================================================================

        void OnGUI()
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null || !kernel.GetAllWorld().Any())
            {
                DrawKernelNotReadyOverlay();
                return;
            }

            var world = kernel.CurrentWorld;
            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            // 🔹 맨 위 상단 바
            DrawTopToolbar(kernel);
            EditorGUILayout.Space(2);

            // 🔹 FIND MODE
            if (_find.IsFindMode)
            {
                DrawFindMode(kernel, world, systems);
                return;
            }
            
            // 🔹 일반 모드
            DrawMainLayout(kernel, world, systems);
            
            #region old
            // // =====================================================
            // // 1) FIND MODE: 좌/우 패널 없이 전체 폭으로 Find 뷰만
            // // =====================================================
            // if (_findMode)
            // {
            //     using (var sv = new EditorGUILayout.ScrollViewScope(_right))
            //     {
            //         _right = sv.scrollPosition;
            //
            //         EditorGUILayout.Space(4);
            //
            //         using (new EditorGUILayout.VerticalScope("box"))
            //         {
            //             // 🔹 맨 위 가운데 Close 버튼
            //             using (new EditorGUILayout.HorizontalScope())
            //             {
            //                 GUIStyle centeredButtonStyle = EditorStyles.toolbarButton;
            //                 centeredButtonStyle.fontStyle = FontStyle.Normal;
            //                 centeredButtonStyle.fontSize = 20;
            //
            //                 GUILayout.FlexibleSpace();
            //                 if (GUILayout.Button("BACK", centeredButtonStyle, GUILayout.Width(80)))
            //                 {
            //                     _entityFold[_foundEntity] = _findEntityFoldBackup;
            //
            //                     _entityIdText = "";
            //                     _findEntityId = null;
            //
            //                     _entityGenText = "0";
            //                     _findEntityGen = null;
            //
            //                     _findWatchedSystemsFold = false;
            //                     _foundValid = false;
            //                     _findMode = false;
            //                     Repaint();
            //                     return;
            //                 }
            //
            //                 GUILayout.FlexibleSpace();
            //             }
            //
            //             EditorGUILayout.Space(6);
            //
            //             // 🔹 Watched Systems 목록 (Back 버튼 바로 아래)
            //             if (world != null && systems != null && _foundValid)
            //             {
            //                 var watchedList = CollectWatchedSystemsForEntity(world, _foundEntity, systems);
            //                 if (watchedList.Count > 0)
            //                 {
            //                     using (new EditorGUILayout.VerticalScope("box"))
            //                     {
            //                         var leftFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            //                             { };
            //
            //                         leftFoldoutStyle.focused.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.onFocused.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.hover.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.onHover.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.active.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.onActive.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.normal.textColor = systemMetaTextColor;
            //                         leftFoldoutStyle.onNormal.textColor = systemMetaTextColor;
            //
            //                         // Foldout 헤더: "Watched Systems (N)"
            //                         _findWatchedSystemsFold = EditorGUILayout.Foldout(
            //                             _findWatchedSystemsFold,
            //                             $"Watched Systems ({watchedList.Count})",
            //                             true,
            //                             leftFoldoutStyle
            //                         );
            //
            //                         if (_findWatchedSystemsFold)
            //                         {
            //                             EditorGUI.indentLevel++;
            //
            //                             // Watched Components와 동일한 회색 네임스페이스 스타일
            //                             var nsStyle = new GUIStyle(EditorStyles.miniLabel)
            //                             {
            //                                 wordWrap = true,
            //                                 fontSize = 10,
            //                                 padding = new RectOffset(0, 0, 0, 0),
            //                                 richText = true,
            //                                 normal =
            //                                 {
            //                                     textColor = systemMetaTextColor
            //                                 }
            //                             };
            //
            //                             foreach (var (sys, tSys) in watchedList)
            //                             {
            //                                 if (tSys == null) continue;
            //                                 string ns = string.IsNullOrEmpty(tSys.Namespace)
            //                                     ? "(global)"
            //                                     : tSys.Namespace;
            //
            //                                 using (new EditorGUILayout.HorizontalScope())
            //                                 {
            //                                     // System 이름
            //                                     EditorGUILayout.LabelField($"{tSys.Name} <color=#707070>[{ns}]</color>",
            //                                         nsStyle);
            //                                     // EditorGUILayout.LabelField(tSys.Name, GUILayout.ExpandWidth(false));
            //                                     //
            //                                     // // [namespace] 어두운 회색
            //                                     // EditorGUILayout.LabelField($"[{ns}]", nsStyle, GUILayout.ExpandWidth(true));
            //
            //                                     // 돋보기 아이콘 (우측 끝)
            //                                     var icon = GetSearchIconContent("Ping system script asset");
            //                                     if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
            //                                             GUILayout.Height(16)))
            //                                     {
            //                                         // 선택은 유지하고 Ping만
            //                                         PingSystemTypeNoSelect(tSys);
            //                                     }
            //                                 }
            //                             }
            //
            //                             EditorGUI.indentLevel--;
            //                         }
            //                     }
            //
            //                     EditorGUILayout.Space(6);
            //                 }
            //             }
            //
            //             // 아래는 기존 Entity 표시 로직 그대로
            //             if (world == null)
            //             {
            //                 EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
            //             }
            //             else if (_findEntityId.HasValue && _findEntityGen.HasValue)
            //             {
            //                 if (_foundValid)
            //                 {
            //                     EditorGUILayout.LabelField(
            //                         $"Entity #{_findEntityId.Value}:{_findEntityGen.Value}",
            //                         EditorStyles.boldLabel);
            //
            //                     GUILayout.Space(2);
            //
            //                     DrawOneEntity(world, _foundEntity);
            //                 }
            //                 else
            //                 {
            //                     EditorGUILayout.HelpBox(
            //                         $"No entity with ID {_findEntityId.Value}:{_findEntityGen.Value} in {world.Name} World.",
            //                         MessageType.Info
            //                     );
            //                 }
            //             }
            //             else
            //             {
            //                 EditorGUILayout.HelpBox(
            //                     "Please enter a valid positive numeric Entity ID/GEN.",
            //                     MessageType.Warning
            //                 );
            //             }
            //         }
            //     }
            //
            //     GUILayout.Space(4);
            //     DrawFooter(kernel);
            //     return;
            // }
            //
            // // =====================================================
            // // 2) 일반 모드: 좌측 Systems + 세로 구분선 + 우측 Entities
            // // =====================================================
            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     // ---------- Left: Systems ----------
            //     using (var sv = new EditorGUILayout.ScrollViewScope(_left, GUILayout.Width(300)))
            //     {
            //         _left = sv.scrollPosition;
            //
            //         EditorGUILayout.Space(4);
            //
            //         using (new EditorGUILayout.HorizontalScope())
            //         {
            //             EditorGUILayout.LabelField(
            //                 $"Systems ({systems?.Count}) Entities ({world?.GetAllEntities().Count})",
            //                 EditorStyles.boldLabel);
            //             GUILayout.FlexibleSpace();
            //
            //             GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            //             buttonStyle.alignment = TextAnchor.LowerCenter;
            //
            //             if (GUILayout.Button(new GUIContent("R", "Clear Selection"), buttonStyle, GUILayout.Width(24),
            //                     GUILayout.Height(24)))
            //             {
            //                 _selSystem = -1;
            //                 _hasSelectedSingleton = false;
            //                 _selSysEntityCount = 0;
            //                 _entityFold.Clear();
            //                 _binderFold.Clear();
            //                 _componentFold.Clear();
            //                 _contextFold.Clear();
            //                 _watchedFold.Clear();
            //                 _cache.Clear();
            //             }
            //         }
            //
            //         //EditorGUILayout.Space(2);
            //
            //         if (systems == null || systems.Count == 0)
            //         {
            //             EditorGUILayout.HelpBox("No systems registered.", MessageType.Info);
            //         }
            //         else
            //         {
            //             DrawSystemTree(systems, world);
            //         }
            //     }
            //
            //     EditorGUILayout.Space(4);
            //
            //     // ---------- Vertical Separator ----------
            //     {
            //         var sepRect = GUILayoutUtility.GetRect(
            //             1f, 1f,
            //             GUILayout.ExpandHeight(true),
            //             GUILayout.Width(1f)
            //         );
            //
            //         var sepColor = EditorGUIUtility.isProSkin
            //             ? new Color(0.22f, 0.22f, 0.22f, 1f)
            //             : new Color(0.6f, 0.6f, 0.6f, 1f);
            //
            //         sepColor = Color.black;
            //         EditorGUI.DrawRect(sepRect, sepColor);
            //     }
            //
            //     // ---------- Right: Entities ----------
            //     using (var sv = new EditorGUILayout.ScrollViewScope(_right))
            //     {
            //         _right = sv.scrollPosition;
            //
            //         EditorGUILayout.Space(4);
            //
            //         bool hasSystem = systems != null &&
            //                          _selSystem >= 0 &&
            //                          _selSystem < (systems?.Count ?? 0);
            //
            //         bool hasSingleton = false;
            //         if (_hasSelectedSingleton && world != null)
            //         {
            //             hasSingleton = world.IsAlive(_selectedSingletonEntity.Id, _selectedSingletonEntity.Gen);
            //             if (!hasSingleton)
            //             {
            //                 // 소멸된 싱글톤 엔티티는 선택 해제
            //                 _hasSelectedSingleton = false;
            //             }
            //         }
            //
            //         if (!hasSystem && !hasSingleton)
            //         {
            //             // 시스템/싱글톤 선택 없음 → 안내 메시지만
            //             GUILayout.FlexibleSpace();
            //             using (new EditorGUILayout.HorizontalScope())
            //             {
            //                 GUILayout.FlexibleSpace();
            //
            //                 EditorGUILayout.HelpBox(
            //                     "Select a system or singleton from the left panel.\n" +
            //                     "The System Meta or Singleton Entity will be shown here.",
            //                     MessageType.Info);
            //
            //                 GUILayout.FlexibleSpace();
            //             }
            //
            //             GUILayout.FlexibleSpace();
            //         }
            //         else if (hasSingleton && !hasSystem && world != null)
            //         {
            //             // ===== 싱글톤 선택 모드 =====
            //             DrawSingletonDetail(world, _selectedSingletonType, _selectedSingletonEntity);
            //         }
            //         else
            //         {
            //             // =========================
            //             // 정상 리스트 모드 (시스템 선택 있음)
            //             // =========================
            //
            //             var sys = systems![_selSystem];
            //
            //             // 🔹 1) System Meta 박스
            //             DrawSystemMeta(sys, world);
            //
            //             EditorGUILayout.Space(6);
            //
            //             // 🔹 2) Entities 헤더 + 리스트
            //             using (new EditorGUILayout.HorizontalScope())
            //             {
            //                 if (_selSysEntityCount > 0)
            //                     EditorGUILayout.LabelField($"Entities ({_selSysEntityCount})", EditorStyles.boldLabel);
            //                 else
            //                     EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
            //
            //                 GUILayout.FlexibleSpace();
            //             }
            //
            //             EditorGUILayout.Space(4);
            //
            //             var done = false;
            //
            //             if (!done && world == null)
            //             {
            //                 EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
            //                 done = true;
            //             }
            //
            //             if (!done)
            //             {
            //                 _cache.Clear();
            //
            //                 if (!WatchQueryRunner.TryCollectByWatch(sys, world,
            //                         _cache))
            //                     EditorGUILayout.HelpBox(
            //                         "No inspector. Implement IInspectableSystem or add [Watch].",
            //                         MessageType.Info);
            //
            //                 _selSysEntityCount = _cache.Count;
            //
            //                 foreach (var e in _cache.Distinct())
            //                     if (world != null)
            //                         DrawOneEntity(world, e);
            //             }
            //         }
            //     }
            // }
            //
            // GUILayout.Space(4);
            // DrawFooter(kernel);
            #endregion
        }

        // =====================================================================
        //  HELPER: KERNEL NOT READY OVERLAY
        // =====================================================================

        void DrawKernelNotReadyOverlay()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = EditorStyles.boldLabel.fontSize + 2,
                wordWrap = true
            };
            var bodyStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
                {
                    GUILayout.Label("ZenECS Kernel is not active yet.", titleStyle);
                    GUILayout.Space(8);
                    GUILayout.Label(
                        "Enter Play Mode to initialize the EcsDriver and Kernel.\n\n" +
                        "When the Kernel becomes active, you can inspect Systems and Entities\n" +
                        "through the ZenECS Explorer.",
                        bodyStyle);
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        // =====================================================================
        //  DRAW: FIND MODE
        // =====================================================================

        void DrawFindMode(IKernel kernel, IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            using (var sv = new EditorGUILayout.ScrollViewScope(_ui.RightScroll))
            {
                _ui.RightScroll = sv.scrollPosition;

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    // 상단 Close 버튼
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Close", GUILayout.Width(80)))
                        {
                            _find.IsFindMode = false;
                            _find.EntityId   = null;
                            _find.EntityGen  = null;
                            _find.FoundValid = false;
                            return;
                        }
                    }

                    EditorGUILayout.Space(4);

                    // Entity ID / GEN 입력
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(ExplorerFindState.LabelEntityId,
                            GUILayout.Width(100));

                        _find.EntityIdText = EditorGUILayout.TextField(_find.EntityIdText,
                            GUILayout.Width(80));
                        _find.EntityGenText = EditorGUILayout.TextField(_find.EntityGenText,
                            GUILayout.Width(60));

                        GUILayout.FlexibleSpace();

                        using (new EditorGUI.DisabledScope(world == null))
                        {
                            if (GUILayout.Button(
                                    new GUIContent(ExplorerFindState.BtnFind,
                                        ExplorerFindState.TipFind),
                                    GUILayout.Width(60)))
                            {
                                TryResolveFindTarget(world);
                            }
                        }

                        if (GUILayout.Button(
                                new GUIContent(ExplorerFindState.BtnClear,
                                    ExplorerFindState.TipClear),
                                GUILayout.Width(60)))
                        {
                            _find.IsFindMode = false;
                            _find.EntityId   = null;
                            _find.EntityGen  = null;
                            _find.FoundValid = false;
                            _ui.ClearSelection();
                        }
                    }

                    EditorGUILayout.Space(4);

                    // 결과 표시
                    DrawFindResult(world, systems);
                }
            }

            GUILayout.Space(4);
            DrawFooter(kernel);
        }

        void TryResolveFindTarget(IWorld? world)
        {
            _find.FoundValid = false;

            if (world == null)
                return;

            if (!int.TryParse(_find.EntityIdText, out var id))
                return;

            if (!int.TryParse(_find.EntityGenText, out var gen))
                gen = 0;

            _find.EntityId  = id;
            _find.EntityGen = gen;

            var e = new Entity(id, gen);
            if (!world.IsAlive(e))
            {
                _find.FoundValid = false;
                return;
            }

            _find.FoundEntity = e;
            _find.FoundValid  = true;
        }

        void DrawFindResult(IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            if (!_find.EntityId.HasValue || !_find.EntityGen.HasValue)
            {
                EditorGUILayout.HelpBox("Enter a valid Entity ID and GEN.",
                    MessageType.Info);
                return;
            }

            if (world == null)
            {
                EditorGUILayout.HelpBox("World not attached.",
                    MessageType.Warning);
                return;
            }

            if (!_find.FoundValid)
            {
                EditorGUILayout.HelpBox(
                    $"Entity #{_find.EntityId.Value}:{_find.EntityGen.Value} not found.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"Entity #{_find.EntityId.Value}:{_find.EntityGen.Value}",
                EditorStyles.boldLabel);

            GUILayout.Space(2);

            // Watched Systems (위쪽)
            if (systems != null)
            {
                var watchedList = CollectWatchedSystemsForEntity(world, _find.FoundEntity, systems);
                if (watchedList.Count > 0)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var foldStyle = new GUIStyle(EditorStyles.foldout);

                        _find.WatchedSystemsFold = EditorGUILayout.Foldout(
                            _find.WatchedSystemsFold,
                            $"Watched Systems ({watchedList.Count})",
                            true,
                            foldStyle);

                        if (_find.WatchedSystemsFold)
                        {
                            foreach (var (sys, t) in watchedList)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(t.Name);
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Ping", GUILayout.Width(60)))
                                        PingSystemType(t);
                                }
                            }
                        }
                    }
                }
            }

            GUILayout.Space(4);

            // 실제 Entity Inspect
            DrawOneEntity(world, _find.FoundEntity);
        }

        // =====================================================================
        //  DRAW: MAIN LAYOUT (좌 System / 우 Entity / 하단 Footer)
        // =====================================================================

        void DrawMainLayout(IKernel kernel, IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            if (world == null)
            {
                EditorGUILayout.HelpBox("No active World.", MessageType.Info);
                GUILayout.Space(4);
                DrawFooter(kernel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftSystemTreePanel(world, systems);
                DrawVerticalSeparator();
                DrawRightEntityPanel(world, systems);
            }

            GUILayout.Space(4);
            DrawFooter(kernel);
        }

        void DrawVerticalSeparator()
        {
            var sepRect = GUILayoutUtility.GetRect(
                1f, 1f,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(1f));

            var sepColor = Color.black;
            EditorGUI.DrawRect(sepRect, sepColor);
        }
        
        // =====================================================================
        //  DRAW: 좌 System / 우 Entity / 하단 Footer)
        // =====================================================================

        private void DrawLeftSystemTreePanel(IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            using var sv = new EditorGUILayout.ScrollViewScope(_left, GUILayout.Width(300));
            _left = sv.scrollPosition;
            
            EditorGUILayout.Space(4);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Systems ({systems?.Count}) Entities ({world?.GetAllEntities().Count})",
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            
                GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
                buttonStyle.alignment = TextAnchor.LowerCenter;
            
                if (GUILayout.Button(new GUIContent("R", "Clear Selection"), buttonStyle, GUILayout.Width(24),
                        GUILayout.Height(24)))
                {
                    _selSystem = -1;
                    _hasSelectedSingleton = false;
                    _selSysEntityCount = 0;
                    _entityFold.Clear();
                    _binderFold.Clear();
                    _componentFold.Clear();
                    _contextFold.Clear();
                    _watchedFold.Clear();
                    _cache.Clear();
                }
            }
            
            //EditorGUILayout.Space(2);
            
            if (systems == null || systems.Count == 0)
            {
                EditorGUILayout.HelpBox("No systems registered.", MessageType.Info);
            }
            else
            {
                DrawSystemTree(systems, world);
            }

            EditorGUILayout.Space(4);
        }

        private void DrawRightEntityPanel(IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            using var sv = new EditorGUILayout.ScrollViewScope(_right);
            _right = sv.scrollPosition;

            EditorGUILayout.Space(4);

            bool hasSystem = systems != null &&
                             _selSystem >= 0 &&
                             _selSystem < (systems?.Count ?? 0);

            bool hasSingleton = false;
            if (_hasSelectedSingleton && world != null)
            {
                hasSingleton = world.IsAlive(_selectedSingletonEntity.Id, _selectedSingletonEntity.Gen);
                if (!hasSingleton)
                {
                    // 소멸된 싱글톤 엔티티는 선택 해제
                    _hasSelectedSingleton = false;
                }
            }

            if (!hasSystem && !hasSingleton)
            {
                // 시스템/싱글톤 선택 없음 → 안내 메시지만
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.HelpBox(
                        "Select a system or singleton from the left panel.\n" +
                        "The System Meta or Singleton Entity will be shown here.",
                        MessageType.Info);

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
            else if (hasSingleton && !hasSystem && world != null)
            {
                // ===== 싱글톤 선택 모드 =====
                DrawSingletonDetail(world, _selectedSingletonType, _selectedSingletonEntity);
            }
            else
            {
                // =========================
                // 정상 리스트 모드 (시스템 선택 있음)
                // =========================

                var sys = systems![_selSystem];

                // 🔹 1) System Meta 박스
                DrawSystemMeta(sys, world);

                EditorGUILayout.Space(6);

                // 🔹 2) Entities 헤더 + 리스트
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_selSysEntityCount > 0)
                        EditorGUILayout.LabelField($"Entities ({_selSysEntityCount})", EditorStyles.boldLabel);
                    else
                        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);

                var done = false;

                if (!done && world == null)
                {
                    EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
                    done = true;
                }

                if (!done)
                {
                    _cache.Clear();

                    if (!WatchQueryRunner.TryCollectByWatch(sys, world,
                            _cache))
                        EditorGUILayout.HelpBox(
                            "No inspector. Implement IInspectableSystem or add [Watch].",
                            MessageType.Info);

                    _selSysEntityCount = _cache.Count;

                    foreach (var e in _cache.Distinct())
                        if (world != null)
                            DrawOneEntity(world, e);
                }
            }
        }
        
        
        // =====================================================================
        //  HELPER: SYSTEM META 박스 (우측 상단 System 정보)
        // =====================================================================

        void DrawSystemMeta(ISystem? sys, IWorld? world)
        {
            if (sys == null) return;

            var t = sys.GetType();

            // 그룹 & Phase (Fixed/Variable/Presentation)
            ResolveGroupAndPhase(t, out var group, out var phase);

            string groupLabel = group switch
            {
                SystemGroup.Unknown => "Unknown",
                SystemGroup.FrameInput => "Frame Input",
                SystemGroup.FrameSync => "Frame Sync",
                SystemGroup.FrameView => "Frame View",
                SystemGroup.FrameUI => "Frame UI",
                SystemGroup.FixedInput => "Fixed Input",
                SystemGroup.FixedDecision => "Fixed Decision",
                SystemGroup.FixedSimulation => "Fixed Simulation",
                SystemGroup.FixedPost => "Fixed Post",
                _ => group.ToString()
            };

            // Execution Group + 대표 인터페이스
            string execLabel = "Unknown";

            if (group == SystemGroup.FrameInput ||
                group == SystemGroup.FrameSync ||
                group == SystemGroup.FrameView ||
                group == SystemGroup.FrameUI)
            {
                execLabel = "Non-deterministic";
            }
            else if (group == SystemGroup.FixedInput ||
                     group == SystemGroup.FixedDecision ||
                     group == SystemGroup.FixedSimulation ||
                     group == SystemGroup.FixedPost)
            {
                execLabel = "Deterministic";
            }

            // Order Before/After (Attribute 기반)
            var beforeList = new List<string>();
            var afterList = new List<string>();

            try
            {
                var beforeAttrs = t.GetCustomAttributes(typeof(OrderBeforeAttribute), true)
                    .Cast<OrderBeforeAttribute>();
                foreach (var a in beforeAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        beforeList.Add(target.Name);
                }

                var afterAttrs = t.GetCustomAttributes(typeof(OrderAfterAttribute), true)
                    .Cast<OrderAfterAttribute>();
                foreach (var a in afterAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        afterList.Add(target.Name);
                }
            }
            catch
            {
                // 구버전에서 타입이 다를 수 있으니 조용히 무시
            }

            string beforeText = beforeList.Count > 0
                ? string.Join(", ", beforeList.Distinct())
                : "—";

            string afterText = afterList.Count > 0
                ? string.Join(", ", afterList.Distinct())
                : "—";

            // ZenSystemWatchAttribute.AllOf 기반으로 Watched Components 추출
            var watchedTypes = new List<Type>();
            try
            {
                var watchAttrs = t.GetCustomAttributes(typeof(ZenSystemWatchAttribute), false)
                    .Cast<ZenSystemWatchAttribute>();

                foreach (var wa in watchAttrs)
                {
                    var allOf = wa.AllOf;
                    if (allOf == null || allOf.Length == 0)
                        continue;

                    foreach (var compType in allOf)
                    {
                        if (compType != null)
                            watchedTypes.Add(compType);
                    }
                }
            }
            catch
            {
                // 구버전이나 리플렉션 실패는 조용히 무시
            }

            var watchedDistinct = watchedTypes
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x.Name)
                .ToList();

            string watchedText = watchedDistinct.Count > 0
                ? string.Join(", ", watchedDistinct.Select(x => x.Name))
                : "—";

            // // 선택된 시스템에 대해 현재 Watched 엔티티 수 (있으면 메타에 추가)
            // int watchedCount = 0;
            // if (world != null)
            // {
            //     try
            //     {
            //         var tmp = new List<Entity>();
            //         if (ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world, tmp))
            //         {
            //             watchedCount = tmp.Count;
            //         }
            //     }
            //     catch
            //     {
            //         // 필요하면 로그, 지금은 조용히 무시
            //     }
            //}

            // 네임스페이스용 회색 스타일
            var nsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.5f, 0.5f, 0.5f)
                        : new Color(0.4f, 0.4f, 0.4f)
                },
                fontSize = 10,
            };

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 상단: System 이름 + Namespace + Ping 아이콘
                string ns = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // 이름 + 네임스페이스를 한 덩어리로 왼쪽에 붙여서 표시
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(t.Name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"[{ns}]", nsStyle);
                    }

                    GUILayout.FlexibleSpace();

                    var searchContent = GetSearchIconContent("Ping script asset for this system type");
                    if (GUILayout.Button(searchContent, EditorStyles.iconButton, GUILayout.Width(20),
                            GUILayout.Height(18)))
                    {
                        PingSystemType(t);
                    }
                }

                EditorGUILayout.Space(2);

                // 조금 눈에 잘 들어오도록 라벨 스타일 준비
                var leftLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = systemMetaTextColor
                    },
                };
                var valueStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontSize = 9,
                    padding = new RectOffset(0, 0, 4, 0),
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Group", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(groupLabel, valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Execution", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(execLabel, valueStyle);
                EditorGUILayout.EndHorizontal();

                // Order (Before/After)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order Before", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(beforeText, valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order After", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(afterText, valueStyle);
                EditorGUILayout.EndHorizontal();

// ==== Watched Components Foldout ====

                var leftFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                };

                leftFoldoutStyle.focused.textColor = systemMetaTextColor;
                leftFoldoutStyle.onFocused.textColor = systemMetaTextColor;
                leftFoldoutStyle.hover.textColor = systemMetaTextColor;
                leftFoldoutStyle.onHover.textColor = systemMetaTextColor;
                leftFoldoutStyle.active.textColor = systemMetaTextColor;
                leftFoldoutStyle.onActive.textColor = systemMetaTextColor;
                leftFoldoutStyle.normal.textColor = systemMetaTextColor;
                leftFoldoutStyle.onNormal.textColor = systemMetaTextColor;

// Watched 대상이 하나도 없으면 간단히 표시만
                if (watchedDistinct.Count == 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Watched", leftLabelStyle, GUILayout.Width(70));
                        EditorGUILayout.LabelField("—", valueStyle);
                        EditorGUILayout.EndHorizontal();
                        // if (watchedCount > 0)
                        // {
                        //     GUILayout.FlexibleSpace();
                        //     EditorGUILayout.LabelField($"Watched Entities: {watchedCount}", EditorStyles.miniLabel,
                        //         GUILayout.MaxWidth(160));
                        // }
                    }
                }
                else
                {
                    // 시스템 타입별로 Foldout 상태를 저장
                    var key = t.FullName ?? t.Name;
                    if (!_watchedFold.TryGetValue(key, out var open))
                        open = false;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Foldout: "Watched (N)"
                        open = EditorGUILayout.Foldout(
                            open,
                            $"Watched ({watchedDistinct.Count})",
                            true,
                            leftFoldoutStyle
                        );
                        _watchedFold[key] = open;

                        GUILayout.FlexibleSpace();

                        // if (watchedCount > 0)
                        // {
                        //     EditorGUILayout.LabelField($"Watched Entities: {watchedCount}",
                        //         EditorStyles.miniLabel, GUILayout.MaxWidth(160));
                        // }
                    }

                    if (open)
                    {
                        EditorGUI.indentLevel++;

                        var componentStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            wordWrap = true,
                            fontSize = 10,
                            padding = new RectOffset(0, 0, 4, 0),
                            richText = true,
                            normal =
                            {
                                textColor = systemMetaTextColor
                            }
                        };

                        foreach (var compType in watchedDistinct)
                        {
                            if (compType == null) continue;

                            string cns = string.IsNullOrEmpty(compType.Namespace) ? "(global)" : compType.Namespace;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                // 컴포넌트명
                                EditorGUILayout.LabelField($"{compType.Name} <color=#707070>[{cns}]</color>",
                                    componentStyle);

                                // // 네임스페이스 [Namespace] (회색)
                                // EditorGUILayout.LabelField($"[{cns}]", nsStyle);

                                // 돋보기 아이콘 (우측 끝)
                                var icon = GetSearchIconContent("Ping component script asset");
                                if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
                                        GUILayout.Height(16)))
                                {
                                    // 선택은 유지하고 Ping만
                                    PingComponentType(compType);
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        // =====================================================================
        //  TOP TOOLBAR + PLUS 메뉴
        // =====================================================================

        void DrawTopToolbar(IKernel? kernel)
        {
            if (kernel == null || !kernel.GetAllWorld().Any()) return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                List<IWorld> worlds = kernel.GetAllWorld().ToList();

                // ─────────────────────────────────────
                // World Select 드롭다운
                // ─────────────────────────────────────
                if (worlds.Count == 0)
                {
                    // 월드가 하나도 없을 때
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Popup(0, new[] { "No World (create in your bootstrap)" },
                            GUILayout.MaxWidth(240));
                    }
                }
                else
                {
                    var metaStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = false,
                        richText = true
                    };

                    GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
                    centeredLabelStyle.fontStyle = FontStyle.Normal;
                    centeredLabelStyle.fontSize = 10;
                    centeredLabelStyle.richText = true;

                    var worldCount = worlds.Count;
                    string countString = $"({worldCount})";
                    centeredLabelStyle.alignment = TextAnchor.LowerCenter;
                    GUILayout.Label(countString, centeredLabelStyle);

                    GUILayout.Space(2);

                    // currentWorld가 없으면 첫 번째 월드를 기본 선택으로 설정 (1회만)
                    if (kernel.CurrentWorld == null)
                    {
                        kernel.SetCurrentWorld(worlds[0]);
                    }

                    var currentWorld = kernel.CurrentWorld;

                    // 현재 월드 인덱스
                    int currentIndex = worlds.FindIndex(w => ReferenceEquals(w, currentWorld));
                    if (currentIndex < 0) currentIndex = 0;

                    // 드롭다운 옵션: World 이름 (없으면 Id 문자열)
                    string[] options = worlds
                        .Select(w =>
                        {
                            var name = string.IsNullOrEmpty(w.Name) ? "(unnamed)" : w.Name;
                            return name;
                        })
                        .ToArray();

                    int newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.MaxWidth(220));
                    if (newIndex != currentIndex)
                    {
                        _entityIdText = "";
                        _entityGenText = "0";
                        _findEntityId = null;
                        _findEntityGen = null;
                        _foundValid = false;
                        _findWatchedSystemsFold = false;
                        _findMode = false;

                        var selected = worlds[newIndex];
                        kernel.SetCurrentWorld(selected);
                        currentWorld = selected;
                    }

                    GUILayout.Space(2);

                    // ─────────────────────────────────────
                    // 선택된 월드 메타: ID / Name / Tags
                    // ─────────────────────────────────────
                    if (currentWorld != null)
                    {
                        string idText = currentWorld.Id.ToString();
                        string nameText = string.IsNullOrEmpty(currentWorld.Name) ? "(unnamed)" : currentWorld.Name;
                        string tagsText = (currentWorld.Tags.Count > 0)
                            ? string.Join(", ", currentWorld.Tags)
                            : "none";

                        centeredLabelStyle.alignment = TextAnchor.LowerLeft;
                        string meta =
                            $"Current World: {nameText} (Tags: {tagsText}) <color=#707070>[GUID: {idText}]</color>";
                        GUILayout.Label(meta, centeredLabelStyle, GUILayout.MaxWidth(600));
                    }
                }

                GUILayout.FlexibleSpace();

                // ─────────────────────────────────────
                // 우측 끝: + 버튼 (기존 기능 유지)
                // ─────────────────────────────────────
                var plusContent = GetPlusIconContent();

                var rPlus = GUILayoutUtility.GetRect(
                    plusContent,
                    EditorStyles.iconButton,
                    GUILayout.Width(24));

                rPlus.y += 2;

                if (GUI.Button(rPlus, plusContent, EditorStyles.iconButton))
                {
                    ShowPlusContextMenu(rPlus, kernel.CurrentWorld);
                }
            }
        }

        void ShowPlusContextMenu(Rect activatorRectGui, IWorld? world)
        {
            var menu = new GenericMenu();

            if (world == null)
            {
                // 월드 없으면 비활성 상태로만 노출
                menu.AddDisabledItem(new GUIContent("Add Entity from Blueprint... (no current World)"));
            }
            else
            {
                menu.AddItem(new GUIContent("Add Entity from Blueprint..."), false,
                    () => { ShowEntityBlueprintPicker(activatorRectGui); });
            }

            // --- Add Singleton ---
            if (world == null)
            {
                menu.AddDisabledItem(new GUIContent("Add Singleton... (no current World)"));
            }
            else
            {
                menu.AddItem(new GUIContent("Add Singleton..."), false, () =>
                {
                    // 전체 싱글톤 struct 타입 수집
                    var allSingletons = SingletonTypeFinder.All();

                    // 이미 world에 존재하는 싱글톤 타입들은 disabled 처리
                    var disabled = new HashSet<Type>();
                    try
                    {
                        foreach (var (t, _) in world.GetAllSingletons())
                        {
                            if (t != null) disabled.Add(t);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    ZenSingletonPickerWindow.Show(
                        allSingletonTypes: allSingletons,
                        disabled: disabled,
                        onPick: pickedType =>
                        {
                            try
                            {
                                // 기본값 생성 후 EditorCommand를 통해 추가
                                // (RemoveSingleton과 대칭되는 AddSingleton은
                                //  EditorCommand 쪽에 구현되어 있어야 한다고 가정)
                                var inst = ZenDefaults.CreateWithDefaults(pickedType);
                                world.ExternalCommandEnqueue(ExternalCommand.SetSingleton(pickedType, inst));

                                // 싱글톤 섹션 갱신
                                _hasSelectedSingleton = false;
                                _selectedSingletonType = null;
                                _selectedSingletonEntity = default;
                                Repaint();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        },
                        activatorRectGui: activatorRectGui,
                        title: "Add Singleton"
                    );
                });
            }

            if (world == null)
            {
                // 월드 없으면 비활성 상태로만 노출
                menu.AddDisabledItem(new GUIContent("Add System (no current World)"));
            }
            else
            {
                menu.AddItem(
                    new GUIContent("Add System..."),
                    false,
                    () =>
                    {
                        // 전체 System 타입 목록
                        var allSystemTypes = SystemTypeFinder.All().ToList();

                        // 이미 등록된 System 타입들은 disabled 처리
                        var disabled = new HashSet<Type>();
                        var existing = world.GetAllSystems();
                        if (existing != null)
                        {
                            foreach (var s in existing)
                            {
                                if (s == null) continue;
                                disabled.Add(s.GetType());
                            }
                        }

                        // Picker 오픈
                        ZenSystemPickerWindow.Show(
                            allSystemTypes: allSystemTypes,
                            disabled: disabled,
                            onPick: t =>
                            {
                                if (world == null) return; // 안전장치

                                try
                                {
                                    var inst = Activator.CreateInstance(t) as ISystem;
                                    if (inst == null)
                                    {
                                        Debug.LogError(
                                            $"ZenECS Explorer: Cannot create system of type {t.FullName}. " +
                                            "System must have a public parameterless constructor.");
                                        return;
                                    }

                                    // 실제 등록
                                    world.AddSystem(inst);

                                    // 선택 상태를 새로 추가된 System으로 이동
                                    var systems = world.GetAllSystems();
                                    if (systems != null)
                                    {
                                        int idx = -1;
                                        for (int i = 0; i < systems.Count; i++)
                                        {
                                            if (ReferenceEquals(systems[i], inst))
                                            {
                                                idx = i;
                                                break;
                                            }
                                        }

                                        if (idx >= 0)
                                        {
                                            _selSystem = idx;
                                            _selSysEntityCount = 0;
                                            _entityFold.Clear();
                                            _binderFold.Clear();
                                            _componentFold.Clear();
                                            _contextFold.Clear();
                                            _watchedFold.Clear();
                                            _cache.Clear();
                                        }
                                    }

                                    Repaint();
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                }
                            },
                            activatorRectGui: activatorRectGui,
                            title: "Add System",
                            onCancel: null
                        );
                    });
            }

            // ───────────── Add System Preset… ─────────────
            if (world == null)
            {
                menu.AddDisabledItem(new GUIContent("Add System Preset (no current World)"));
            }
            else if (ZenEcsUnityBridge.SystemPresetResolver == null)
            {
                menu.AddDisabledItem(new GUIContent("Add System Preset (no SystemPresetResolver)"));
            }
            else
            {
                menu.AddItem(
                    new GUIContent("Add System Preset..."),
                    false,
                    () => ShowSystemPresetPicker(activatorRectGui, world)
                );
            }

            // 버튼 기준으로 컨텍스트 드롭다운
            menu.DropDown(activatorRectGui);
        }

        void ShowEntityBlueprintPicker(Rect activatorRectGui)
        {
            ZenBlueprintPickerWindow.Show(
                activatorRectGui,
                onPick: CreateEntityFromBlueprint,
                title: "Create Entity from Blueprint"
            );
        }

        void CreateEntityFromBlueprint(EntityBlueprint blueprint)
        {
            if (blueprint == null) return;

            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null)
            {
                EditorUtility.DisplayDialog(
                    "ZenECS Kernel not ready",
                    "Kernel is not initialized. Please make sure ZenEcsUnityBridge has a running Kernel.",
                    "OK");
                return;
            }

            var world = kernel.CurrentWorld;
            if (world == null)
            {
                EditorUtility.DisplayDialog(
                    "No current World",
                    "Kernel.CurrentWorld is null. Please ensure a World is created and set as current.",
                    "OK");
                return;
            }

            var resolver = ZenEcsUnityBridge.SharedContextResolver;
            if (resolver == null)
            {
                EditorUtility.DisplayDialog(
                    "SharedContextResolver missing",
                    "ZenEcsUnityBridge.SharedContextResolver is null. Please configure a SharedContextResolver.",
                    "OK");
                return;
            }

            try
            {
                // EntityBlueprint API에 맞게 Spawn 호출
                // (이미 EntityBlueprintInspector에서 사용하던 Spawn 시그니처 기준)
                blueprint.Spawn(world, resolver);

                // 필요하면 Explorer 갱신
                //RefreshEntityListForCurrentSystem();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Spawn failed",
                    $"Failed to spawn Entity from '{blueprint.name}'.\nSee console for details.",
                    "OK");
            }
        }

        // =====================================================================
        //  PING HELPERS (System/Component/Context 파일 Ping)
        // =====================================================================

        static void PingSystemTypeNoSelect(Type t)
        {
            if (t == null) return;

            var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in scripts)
            {
                if (ms == null) continue;
                try
                {
                    if (ms.GetClass() == t)
                    {
                        // Selection은 건드리지 않고 Ping만
                        EditorGUIUtility.PingObject(ms);
                        return;
                    }
                }
                catch
                {
                    // 무시
                }
            }

            Debug.Log($"ZenECS Explorer: Could not locate script asset for system type {t.FullName}");
        }

        static void PingSystemType(Type t)
        {
            // 현재 로드된 모든 MonoScript 중에서 이 타입을 가진 스크립트 찾기
            var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in scripts)
            {
                if (ms == null) continue;
                try
                {
                    if (ms.GetClass() == t)
                    {
                        EditorGUIUtility.PingObject(ms);
                        Selection.activeObject = ms;
                        return;
                    }
                }
                catch
                {
                    // 일부 스크립트는 GetClass() 호출 시 예외 날 수 있음 → 무시
                }
            }

            Debug.Log($"EcsExplorer: Could not locate script asset for system type {t.FullName}");
        }

        static void PingComponentType(Type t)
        {
            if (t == null) return;

            // 시스템 Ping과 동일하게 MonoScript에서 타입을 찾아 Ping
            var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in scripts)
            {
                if (ms == null) continue;
                try
                {
                    if (ms.GetClass() == t)
                    {
                        // Selection은 유지하고 Ping만
                        EditorGUIUtility.PingObject(ms);
                        return;
                    }
                }
                catch
                {
                    // 일부 스크립트는 GetClass() 호출시 예외 발생 가능 → 무시
                }
            }

            Debug.Log(
                $"EcsExplorer: Unable to locate a script asset for component type {t.FullName}.\nIt may not exist, or a matching type name is required to ping the script source.");
        }

        static void PingContextType(Type t)
        {
            if (t == null) return;

            // 시스템 Ping과 동일하게 MonoScript에서 타입을 찾아 Ping
            var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in scripts)
            {
                if (ms == null) continue;
                try
                {
                    if (ms.GetClass() == t)
                    {
                        // Selection은 유지하고 Ping만
                        EditorGUIUtility.PingObject(ms);
                        return;
                    }
                }
                catch
                {
                    // 일부 스크립트는 GetClass() 호출시 예외 발생 가능 → 무시
                }
            }

            Debug.Log($"EcsExplorer: Could not locate script asset for component type {t.FullName}");
        }

        // =====================================================================
        //  BRIDGE ENTRY: SelectEntity (외부에서 호출)
        // =====================================================================

        /// <summary>
        /// Called from ZenEcsExplorerBridge to open the Explorer and focus a specific entity.
        /// </summary>
        public void SelectEntity(IWorld world, int entityId, int entityGen)
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null) return;

            _findEntityId = entityId;
            _findEntityGen = entityGen;

            // Explorer에서 현재 선택된 World로 검사한다.
            var currentWorld = kernel.CurrentWorld;
            if (currentWorld == null || currentWorld.Id != world.Id)
            {
                kernel.SetCurrentWorld(world);
                currentWorld = world;
            }

            _foundValid = currentWorld?.IsAlive(entityId, entityGen) ?? false;
            if (_foundValid)
            {
                _entityIdText = entityId.ToString();
                _entityGenText = entityGen.ToString();
                _findEntityId = entityId;
                _findEntityGen = entityGen;
                _foundEntity = _foundValid
                    ? (Entity)Activator.CreateInstance(typeof(Entity), entityId, entityGen)
                    : default;

                if (_foundValid)
                {
                    if (_entityFold.TryGetValue(_foundEntity, out var fold))
                    {
                        _findEntityFoldBackup = fold;
                        _entityFold[_foundEntity] = true;
                    }
                    else
                    {
                        _findEntityFoldBackup = false;
                        _entityFold.TryAdd(_foundEntity, true);
                    }
                }

                _findMode = true;
                Repaint();
                return;
            }

            _foundValid = false;
            _findWatchedSystemsFold = false;
            _findMode = true; // still enter to show guidance
        }
    }
}
#endif