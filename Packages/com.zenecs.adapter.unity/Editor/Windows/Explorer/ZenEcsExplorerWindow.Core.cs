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
            public const string LabelEntityId = "Entity ID:GEN ";       // LABEL_ENTITY_ID
            public const string BtnFind = "Find";                       // BTN_FIND
            public const string BtnClear = "Clear";                     // BTN_CLEAR_FILTER

            public const string TipFind =   // TIP_FIND
                "Show only the entity with this ID (no system switching).";

            public const string TipClear =  // TIP_CLEAR
                "Exit single-entity view and show all entities.";

            // 입력 문자열
            public string EntityIdText = string.Empty;      // _entityIdText
            public string EntityGenText = "0";              // _entityGenText

            // 파싱된 타겟
            public int? EntityId;               // _findEntityId
            public int? EntityGen;              // _findEntityGen

            // UI 상태
            public bool IsFindMode;             // _findMode
            public bool FoundValid;             // _foundValid
            public bool WatchedSystemsFold;     // _findWatchedSystemsFold
            public bool EntityFoldBackup;       // _findEntityFoldBackup

            public Entity FoundEntity;          // _foundEntity
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
            public Vector2 LeftScroll;      // _left
            public Vector2 RightScroll;     // _right

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
        readonly ExplorerFindState _findState = new();
        readonly ExplorerUiState _uiState = new();

        // =====================================================================
        //  기타 캐시 / enum
        // =====================================================================

        // Fixed / Variable / Presentation 구분
        enum PhaseKind
        {
            Unknown,
            Deterministic,
            NonDeterministic,
        }
        
        private double _nextRepaint;
        private const float _repaintInterval = 0.25f;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================

        void ClearState(bool repaint = true)
        {
            _uiState.ClearSelection();

            _findState.IsFindMode         = false;
            _findState.EntityId           = null;
            _findState.EntityGen          = null;
            _findState.FoundValid         = false;
            _findState.WatchedSystemsFold = false;

            _uiState.ClearTreeFoldouts();
            
            if (repaint) Repaint();
        }
        
        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;

            ClearState();

            _nextRepaint = EditorApplication.timeSinceStartup + _repaintInterval;            
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
                _nextRepaint = EditorApplication.timeSinceStartup + _repaintInterval;
                Repaint();
            }
        }

        void OnBeforeReload()
        {
            ClearState();
        }

        void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                ClearState();
            }
        }

        // =====================================================================
        //  OnGUI (기존 버전 그대로 사용)
        // =====================================================================

        void OnGUI()
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null)
            {
                DrawKernelNotReadyOverlay();
                return;
            }

            if (!kernel.GetAllWorld().Any())
            {
                DrawNoWorldOverlay();
                return;
            }

            var world = kernel.CurrentWorld;
            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            // 🔹 맨 위 상단 바
            DrawTopToolbar(kernel);
            EditorGUILayout.Space(2);

            // 🔹 FIND MODE
            if (_findState.IsFindMode)
            {
                DrawFindMode(kernel, world, systems);
                return;
            }
            
            // 🔹 일반 모드
            DrawMainLayout(kernel, world, systems);
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

        void DrawNoWorldOverlay()
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
                    GUILayout.Label("ZenECS No active world.", titleStyle);
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
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_uiState.RightScroll))
                {
                    _uiState.RightScroll = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // 상단 Close 버튼
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Close", GUILayout.Width(80)))
                            {
                                _findState.IsFindMode = false;
                                _findState.EntityId = null;
                                _findState.EntityGen = null;
                                _findState.FoundValid = false;
                                return;
                            }
                        }

                        // 결과 표시
                        DrawFindResult(world, systems);
                    }
                }
            }
            
            GUILayout.Space(4);
            DrawFooter(kernel);
        }

        void TryResolveFindTarget(IWorld? world)
        {
            if (world == null) return;
            
            _findState.FoundValid = false;

            if (!int.TryParse(_findState.EntityIdText, out var id))
                return;

            if (!int.TryParse(_findState.EntityGenText, out var gen))
                gen = 0;

            _findState.EntityId  = id;
            _findState.EntityGen = gen;

            var e = new Entity(id, gen);
            if (!world.IsAlive(e))
            {
                _findState.FoundValid = false;
                return;
            }

            _findState.FoundEntity = e;
            _findState.FoundValid  = true;
        }

        void DrawFindResult(IWorld? world, IReadOnlyList<ISystem>? systems)
        {
            if (world == null) return;
            
            if (!_findState.EntityId.HasValue || !_findState.EntityGen.HasValue)
            {
                EditorGUILayout.HelpBox("Enter a valid Entity ID and GEN.",
                    MessageType.Info);
                return;
            }

            if (!_findState.FoundValid)
            {
                EditorGUILayout.HelpBox(
                    $"Entity #{_findState.EntityId.Value}:{_findState.EntityGen.Value} not found.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"Entity #{_findState.EntityId.Value}:{_findState.EntityGen.Value}",
                EditorStyles.boldLabel);

            GUILayout.Space(2);

            // Watched Systems (위쪽)
            if (systems != null)
            {
                var watchedList = CollectWatchedSystemsForEntity(world, _findState.FoundEntity, systems);
                if (watchedList.Count > 0)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var foldStyle = new GUIStyle(EditorStyles.foldout);

                        _findState.WatchedSystemsFold = EditorGUILayout.Foldout(
                            _findState.WatchedSystemsFold,
                            $"Watched Systems ({watchedList.Count})",
                            true,
                            foldStyle);

                        if (_findState.WatchedSystemsFold)
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
            DrawOneEntity(world, _findState.FoundEntity);
        }

        // =====================================================================
        //  DRAW: MAIN LAYOUT (좌 System / 우 Entity / 하단 Footer)
        // =====================================================================

        void DrawMainLayout(IKernel kernel, IWorld? world, IReadOnlyList<ISystem>? systems)
        {
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
            using var sv = new EditorGUILayout.ScrollViewScope(_uiState.LeftScroll, GUILayout.Width(300));
            _uiState.LeftScroll = sv.scrollPosition;
            
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
                    _uiState.ClearSelection();
                }
            }
            
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
            using var sv = new EditorGUILayout.ScrollViewScope(_uiState.RightScroll);
            _uiState.RightScroll = sv.scrollPosition;

            EditorGUILayout.Space(4);

            bool hasSystem = systems != null &&
                             _uiState.SelectedSystemIndex >= 0 &&
                             _uiState.SelectedSystemIndex < (systems?.Count ?? 0);

            bool hasSingleton = false;
            if (_uiState.HasSelectedSingleton && world != null)
            {
                hasSingleton = world.IsAlive(_uiState.SelectedSingletonEntity);
                if (!hasSingleton)
                {
                    // 소멸된 싱글톤 엔티티는 선택 해제
                    _uiState.HasSelectedSingleton = false;
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
                DrawSingletonDetail(world, _uiState.SelectedSingletonType, _uiState.SelectedSingletonEntity);
            }
            else
            {
                // =========================
                // 정상 리스트 모드 (시스템 선택 있음)
                // =========================

                var sys = systems![_uiState.SelectedSystemIndex];

                // 🔹 1) System Meta 박스
                DrawSystemMeta(sys, world);

                EditorGUILayout.Space(6);

                // 🔹 2) Entities 헤더 + 리스트
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_uiState.SelectedSystemEntityCount > 0)
                        EditorGUILayout.LabelField($"Entities ({_uiState.SelectedSystemEntityCount})", EditorStyles.boldLabel);
                    else
                        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);

                var tmp = new List<Entity>();
                if (!WatchQueryRunner.TryCollectByWatch(sys, world,
                        tmp))
                    EditorGUILayout.HelpBox(
                        "No inspector. Implement IInspectableSystem or add [Watch].",
                        MessageType.Info);

                _uiState.SelectedSystemEntityCount = tmp.Count;

                foreach (var e in tmp.Distinct())
                    if (world != null)
                        DrawOneEntity(world, e);
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
                    if (!_uiState.WatchedFold.TryGetValue(key, out var open))
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
                        _uiState.WatchedFold[key] = open;

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
                        ClearState();

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
            if (world == null) return;
            
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Add Entity from Blueprint..."), false,
                () => { ShowEntityBlueprintPicker(activatorRectGui); });

            // --- Add Singleton ---
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

            // ───────────── Add System Preset… ─────────────
            if (ZenEcsUnityBridge.SystemPresetResolver == null)
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

            _findState.EntityId = entityId;
            _findState.EntityGen = entityGen;

            // Explorer에서 현재 선택된 World로 검사한다.
            var currentWorld = kernel.CurrentWorld;
            if (currentWorld == null || currentWorld.Id != world.Id)
            {
                kernel.SetCurrentWorld(world);
                currentWorld = world;
            }

            _findState.FoundValid = currentWorld?.IsAlive(entityId, entityGen) ?? false;
            if (_findState.FoundValid)
            {
                _findState.EntityIdText = entityId.ToString();
                _findState.EntityGenText = entityGen.ToString();
                _findState.EntityId = entityId;
                _findState.EntityGen = entityGen;
                _findState.FoundEntity = _findState.FoundValid
                    ? (Entity)Activator.CreateInstance(typeof(Entity), entityId, entityGen)
                    : default;

                if (_findState.FoundValid)
                {
                    if (_uiState.EntityFold.TryGetValue(_findState.FoundEntity, out var fold))
                    {
                        _findState.EntityFoldBackup = fold;
                        _uiState.EntityFold[_findState.FoundEntity] = true;
                    }
                    else
                    {
                        _findState.EntityFoldBackup = false;
                        _uiState.EntityFold.TryAdd(_findState.FoundEntity, true);
                    }
                }

                _findState.IsFindMode = true;
                Repaint();
                return;
            }

            _findState.FoundValid = false;
            _findState.WatchedSystemsFold = false;
            _findState.IsFindMode = true;
        }
    }
}
#endif