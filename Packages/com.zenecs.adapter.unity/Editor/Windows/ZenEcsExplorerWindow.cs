#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using ZenECS.EditorCommands;
using ZenECS.EditorCommon;
using ZenECS.EditorRoot;

namespace ZenECS.EditorWindows
{
    public sealed class ZenEcsExplorerWindow : EditorWindow
    {
        [MenuItem("ZenECS/Tools/ZenECS Explorer")]
        public static void Open() => GetWindow<ZenEcsExplorerWindow>("ZenECS Explorer");

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

        readonly List<Entity> _cache = new(256);
        double _nextRepaint;
        private int _selSysEntityCount;
        private static GUIStyle? _bigPlusButton;
        static bool _bigPlusReady;

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

        // Fixed / Variable / Presentation 구분
        enum PhaseKind
        {
            Unknown,
            Deterministic,
            NonDeterministic,
        }

        // System.Enabled 리플렉션 캐시
        static readonly Dictionary<Type, PropertyInfo?> _systemEnabledPropCache = new();

        readonly Dictionary<Entity, bool> _entityFold = new();    // entityId → fold
        readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _binderFold = new();    // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _contextFold = new();   // $"{entityId}:{typeName}:CTX" → fold
        bool _editMode = true;

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
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
                _nextRepaint = EditorApplication.timeSinceStartup + 0.25;
                Repaint();
            }
        }

        void OnBeforeReload()
        {
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

        void OnGUI()
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null || !kernel.GetAllWorld().Any())
            {
                DrawKernelNotReadyOverlay();
                return;
            }

            var world = kernel.CurrentWorld;

            // 🔹 맨 위 상단 바
            DrawTopToolbar(kernel);
            EditorGUILayout.Space(2);

            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            // =====================================================
            // 1) FIND MODE: 좌/우 패널 없이 전체 폭으로 Find 뷰만
            // =====================================================
            if (_findMode)
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_right))
                {
                    _right = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // 🔹 맨 위 가운데 Close 버튼
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUIStyle centeredButtonStyle = EditorStyles.toolbarButton;
                            centeredButtonStyle.fontStyle = FontStyle.Normal;
                            centeredButtonStyle.fontSize = 20;

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("BACK", centeredButtonStyle, GUILayout.Width(80)))
                            {
                                _entityFold[_foundEntity] = _findEntityFoldBackup;

                                _entityIdText = "";
                                _findEntityId = null;

                                _entityGenText = "0";
                                _findEntityGen = null;

                                _findWatchedSystemsFold = false;
                                _foundValid = false;
                                _findMode = false;
                                Repaint();
                                return;
                            }

                            GUILayout.FlexibleSpace();
                        }

                        EditorGUILayout.Space(6);

                        // 🔹 Watched Systems 목록 (Back 버튼 바로 아래)
                        if (world != null && systems != null && _foundValid)
                        {
                            var watchedList = CollectWatchedSystemsForEntity(world, _foundEntity, systems);
                            if (watchedList.Count > 0)
                            {
                                using (new EditorGUILayout.VerticalScope("box"))
                                {
                                    var leftFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                                        { };

                                    leftFoldoutStyle.focused.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.onFocused.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.hover.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.onHover.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.active.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.onActive.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.normal.textColor = systemMetaTextColor;
                                    leftFoldoutStyle.onNormal.textColor = systemMetaTextColor;

                                    // Foldout 헤더: "Watched Systems (N)"
                                    _findWatchedSystemsFold = EditorGUILayout.Foldout(
                                        _findWatchedSystemsFold,
                                        $"Watched Systems ({watchedList.Count})",
                                        true,
                                        leftFoldoutStyle
                                    );

                                    if (_findWatchedSystemsFold)
                                    {
                                        EditorGUI.indentLevel++;

                                        // Watched Components와 동일한 회색 네임스페이스 스타일
                                        var nsStyle = new GUIStyle(EditorStyles.miniLabel)
                                        {
                                            wordWrap = true,
                                            fontSize = 10,
                                            padding = new RectOffset(0, 0, 0, 0),
                                            richText = true,
                                            normal =
                                            {
                                                textColor = systemMetaTextColor
                                            }
                                        };

                                        foreach (var (sys, tSys) in watchedList)
                                        {
                                            if (tSys == null) continue;
                                            string ns = string.IsNullOrEmpty(tSys.Namespace)
                                                ? "(global)"
                                                : tSys.Namespace;

                                            using (new EditorGUILayout.HorizontalScope())
                                            {
                                                // System 이름
                                                EditorGUILayout.LabelField($"{tSys.Name} <color=#707070>[{ns}]</color>",
                                                    nsStyle);
                                                // EditorGUILayout.LabelField(tSys.Name, GUILayout.ExpandWidth(false));
                                                //
                                                // // [namespace] 어두운 회색
                                                // EditorGUILayout.LabelField($"[{ns}]", nsStyle, GUILayout.ExpandWidth(true));

                                                // 돋보기 아이콘 (우측 끝)
                                                var icon = GetSearchIconContent("Ping system script asset");
                                                if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
                                                        GUILayout.Height(16)))
                                                {
                                                    // 선택은 유지하고 Ping만
                                                    PingSystemTypeNoSelect(tSys);
                                                }
                                            }
                                        }

                                        EditorGUI.indentLevel--;
                                    }
                                }

                                EditorGUILayout.Space(6);
                            }
                        }

                        // 아래는 기존 Entity 표시 로직 그대로
                        if (world == null)
                        {
                            EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
                        }
                        else if (_findEntityId.HasValue && _findEntityGen.HasValue)
                        {
                            if (_foundValid)
                            {
                                EditorGUILayout.LabelField(
                                    $"Entity #{_findEntityId.Value}:{_findEntityGen.Value}",
                                    EditorStyles.boldLabel);

                                GUILayout.Space(2);

                                DrawOneEntity(world, _foundEntity);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    $"No entity with ID {_findEntityId.Value}:{_findEntityGen.Value} in {world.Name} World.",
                                    MessageType.Info
                                );
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                "Please enter a valid positive numeric Entity ID/GEN.",
                                MessageType.Warning
                            );
                        }
                    }
                }

                GUILayout.Space(4);
                DrawFooter(kernel);
                return;
            }

            // =====================================================
            // 2) 일반 모드: 좌측 Systems + 세로 구분선 + 우측 Entities
            // =====================================================
            using (new EditorGUILayout.HorizontalScope())
            {
                // ---------- Left: Systems ----------
                using (var sv = new EditorGUILayout.ScrollViewScope(_left, GUILayout.Width(300)))
                {
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
                }

                EditorGUILayout.Space(4);

                // ---------- Vertical Separator ----------
                {
                    var sepRect = GUILayoutUtility.GetRect(
                        1f, 1f,
                        GUILayout.ExpandHeight(true),
                        GUILayout.Width(1f)
                    );

                    var sepColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f, 1f)
                        : new Color(0.6f, 0.6f, 0.6f, 1f);

                    sepColor = Color.black;
                    EditorGUI.DrawRect(sepRect, sepColor);
                }

                // ---------- Right: Entities ----------
                using (var sv = new EditorGUILayout.ScrollViewScope(_right))
                {
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

                            if (!ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world,
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
            }

            GUILayout.Space(4);
            DrawFooter(kernel);
        }

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

        /// <summary>
        /// Draws a full-window overlay message when the ECS Kernel / World is not yet ready.
        /// </summary>
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

            Debug.Log($"EcsExplorer: Unable to locate a script asset for component type {t.FullName}.\nIt may not exist, or a matching type name is required to ping the script source.");
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
                                ZenEcsEditor.CommandQueue.Enqueue(EditorCommand.SetSingleton(pickedType, inst));

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

        /// <summary>
        /// Top toolbar의 + 버튼 기준으로 EntityBlueprint picker를 띄운다.
        /// </summary>
        void ShowEntityBlueprintPicker(Rect activatorRectGui)
        {
            ZenBlueprintPickerWindow.Show(
                activatorRectGui,
                onPick: CreateEntityFromBlueprint,
                title: "Create Entity from Blueprint"
            );
        }

        /// <summary>
        /// 선택된 EntityBlueprint로 현재 Kernel.World에 Entity를 Spawn.
        /// </summary>
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

        void OnClickAddEntity()
        {
            // TODO: 실제 Entity 생성 로직으로 교체
            Debug.Log("ZenECS Explorer: Add Entity clicked.");
        }

        void OnClickAddSystem()
        {
            // TODO: 실제 System 추가 / 등록 UI로 교체
            Debug.Log("ZenECS Explorer: Add System clicked.");
        }

        static GUIContent GetPlusIconContent()
        {
            // Unity 기본 검색 아이콘
            var gc = EditorGUIUtility.IconContent("d_CreateAddNew");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("CreateAddNew");

            if (gc == null)
                gc = new GUIContent("+");

            return gc;
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            // Unity 기본 검색 아이콘
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("Search Icon");

            // 혹시 아이콘을 못 찾았을 경우 텍스트로 fallback
            if (gc == null)
                gc = new GUIContent("🔍", tooltip);
            else
                gc.tooltip = tooltip;

            return gc;
        }

        private void DrawFooter(IKernel? kernel)
        {
            if (kernel?.CurrentWorld == null) return;
            
            var world = kernel.CurrentWorld;
            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // ===== Global Pause 토글 버튼 (커널 기반, System Pause와 동일 스타일) =====
                var isPaused = kernel.IsPaused;

                // 툴바 라인 높이에 맞춘 Rect
                var rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                var pauseRect = GUILayoutUtility.GetRect(24f, rowHeight, GUILayout.Width(24f));

                using (new EditorGUI.DisabledScope(false))
                {
                    // System 리스트에서 쓰는 것과 동일한 버튼 영역 보정
                    var btnRect = new Rect(
                        pauseRect.x,
                        pauseRect.y + 1f,
                        pauseRect.width,
                        pauseRect.height - 2f
                    );

                    // Unity 기본 Pause 아이콘
                    var pauseContent = EditorGUIUtility.IconContent("PauseButton");
                    if (pauseContent == null || pauseContent.image == null)
                        pauseContent = EditorGUIUtility.TrTextContent("⏸");

                    // 항상 같은 스타일을 사용해서 크기/모양 변화 없게
                    var pauseStyle = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };

                    var oldBg = GUI.backgroundColor;
                    var oldCont = GUI.contentColor;

                    if (isPaused)
                    {
                        // Unity 툴바 Pause랑 비슷한 파란색
                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                            ? new Color(0.24f, 0.48f, 0.90f, 1f)  // Dark Skin
                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                        GUI.contentColor = Color.white;
                    }

                    if (GUI.Button(btnRect, pauseContent, pauseStyle))
                    {
                        kernel.TogglePause();
                    }

                    GUI.backgroundColor = oldBg;
                    GUI.contentColor = oldCont;
                }

                GUILayout.Space(4);

                // ===== 기존 정보 라벨들 =====
                var elapsed = kernel.SimulationAccumulatorSeconds;

                // Create a custom GUIStyle for the label
                GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
                centeredLabelStyle.alignment = TextAnchor.LowerCenter;
                centeredLabelStyle.fontStyle = FontStyle.Normal;
                centeredLabelStyle.fontSize = 10;

                GUILayout.Label($"Since running in seconds: {elapsed:0}", centeredLabelStyle);

                GUILayout.FlexibleSpace();

                GUILayout.Label(LABEL_ENTITY_ID, centeredLabelStyle, GUILayout.Width(70));

                GUIStyle tfStyle = new GUIStyle(GUI.skin.textField);
                tfStyle.alignment = TextAnchor.LowerLeft;
                tfStyle.fontStyle = FontStyle.Normal;
                tfStyle.fontSize = 10;

                _entityIdText = GUILayout.TextField(_entityIdText, tfStyle, GUILayout.Width(40));
                _entityIdText = new string(_entityIdText.Where(char.IsDigit).ToArray());

                _entityGenText = GUILayout.TextField(_entityGenText, tfStyle, GUILayout.Width(40));
                _entityGenText = new string(_entityGenText.Where(char.IsDigit).ToArray());

                if (int.TryParse(_entityGenText, out var gen))
                {
                    if (gen < 0) _entityGenText = "0";
                }

                GUIStyle centeredButtonStyle = new GUIStyle(GUI.skin.button);
                centeredButtonStyle.alignment = TextAnchor.MiddleCenter;
                centeredButtonStyle.fontStyle = FontStyle.Normal;
                centeredButtonStyle.fontSize = 10;
                // Find => enter single-entity view (no system switching)
                var contentFind = new GUIContent(BTN_FIND, TIP_FIND);
                if (GUILayout.Button(contentFind, centeredButtonStyle, GUILayout.Width(56)))
                {
                    if (int.TryParse(_entityIdText, out var id) && id > 0)
                    {
                        _findEntityId = id;

                        if (int.TryParse(_entityGenText, out var gen2))
                        {
                            _findEntityGen = gen2;
                        }

                        _foundValid = world?.IsAlive(id, gen2) ?? false;
                        _foundEntity =
                            _foundValid ? (Entity)Activator.CreateInstance(typeof(Entity), id, gen) : default;

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
                    }
                    else
                    {
                        _findEntityId = null;
                        _findEntityGen = null;
                        _foundValid = false;
                        _findWatchedSystemsFold = false;
                        _findMode = true; // still enter to show guidance
                    }

                    Repaint();
                }

                // Clear Filter => exit single-entity view
                var contentClear = new GUIContent(BTN_CLEAR_FILTER, TIP_CLEAR);
                if (GUILayout.Button(contentClear, centeredButtonStyle, GUILayout.Width(60)))
                {
                    if (_findMode)
                    {
                        _entityFold[_foundEntity] = _findEntityFoldBackup;
                    }

                    _entityIdText = "";
                    _entityGenText = "0";
                    _findEntityId = null;
                    _findEntityGen = null;
                    _foundValid = false;
                    _findWatchedSystemsFold = false;
                    _findMode = false;
                    Repaint();
                }

                _editMode = GUILayout.Toggle(_editMode, "Edit", centeredButtonStyle, GUILayout.Width(60));
            }
        }

        static void EnsureBigPlusStyle()
        {
            if (_bigPlusReady) return;
            _bigPlusReady = true;

            _bigPlusButton = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 16, // 기본보다 크게
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = 22f, // 엔티티 헤더 버튼 폭과 비슷하게
                fixedHeight = 18f
            };
        }

        // --- Render a single entity box (reused by list mode & single view mode) ---
        void DrawOneEntity(IWorld world, Entity e)
        {
            _entityFold.TryAdd(e, false);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // ===== Entity header =====
                var headRect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight + 6f,
                    GUILayout.ExpandWidth(true));
                bool openE = _entityFold[e];

                var entityTitle = $"Entity #{e.Id}:{e.Gen}";
                var isSingleton = world.HasSingleton(e);
                if (isSingleton)
                {
                    entityTitle += " <color=#999900><size=10>SINGLETON</size></color>";
                }

                ZenFoldoutHeader.DrawRow(ref openE, headRect, entityTitle, "", rRight =>
                {
                    var style = EditorStyles.miniButton;

                    const float wBtn = 20f;
                    const float gap = 3f;

                    var hBtn = Mathf.Ceil(EditorGUIUtility.singleLineHeight + 2f);
                    var yBtn = rRight.y + Mathf.Max(0f, (rRight.height - hBtn) * 0.5f);

                    var right = rRight.xMax - 4f;

                    // 맨 오른쪽: 삭제 X
                    var rDel = new Rect(right - wBtn, yBtn, wBtn, hBtn);
                    right = rDel.x - gap;

                    // // 그 왼쪽: 메인 뷰 선택 버튼 (•)
                    // Rect? rSel = null;
                    // if (EcsExplorerActions.TryGetEntityMainView(world, e, out var go))
                    // {
                    //     rSel = new Rect(right - wBtn + 2, yBtn + 1, wBtn, hBtn);
                    //     right = rSel.Value.x - gap;
                    // }

                    using (new EditorGUI.DisabledScope(!_editMode))
                    {
                        if (GUI.Button(rDel, "X", style))
                        {
                            var msg = $"Remove this entity?\n\nEntity #{e.Id}:{e.Gen}";
                            if (isSingleton)
                            {
                                msg = $"Remove this singleton?\n\nSingleton Entity #{e.Id}:{e.Gen}";
                            }

                            if (EditorUtility.DisplayDialog(
                                    "Remove Entity",
                                    msg,
                                    "Yes", "No"))
                            {
                                ZenEcsEditor.CommandQueue.Enqueue(EditorCommand.DestroyEntity(e));
                                
                                _entityFold[_foundEntity] = _findEntityFoldBackup;

                                _entityIdText = "";
                                _findEntityId = null;

                                _entityGenText = "0";
                                _findEntityGen = null;

                                _findWatchedSystemsFold = false;
                                _foundValid = false;
                                _findMode = false;
                                Repaint();
                            }
                        }
                    }

                    // var gcPing = GetSearchIconContent("Ping entity view in Hierarchy");
                    // if (rSel.HasValue && GUI.Button(rSel.Value, gcPing, EditorStyles.iconButton))
                    // {
                    //     EcsExplorerActions.TrySelectEntityMainView(go);
                    // }
                }, true, false);

                _entityFold[e] = openE;

                if (!openE) return;

                // ===== Components Summary line =====
                var line = EditorGUIUtility.singleLineHeight;
                var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

                var compsEnum = world.GetAllComponents(e);
                var arr = compsEnum.ToArray();

// Arrow toggle (open/close all visible components)
                var rArrow = new Rect(r.x + 3, r.y + 1, 18f, r.height - 2);

                const float addWComp = 20f;
                const float gapComp = 6f;

// 오른쪽 끝 Add Component 버튼 영역
                var rAddComp = new Rect(r.xMax - addWComp - 1.5f, r.y + 2, addWComp, r.height);

// 라벨 영역: 화살표 + Add 버튼을 제외한 부분
                var rLabel = new Rect(
                    rArrow.xMax - 1f,
                    r.y,
                    r.width - (rArrow.width + addWComp + gapComp + 4f),
                    r.height
                );

                var allOpen = AreAllComponentsOpen_VisibleOnly(e, arr);

                // 전체 컴포넌트 접기/펼치기 토글
                EditorGUI.BeginChangeCheck();
                var visNext = EditorGUI.Foldout(rArrow, allOpen, GUIContent.none, false);
                EditorGUIUtility.AddCursorRect(rArrow, MouseCursor.Link);
                if (EditorGUI.EndChangeCheck())
                {
                    SetAllComponentsFold(world, e, visNext);
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                // 라벨
                if (isSingleton)
                {
                    EditorGUI.LabelField(rLabel, $"Component");
                }
                else
                {
                    EditorGUI.LabelField(rLabel, $"Components: {arr.Length}");
                }

                // Add Component 버튼 (이제 여기!)
                if (!isSingleton)
                {
                    using (new EditorGUI.DisabledScope(!_editMode))
                    {
                        if (GUI.Button(rAddComp, GetPlusIconContent(), EditorStyles.iconButton))
                        {
                            var all = ZenECS.EditorCommon.ZenComponentPickerWindow.FindAllZenComponents().ToList();
                            var disabled = new HashSet<Type>();
                            foreach (var (tHave, _) in world.GetAllComponents(e))
                                disabled.Add(tHave);

                            ZenComponentPickerWindow.Show(
                                all,
                                disabled,
                                picked =>
                                {
                                    var inst = ZenDefaults.CreateWithDefaults(picked);
                                    if (inst != null)
                                    {
                                        ZenEcsEditor.CommandQueue.Enqueue(
                                            EditorCommand.AddComponent(e, inst.GetType(), inst));
                                    }

                                    Repaint();
                                },
                                rAddComp, // 이제 Components 줄 오른쪽 Rect 기준으로
                                $"Entity #{e.Id}:{e.Gen} Add Component"
                            );
                        }
                    }
                }

                // 실제 리스트 렌더링
                DrawComponentsList(world, e, isSingleton, arr);

                // ===== Components 끝난 직후 바로 아래에 추가 =====
                {
                    // ===== Contexts Summary line =====
                    if (ContextApi.TryGetAll(world, e, out var ctxs))
                    {
                        var lineC = EditorGUIUtility.singleLineHeight;
                        var rc = GUILayoutUtility.GetRect(10, lineC, GUILayout.ExpandWidth(true));

                        var rArrowC = new Rect(rc.x + 3, rc.y + 1, 18f, rc.height - 2);

                        const float addW = 20f;
                        const float gap = 6f;
                        var rAddC = new Rect(rc.xMax - addW - 1.5f, rc.y + 2, addW, rc.height);
                        var rLabelC = new Rect(
                            rArrowC.xMax - 1f, rc.y,
                            rc.width - (rArrowC.width + addW + gap + 4f),
                            rc.height
                        );

                        // 모든 컨텍스트가 열려 있는지 (Components와 동일 패턴)
                        var allOpen_ = AreAllContextsOpen(e, ctxs);

                        EditorGUI.BeginChangeCheck();
                        var visNextCtx = EditorGUI.Foldout(rArrowC, allOpen_, GUIContent.none, false);
                        EditorGUIUtility.AddCursorRect(rArrowC, MouseCursor.Link);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 루트 토글 → 전체 컨텍스트 접기/펼치기
                            SetAllContextsFold(world, e, visNextCtx);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        EditorGUI.LabelField(rLabelC, $"Contexts: {ctxs.Length}");

                        // Add Context 버튼 (기존 로직 유지)
                        using (new EditorGUI.DisabledScope(!_editMode))
                        {
                            if (GUI.Button(rAddC, GetPlusIconContent(), EditorStyles.iconButton))
                            {
                                // 이미 붙어있는 컨텍스트 타입 집합
                                var disabledCtxTypes = new HashSet<Type>(ctxs.Select(c => c.type));

                                ContextAssetPickerWindow.Show(
                                    activatorRectGui: rAddC,
                                    onPick: asset =>
                                    {
                                        ContextApi.AddFromAsset(world, e, asset);
                                        Repaint();
                                    },
                                    disabledContextTypes: disabledCtxTypes,
                                    title: $"Entity #{e.Id}:{e.Gen} - Add Context"
                                );
                            }
                        }

                        // 🔴 항상 DrawContextsList 호출 → 이름/네임스페이스는 언제나 표시
                        DrawContextsList(world, e, ctxs);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Contexts API has been disconnected.",
                            MessageType.None);
                    }
                }


                {
                    // ===== Binders Summary line =====
                    var bindersOk = BinderApi.TryGetAll(world, e, out var binders);
                    if (!bindersOk)
                    {
                        EditorGUILayout.HelpBox(
                            "Binders API has been disconnected.",
                            MessageType.None);
                    }
                    else
                    {
                        var line2 = EditorGUIUtility.singleLineHeight;
                        var r2 = GUILayoutUtility.GetRect(10, line2, GUILayout.ExpandWidth(true));

                        // Fold-all for binders (보이는 것만)
                        var rArrow2 = new Rect(r2.x + 3, r2.y + 1, 18f, r2.height - 2);

                        const float addW = 20f; // 버튼 폭
                        const float gap = 6f;

                        // 오른쪽 끝 Add 버튼 영역
                        var rAdd = new Rect(r2.xMax - addW - 1.5f, r2.y + 2, addW, r2.height);
                        // 라벨은 버튼과 화살표를 제외한 영역
                        var rLabel2 = new Rect(rArrow2.xMax - 1f, r2.y, r2.width - (rArrow2.width + addW + gap + 4f),
                            r2.height);

                        bool allOpenB = AreAllBindersOpen_VisibleOnly(e, binders);

                        // 화살표 토글
                        EditorGUI.BeginChangeCheck();
                        var visNextB = EditorGUI.Foldout(rArrow2, allOpenB, GUIContent.none, false);
                        EditorGUIUtility.AddCursorRect(rArrow2, MouseCursor.Link);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetAllBindersFold(world, e, visNextB);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        // 라벨
                        EditorGUI.LabelField(rLabel2, $"Binders: {binders.Length}");

                        // Add Binder 버튼
                        using (new EditorGUI.DisabledScope(!_editMode || !BinderApi.CanAdd(world)))
                        {
                            if (GUI.Button(rAdd, GetPlusIconContent(), EditorStyles.iconButton))
                            {
                                // 전체 바인더 타입 수집
                                var allBinders = BinderTypeFinder.All();
                                // 이미 붙어있는 타입은 disabled 처리
                                var disabledB = new HashSet<Type>(binders.Select(x => x.type));

                                ZenBinderPickerWindow.Show(
                                    allBinderTypes: allBinders,
                                    disabled: disabledB,
                                    onPick: picked =>
                                    {
                                        var inst = ZenDefaults.CreateWithDefaults(picked);
                                        if (inst != null) BinderApi.Add(world, e, inst);
                                        Repaint();
                                    },
                                    activatorRectGui: rAdd,
                                    title: $"Entity #{e.Id}:{e.Gen} - Add Binder"
                                );
                            }
                        }

                        // 실제 리스트
                        DrawBindersList(world, e, binders);
                    }
                }
            }
        }

        void SetAllContextsFold(IWorld world, Entity e, bool open)
        {
            if (!ContextApi.TryGetAll(world, e, out var ctxs)) return;

            foreach (var (t, _) in ctxs)
            {
                if (t == null) continue;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:CTX";
                _contextFold[key] = open;
            }
        }

        bool AreAllContextsOpen(Entity e, (Type type, object? boxed)[] ctxs)
        {
            bool any = false;

            foreach (var (t, _) in ctxs)
            {
                if (t == null) continue;

                any = true;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:CTX";

                // 컴포넌트와 동일 패턴:
                // - 키가 없거나
                // - false 이면
                //   => "다 안 열려 있음"으로 판단
                if (!_contextFold.TryGetValue(key, out var open) || !open)
                    return false;
            }

            return any && true;
        }

        void SetAllBindersFold(IWorld world, Entity e, bool open)
        {
            if (!BinderApi.TryGetAll(world, e, out var arr)) return;
            foreach (var (t, boxed) in arr)
            {
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                _binderFold[key] = open;
            }
        }

        bool AreAllBindersOpen_VisibleOnly(Entity e, (Type type, object? boxed)[] binders)
        {
            bool any = false;
            foreach (var (t, boxed) in binders)
            {
                if (boxed != null && !CanShowBinderBody(t, boxed)) continue;
                any = true;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                if (!_binderFold.TryGetValue(key, out bool open) || !open)
                    return false;
            }

            return any && true;
        }

        static class BinderIntrospection
        {
            static bool IsBindsInterface(Type t)
                => t.IsInterface && t.IsGenericType && t.Name.StartsWith("IBind", StringComparison.Ordinal);

            public static IReadOnlyList<Type> ExtractObservedComponentTypes(Type binderType)
            {
                var set = new HashSet<Type>();
                foreach (var itf in binderType.GetInterfaces())
                {
                    if (!IsBindsInterface(itf)) continue;

                    // 패턴 허용: IBind<T>, IBind<TDelta>, IBind<TComp,TDelta> 등
                    foreach (var ga in itf.GetGenericArguments())
                    {
                        // 컨텍스트/바인더/시스템 등은 제외하려 시도(휴리스틱)
                        if (ga.IsAbstract) continue;
                        if (ga.IsInterface && ga.Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        if (ga.Namespace?.EndsWith(".Editor", StringComparison.Ordinal) == true) continue;

                        set.Add(ga);
                    }
                }

                return set.OrderBy(t => t.Name).ToArray();
            }
        }

        static bool TryHasContext(IWorld w, Entity e, Type ctxType, out bool available)
        {
            available = w.HasContext(e, ctxType);
            return available;
        }

        class OkLabel
        {
            private static GUIStyle? _rightMini;
            static bool _ready;

            static void Ensure()
            {
                if (_ready) return;
                _rightMini = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    richText = true,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _ready = true;
            }

            /// <summary>
            /// 관찰 컴포넌트가 엔티티에 존재할 때의 체크 표시(✔).
            /// </summary>
            public static void DrawOK(float width = 20f)
            {
                Ensure();
                // 녹색 체크 한 글자
                GUILayout.Label(
                    new GUIContent("<b><color=#2ECC71>✔</color></b>"),
                    _rightMini,
                    GUILayout.Width(width)
                );
            }
        }

        static class NotAssignLabel
        {
            private static GUIStyle? _rightMini;
            static bool _ready;

            static void Ensure()
            {
                if (_ready) return;
                _rightMini = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    richText = true,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _ready = true;
            }

            /// <summary>
            /// 관찰 컴포넌트가 엔티티에 없을 때의 X 표시(✕).
            /// </summary>
            public static void Draw(float width = 20f)
            {
                Ensure();
                // 빨간 X 한 글자
                GUILayout.Label(
                    new GUIContent("<b><color=#E74C3C>✕</color></b>"),
                    _rightMini,
                    GUILayout.Width(width)
                );
            }
        }

        static class ItalicLabel
        {
            private static GUIStyle? _leftMiniItalic;
            static bool _ready;

            static void Ensure()
            {
                if (_ready) return;
                _leftMiniItalic = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleLeft,
                    richText = true,
                    padding = new RectOffset(16, 0, 0, 0),
                    wordWrap = false,
                };
                _ready = true;
            }

            public static void DrawLeft(string text, params GUILayoutOption[] opts)
            {
                Ensure();
                GUILayout.Label($"<color=#686868>{text}</color>", _leftMiniItalic, opts);
            }
        }

        enum StatusKind
        {
            Present,
            Absent,
            Available,
            Missing
        }

        static class StatusLabelGUI
        {
            private static GUIStyle? _pill;
            private static GUIStyle? _pillRight;
            static bool _ready;

            static void Ensure()
            {
                if (_ready) return;
                _ready = true;

                // 기본 미니 라벨 스타일 복제 + 리치텍스트 활성화
                _pill = new GUIStyle(EditorStyles.miniLabel)
                {
                    richText = true,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(6, 6, 2, 2)
                };
                _pillRight = new GUIStyle(_pill) { alignment = TextAnchor.MiddleRight };
            }

            static string ColorFor(StatusKind k) => k switch
            {
                StatusKind.Present => "#27ae60", // green
                StatusKind.Available => "#27ae60",
                StatusKind.Absent => "#bdc3c7",  // gray
                StatusKind.Missing => "#e74c3c", // red
                _ => "#ffffff"
            };

            static string TextFor(StatusKind k) => k switch
            {
                StatusKind.Present => "PRESENT",
                StatusKind.Absent => "ABSENT",
                StatusKind.Available => "AVAILABLE",
                StatusKind.Missing => "MISSING",
                _ => "-"
            };

            public static void Draw(StatusKind kind, float width = 86f, bool alignRight = true)
            {
                Ensure();
                var style = alignRight ? _pillRight : _pill;
                var hex = ColorFor(kind);
                var txt = TextFor(kind);
                var content = new GUIContent($"<b><color={hex}>{txt}</color></b>");
                GUILayout.Label(content, style, GUILayout.Width(width));
            }
        }

        void DrawBinderMeta(IWorld world, Entity e, object? binderObj)
        {
            if (binderObj == null) return;

            var t = binderObj.GetType();
            var binder = binderObj as IBinder;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // --- Priority 편집 영역 ---
                if (binder != null)
                {
                    // 현재 Priority 값 가져오기 (우선 리플렉션, 실패하면 0)
                    int currentPriority = 0;
                    try
                    {
                        var piPrio = t.GetProperty(
                            "ApplyOrder",
                            BindingFlags.Public | BindingFlags.Instance
                        );
                        if (piPrio != null &&
                            piPrio.PropertyType == typeof(int) &&
                            piPrio.GetIndexParameters().Length == 0)
                        {
                            currentPriority = (int)(piPrio.GetValue(binder) ?? 0);
                        }
                    }
                    catch
                    {
                        // 무시하고 0 사용
                    }

                    using (new EditorGUI.DisabledScope(!_editMode))
                    {
                        // 한 줄짜리 Rect를 직접 받아서 x 좌표를 제어
                        var lineRect = EditorGUILayout.GetControlRect();

                        int newPriority = currentPriority;
                        bool changedByButton = false;

                        // Observing (IBinds) 가 사용하는 boldLabel의 왼쪽 마진만큼 안쪽에서 시작
                        float startX = lineRect.x + EditorStyles.boldLabel.margin.left + 12;
                        float h = lineRect.height;
                        const float btnW = 22f;
                        const float btnGap = 2f;
                        const float labelGap = 1f;
                        const float labelW = 90f;

                        // [-] / [+] / Label / IntField의 Rect 계산
                        var minusRect = new Rect(startX, lineRect.y, btnW, h);
                        var plusRect = new Rect(minusRect.xMax + btnGap, lineRect.y, btnW, h);
                        var labelRect = new Rect(plusRect.xMax + labelGap - 8, lineRect.y, labelW, h);
                        var fieldRect = new Rect(labelRect.xMax + labelGap, lineRect.y,
                            lineRect.xMax - (labelRect.xMax + labelGap), h);

                        // [-] 버튼
                        if (GUI.Button(minusRect, "-"))
                        {
                            newPriority = currentPriority - 1;
                            changedByButton = true;
                        }

                        // [+] 버튼
                        if (GUI.Button(plusRect, "+"))
                        {
                            newPriority = currentPriority + 1;
                            changedByButton = true;
                        }

                        // "Apply Order" 라벨
                        EditorGUI.LabelField(labelRect, "Apply Order");

                        // 숫자 직접 입력
                        EditorGUI.BeginChangeCheck();
                        newPriority = EditorGUI.IntField(fieldRect, GUIContent.none, newPriority);
                        bool changedByField = EditorGUI.EndChangeCheck();

                        if ((changedByButton || changedByField) && newPriority != currentPriority)
                        {
                            try
                            {
                                binder.SetApplyOrder(newPriority);
                            }
                            catch (MissingMethodException)
                            {
                                Debug.LogWarning(
                                    $"IBinder.SetApplyOrder(int) 가 구현되지 않았습니다: {t.FullName}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }

                            Repaint();
                        }
                    }

                    GUILayout.Space(4);
                }

                // --- Observing (IBinds) 메타 정보는 그대로 유지 ---
                EditorGUILayout.LabelField("Observing (IBinds)", EditorStyles.boldLabel);

                var observed = BinderIntrospection.ExtractObservedComponentTypes(t);

                if (observed.Count == 0)
                {
                    EditorGUILayout.LabelField("— (no IBind<> found)");
                }
                else
                {
                    foreach (var ct in observed)
                    {
                        var has = world.HasComponentBoxed(e, ct);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (has)
                                EditorGUILayout.LabelField(ct.Name, GUILayout.MinWidth(80));
                            else
                                ItalicLabel.DrawLeft(ct.Name, GUILayout.MinWidth(80));

                            GUILayout.FlexibleSpace();

                            if (has)
                            {
                                OkLabel.DrawOK(20f);
                            }
                            else
                            {
                                NotAssignLabel.Draw(20);
                            }
                        }
                    }
                }
            }
        }

        static class BinderTypeFinder
        {
            private static List<Type>? _cache;

            public static IEnumerable<Type> All()
            {
                if (_cache != null) return _cache;

                var list = new List<Type>(256);
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var asm in asms)
                {
                    // 편의상 에디터/시스템 어셈블리는 스킵 (원하면 조건 완화 가능)
                    var n = asm.GetName().Name;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var t in types)
                    {
                        if (t == null || t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        // 생성자 조건: 기본 생성자
                        if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                        // “Binder” 후보 판별 (마커 인터페이스/특성/이름 규칙 중 하나라도 맞으면 통과)
                        if (LooksLikeBinder(t))
                            list.Add(t);
                    }
                }

                // 정렬 및 캐시
                _cache = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();
                return _cache;
            }

            static bool LooksLikeBinder(Type t) => typeof(IBinder).IsAssignableFrom(t);
            // {
            //     // 1) 인터페이스 이름에 Binder 포함 (e.g., IBinder, IEntityBinder 등)
            //     if (t.GetInterfaces().Any(i => i.Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0))
            //         return true;
            //
            //     // 2) 특성 이름에 Binder 포함 (e.g., [ZenBinder], [Binder] 등)
            //     if (t.GetCustomAttributes(inherit: true).Any(a => a.GetType().Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0))
            //         return true;
            //
            //     // 3) 타입명 규칙상 Binder 접미사
            //     if (t.Name.EndsWith("Binder", StringComparison.Ordinal))
            //         return true;
            //
            //     return false;
            // }
        }

        static bool CanShowBinderBody(Type t, object boxed)
        {
            // 1) 에디터 폼으로 그릴 수 있는 필드가 있거나
            if (ZenComponentFormGUI.HasDrawableFields(t))
                return true;

            // 2) 메타(IBind)라도 있으면 바디를 보여줄 가치가 있음
            var hasObserved = BinderIntrospection.ExtractObservedComponentTypes(t).Count > 0;
            if (hasObserved) return true;

            return false;
        }

        void DrawBindersList(IWorld world, Entity e, (Type type, object? boxed)[] bindersArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in bindersArray)
                {
                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                    if (!_binderFold.ContainsKey(ck)) _binderFold[ck] = false;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    bool hasMetaOrFields = boxed != null && CanShowBinderBody(t, boxed);

                    // IBinder + Disabled 판정
                    var binder = boxed as IBinder;
                    bool isDisabled = binder != null && !binder.Enabled;

                    string binderTitle = t.Name;
                    if (isDisabled)
                        binderTitle += " [Disabled]";

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var headRectB = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
                        bool openB = _binderFold[ck];

                        // 헤더 이름 색상: Disabled면 짙은 회색
                        var prevHeaderColor = GUI.color;
                        if (isDisabled)
                        {
                            // 짙은 회색 (0=검정,1=흰색 기준)
                            GUI.color = new Color(0.45f, 0.45f, 0.45f);
                        }

                        ZenFoldoutHeader.DrawRow(
                            ref openB,
                            headRectB,
                            binderTitle,
                            nameSpace: t.Namespace,
                            rRight =>
                            {
                                var style = EditorStyles.miniButton;

                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                // 오른쪽 끝: 삭제 버튼
                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // binder 활성 상태
                                var hasBinder = binder != null;
                                bool isEnabled = hasBinder && binder is { Enabled: true };
                                bool canToggle = hasBinder && _editMode; // 읽기전용일 땐 토글 비활성

                                var icon = GetSearchIconContent("Ping script asset");
                                if (GUI.Button(rR2, icon, EditorStyles.iconButton))
                                {
                                    PingContextType(t);
                                }

                                // 🔹 Enabled Pause 스타일 (System / Global Pause와 동일)
                                using (new EditorGUI.DisabledScope(!canToggle))
                                {
                                    // CKWORK

                                    // Unity 기본 Pause 아이콘
                                    var pauseContent = EditorGUIUtility.IconContent("PauseButton");
                                    if (pauseContent == null || pauseContent.image == null)
                                        pauseContent = EditorGUIUtility.TrTextContent("⏸");

                                    var pauseStyle = new GUIStyle("Button")
                                    {
                                        alignment = TextAnchor.MiddleCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    var oldBg = GUI.backgroundColor;
                                    var oldCont = GUI.contentColor;

                                    if (hasBinder && !isEnabled)
                                    {
                                        // Unity 툴바 Pause랑 비슷한 파란색
                                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                                            ? new Color(0.24f, 0.48f, 0.90f, 1f)  // Dark Skin
                                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                                        GUI.contentColor = Color.white;
                                    }

                                    if (GUI.Button(rR1, pauseContent, pauseStyle) && canToggle)
                                    {
                                        isEnabled = !isEnabled;
                                        if (binder != null)
                                        {
                                            binder.Enabled = isEnabled;
                                        }
                                    }

                                    GUI.backgroundColor = oldBg;
                                    GUI.contentColor = oldCont;
                                }

                                // 🔸 삭제 버튼 (기존 그대로)
                                using (new EditorGUI.DisabledScope(!_editMode || !BinderApi.CanRemove(world)))
                                {
                                    var gcDel = new GUIContent("X", "Remove this Binder from Entity");
                                    if (GUI.Button(rR0, gcDel, style))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Binder",
                                                $"Remove this binder?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                "Yes", "No"))
                                        {
                                            BinderApi.Remove(world, e, t);
                                            _binderFold.Remove(ck);
                                            Repaint();
                                        }
                                    }
                                }
                            },
                            foldable: hasMetaOrFields,
                            false
                        );

                        GUI.color = prevHeaderColor;
                        _binderFold[ck] = openB;

                        if (!_binderFold[ck]) continue;

                        // ===== body =====
                        var prevBodyColor = GUI.color;
                        if (isDisabled)
                        {
                            // 바디도 같은 짙은 회색 톤
                            GUI.color = new Color(0.45f, 0.45f, 0.45f);
                        }

                        // 메타 (IBind 관찰 컴포넌트 등)
                        DrawBinderMeta(world, e, boxed);

                        try
                        {
                            if (hasFields)
                            {
                                object obj = CopyBox(boxed, t);
                                float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                                bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);
                                var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
                                var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);

                                EditorGUI.BeginChangeCheck();
                                ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                                if (EditorGUI.EndChangeCheck() && _editMode && BinderApi.CanReplace(world))
                                    BinderApi.Replace(world, e, obj);
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            // ignore
                        }

                        GUI.color = prevBodyColor;
                    }
                }
            }
        }


        void DrawComponentsList(IWorld world, Entity e, bool isSingleton, (Type type, object? boxed)[] compsArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in compsArray)
                {
                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                    if (!_componentFold.ContainsKey(ck)) _componentFold[ck] = false;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // ===== Component header =====
                        var headRectC = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
                        bool openC = _componentFold[ck];

                        ZenFoldoutHeader.DrawRow(
                            ref openC,
                            headRectC,
                            t.Name,
                            t.Namespace,
                            rRight =>
                            {
                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                using (new EditorGUI.DisabledScope(!_editMode))
                                {
                                    if (!isSingleton)
                                    {
                                        if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                        {
                                            if (EditorUtility.DisplayDialog(
                                                    "Remove Component",
                                                    $"Remove this component?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                    "Yes", "No"))
                                            {
                                                ZenEcsEditor.CommandQueue.Enqueue(EditorCommand.RemoveComponent(e, t));
                                                
                                                _componentFold.Remove(ck);
                                                Repaint();
                                            }
                                        }

                                        using (new EditorGUI.DisabledScope(!hasFields))
                                        {
                                            if (GUI.Button(rR1, "R", EditorStyles.miniButton))
                                            {
                                                // if (EditorUtility.DisplayDialog(
                                                //         "Reset Component",
                                                //         $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                //         "Yes", "No"))
                                                {
                                                    var def = ZenDefaults.CreateWithDefaults(t);
                                                    ZenEcsEditor.CommandQueue.Enqueue(
                                                        EditorCommand.ReplaceComponent(e, t, def));
                                                    
                                                    Repaint();
                                                }
                                            }
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR2, icon, EditorStyles.iconButton))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                    else
                                    {
                                        using (new EditorGUI.DisabledScope(!hasFields))
                                        {
                                            if (GUI.Button(rR0, "R", EditorStyles.miniButton))
                                            {
                                                // if (EditorUtility.DisplayDialog(
                                                //         "Reset Component",
                                                //         $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                //         "Yes", "No"))
                                                {
                                                    var def = ZenDefaults.CreateWithDefaults(t);
                                                    ZenEcsEditor.CommandQueue.Enqueue(
                                                        EditorCommand.ReplaceComponent(e, t, def));
                                                    Repaint();
                                                }
                                            }
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR1, icon, EditorStyles.iconButton))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                }
                            },
                            foldable: hasFields,
                            false
                        );

                        _componentFold[ck] = hasFields && openC;

                        // ===== body =====
                        if (!hasFields || !_componentFold[ck]) continue;

                        try
                        {
                            object obj = CopyBox(boxed, t);
                            float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                            bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                            var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
                            var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);

                            EditorGUI.BeginChangeCheck();
                            ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                            if (EditorGUI.EndChangeCheck() && _editMode)
                            {
                                ZenEcsEditor.CommandQueue.Enqueue(EditorCommand.ReplaceComponent(e, t, obj));
                            }
                        }
                        catch (KeyNotFoundException) { }
                    }
                }
            }
        }

        static int CountLines(string s)
        {
            if (string.IsNullOrEmpty(s)) return 1;
            int c = 1;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '\n')
                    c++;
            return c;
        }

        static class BinderApi
        {
            // ▶ 전부 nullable 로
            static MethodInfo? _miGetAllBinders;
            static MethodInfo? _miAddBinderBoxed;
            static MethodInfo? _miRemoveBinderBoxed;
            static MethodInfo? _miReplaceBinderBoxed;

            // key → MethodInfo 캐시 (null은 캐시하지 않음)
            static readonly Dictionary<(Type owner, string name, int argc), MethodInfo> _miCache =
                new();

            static MethodInfo? FindAndCache(IWorld w, string name, int paramCount, Func<MethodInfo, bool>? pred = null)
            {
                var key = (w.GetType(), name, paramCount);
                if (_miCache.TryGetValue(key, out var hit)) return hit;

                var found = w.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == name
                                         && m.GetParameters().Length == paramCount
                                         && (pred is null || pred(m)));

                if (found is not null)
                    _miCache[key] = found;

                return found;
            }

            public static bool TryGetAll(IWorld w, Entity e, out (Type type, object? boxed)[] binders)
            {
                binders = Array.Empty<(Type, object?)>();

                // ✅ nullable 필드에 안전한 ??= 사용
                var mi = _miGetAllBinders ??= FindAndCache(w, "GetAllBinders", 1, m =>
                {
                    var prms = m.GetParameters();
                    return prms.Length == 1 && prms[0].ParameterType == typeof(Entity);
                });

                if (mi is not null)
                {
                    var result = mi.Invoke(w, new object[] { e });
                    if (result is Array arr && arr.Length > 0)
                    {
                        var list = new List<(Type, object?)>(arr.Length);
                        foreach (var item in arr)
                        {
                            var fields = item.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                            if (fields.Length >= 2 && fields[0].FieldType == typeof(Type))
                            {
                                var t = (Type)fields[0].GetValue(item)!;
                                var o = fields[1].GetValue(item);
                                list.Add((t, o));
                            }
                        }

                        binders = list.ToArray();
                        return true;
                    }
                }

                // 대안 시그니처: IEnumerable 반환
                var miEnum = FindAndCache(w, "GetAllBinders", 1, m =>
                {
                    var prms = m.GetParameters();
                    return prms.Length == 1
                           && prms[0].ParameterType == typeof(Entity)
                           && typeof(System.Collections.IEnumerable).IsAssignableFrom(m.ReturnType);
                });

                if (miEnum is not null)
                {
                    if (miEnum.Invoke(w, new object[] { e }) is System.Collections.IEnumerable en)
                    {
                        var list = new List<(Type, object?)>();
                        foreach (var b in en)
                        {
                            if (b is null) continue;
                            list.Add((b.GetType(), b));
                        }

                        binders = list.ToArray();
                        return true;
                    }
                }

                return false;
            }

            public static bool CanAdd(IWorld w)
            {
                return true;
                // _miAddBinderBoxed ??= FindAndCache(w, "AddBinderBoxed", 2, m =>
                // {
                //     var p = m.GetParameters();
                //     return p.Length == 2 && p[0].ParameterType == typeof(Entity);
                // });
                // return _miAddBinderBoxed is not null;
            }

            public static void Add(IWorld w, Entity e, object binder)
            {
                var b = (IBinder)binder;
                w.AttachBinder(e, b);

                if (_miAddBinderBoxed is null) return;
                _miAddBinderBoxed.Invoke(w, new object[] { e, binder });
            }

            public static bool CanRemove(IWorld w)
            {
                return true;
                // _miRemoveBinderBoxed ??= FindAndCache(w, "RemoveBinderBoxed", 2, m =>
                // {
                //     var p = m.GetParameters();
                //     return p.Length == 2
                //            && p[0].ParameterType == typeof(Entity)
                //            && (p[1].ParameterType == typeof(Type) || p[1].ParameterType == typeof(object));
                // });
                // return _miRemoveBinderBoxed is not null;
            }

            public static void Remove(IWorld w, Entity e, Type binderType)
            {
                w.DetachBinder(e, binderType);
            }

            public static bool CanReplace(IWorld w)
            {
                _miReplaceBinderBoxed ??= FindAndCache(w, "ReplaceBinderBoxed", 2, m =>
                {
                    var p = m.GetParameters();
                    return p.Length == 2 && p[0].ParameterType == typeof(Entity);
                });
                return _miReplaceBinderBoxed is not null;
            }

            public static void Replace(IWorld w, Entity e, object binder)
            {
                if (_miReplaceBinderBoxed is null) return;
                _miReplaceBinderBoxed.Invoke(w, new object[] { e, binder });
            }
        }

        // ===== Safe new & shallow copy =====
        static class SafeNew
        {
            public static object New(Type t)
            {
                if (t.IsValueType) return Activator.CreateInstance(t);
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null) return Activator.CreateInstance(t);
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
            }
        }

        static object CopyBox(object? src, Type t)
        {
            if (src == null) return SafeNew.New(t);
            if (t.IsValueType) return src;
            var dst = SafeNew.New(t);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(dst, f.GetValue(src));
            return dst;
        }

        // Expand/collapse helpers
        void SetAllComponentsFold(IWorld world, Entity e, bool open)
        {
            var comps = world.GetAllComponents(e);
            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                _componentFold[key] = open;
            }
        }

        bool AreAllComponentsOpen_VisibleOnly(Entity e, (Type type, object? boxed)[] comps)
        {
            bool any = false;
            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
                any = true;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                if (!_componentFold.TryGetValue(key, out bool open) || !open)
                    return false;
            }

            return any && true;
        }

        // external call (ex: EcsExplorerBridge)
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

        static class ContextApi
        {
            static MethodInfo? _miGetAllContexts; // (Entity) -> (Type, object)[] 또는 IEnumerable
            static MethodInfo? _miAddFromAsset;   // (Entity, ContextAsset) -> void
            static MethodInfo? _miAttachContext;  // (Entity, IContext) -> void
            static MethodInfo? _miRemoveContext;  // (Entity, Type) -> void

            static readonly Dictionary<(Type, string, int), MethodInfo> _cache = new();

            static MethodInfo? Find(IWorld w, string name, int argc, Func<MethodInfo, bool>? pred = null)
            {
                var key = (w.GetType(), name, argc);
                if (_cache.TryGetValue(key, out var hit)) return hit;

                var mi = w.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == name
                                         && m.GetParameters().Length == argc
                                         && (pred is null || pred(m)));
                if (mi != null) _cache[key] = mi;
                return mi;
            }

            public static bool TryGetAll(IWorld w, Entity e, out (Type type, object? boxed)[] contexts)
            {
                contexts = Array.Empty<(Type, object?)>();

                _miGetAllContexts ??= Find(w, "GetAllContexts", 1, m =>
                {
                    var ps = m.GetParameters();
                    return ps[0].ParameterType == typeof(Entity);
                });

                if (_miGetAllContexts != null)
                {
                    var ret = _miGetAllContexts.Invoke(w, new object[] { e });
                    if (ret is Array arr)
                    {
                        var list = new List<(Type, object)>(arr.Length);
                        foreach (var item in arr)
                        {
                            var fs = item.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                            if (fs.Length >= 2 && fs[0].FieldType == typeof(Type))
                            {
                                list.Add(((Type)fs[0].GetValue(item), fs[1].GetValue(item)));
                            }
                        }

                        contexts = list.ToArray()!;
                        return true;
                    }

                    if (ret is System.Collections.IEnumerable en)
                    {
                        var list = new List<(Type, object)>();
                        foreach (var c in en)
                        {
                            if (c == null) continue;
                            list.Add((c.GetType(), c));
                        }

                        contexts = list.ToArray()!;
                        return true;
                    }
                }

                // 마지막 대안: 없는 경우 실패
                return false;
            }

            public static bool CanAdd(IWorld w)
                => Find(w, "AddContextFromAsset", 2, m =>
                       m.GetParameters()[0].ParameterType == typeof(Entity) &&
                       typeof(ContextAsset).IsAssignableFrom(m.GetParameters()[1].ParameterType)) != null
                   || Find(w, "AttachContext", 2, m =>
                       m.GetParameters()[0].ParameterType == typeof(Entity) &&
                       typeof(ZenECS.Core.Binding.IContext).IsAssignableFrom(m.GetParameters()[1].ParameterType)) !=
                   null;

            public static void AddFromAsset(IWorld w, Entity e, ContextAsset asset)
            {
                switch (asset)
                {
                    case SharedContextAsset markerAsset:
                    {
                        var resolver = ZenEcsUnityBridge.SharedContextResolver;
                        if (resolver != null)
                        {
                            var ctx = resolver.Resolve(markerAsset);
                            w.RegisterContext(e, ctx);
                        }

                        break;
                    }
                    case PerEntityContextAsset perEntityAsset:
                    {
                        var ctx = perEntityAsset.Create();
                        w.RegisterContext(e, ctx);
                        break;
                    }
                }


                // // 1) World가 직접 (Entity, ContextAsset) 받는 경우
                // _miAddFromAsset ??= Find(w, "AddContextFromAsset", 2, m =>
                //     m.GetParameters()[0].ParameterType == typeof(Entity) &&
                //     typeof(ContextAsset).IsAssignableFrom(m.GetParameters()[1].ParameterType));
                //
                // if (_miAddFromAsset != null)
                // {
                //     _miAddFromAsset.Invoke(w, new object[] { e, asset });
                //     return;
                // }
                //
                // // 2) Asset → IContext 인스턴스로 만들어 AttachContext(Entity, IContext)
                // _miAttachContext ??= Find(w, "AttachContext", 2, m =>
                //     m.GetParameters()[0].ParameterType == typeof(Entity) &&
                //     typeof(ZenECS.Core.Binding.IContext).IsAssignableFrom(m.GetParameters()[1].ParameterType));
                //
                // if (_miAttachContext == null)
                //     throw new MissingMethodException("World.AttachContext(Entity, IContext) not found.");
                //
                // // Asset에서 인스턴스 만드는 규약 탐색
                // object? ctx = TryCreateContextInstance(asset, w, e);
                // if (ctx == null)
                //     throw new MissingMethodException("ContextAsset에서 컨텍스트 인스턴스를 만들 수 있는 팩토리를 찾지 못했습니다.");
                //
                // _miAttachContext.Invoke(w, new object[] { e, ctx });
            }

            static object? TryCreateContextInstance(ContextAsset asset, IWorld w, Entity e)
            {
                var aType = asset.GetType();
                // 우선순위: Create(IWorld,Entity) -> Create(IWorld) -> Create() -> Build/Instantiate() 변형
                var names = new[] { "Create", "Build", "Instantiate", "Make", "ToInstance" };
                foreach (var name in names)
                {
                    var mi = aType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(IWorld), typeof(Entity) }, null);
                    if (mi != null) return mi.Invoke(asset, new object[] { w, e });

                    mi = aType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(IWorld) }, null);
                    if (mi != null) return mi.Invoke(asset, new object[] { w });

                    mi = aType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);
                    if (mi != null) return mi.Invoke(asset, Array.Empty<object>());
                }

                return null;
            }

            public static bool CanRemove(IWorld w)
                => Find(w, "RemoveContext", 2, m =>
                    m.GetParameters()[0].ParameterType == typeof(Entity) &&
                    m.GetParameters()[1].ParameterType == typeof(Type)) != null;

            public static void Remove(IWorld w, Entity e, IContext? ctx)
            {
                if (ctx == null) return;
                w.RemoveContext(e, ctx);
            }
        }

        sealed class ContextAssetPickerWindow : EditorWindow
        {
            public static void Show(
                Rect activatorRectGui,
                Action<ContextAsset> onPick,
                IReadOnlyCollection<Type>? disabledContextTypes,
                string title = "Add Context")
            {
                var w = CreateInstance<ContextAssetPickerWindow>();
                w._title = title;
                w._onPick = onPick;
                w._all = LoadAllAssets();
                w._disabledSet = disabledContextTypes != null
                    ? new HashSet<Type>(disabledContextTypes)
                    : new HashSet<Type>();

                var screen = GUIUtility.GUIToScreenRect(activatorRectGui);
                var size = new Vector2(520, 400);
                w.position = new Rect(screen.x, screen.yMax, size.x, size.y);
                w.ShowAsDropDown(screen, size);
                w.Focus();
            }

            string _title = "Add Context";
            private Action<ContextAsset>? _onPick;
            List<ContextAsset> _all = new();
            string _search = "";
            Vector2 _scroll;
            int _hover = -1;

            HashSet<Type> _disabledSet = new(); // 이미 붙어있는 컨텍스트 타입들

            static List<ContextAsset> LoadAllAssets()
            {
                var res = new List<ContextAsset>(64);
                foreach (var guid in AssetDatabase.FindAssets("t:ContextAsset"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var a = AssetDatabase.LoadAssetAtPath<ContextAsset>(path);
                    if (a) res.Add(a);
                }

                return res.OrderBy(a => a.name).ToList();
            }

            void OnGUI()
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                }

                var list = string.IsNullOrWhiteSpace(_search)
                    ? _all
                    : _all.Where(a => a.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = sv.scrollPosition;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var a = list[i];
                        var r = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));

                        if (r.Contains(Event.current.mousePosition)) _hover = i;
                        if (i == _hover) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.15f));

                        // 이 SO가 만들어줄 컨텍스트 타입 추론
                        var ctxType = TryResolveContextType(a);
                        bool disabled = ctxType != null &&
                                        _disabledSet.Any(t =>
                                            t == ctxType ||
                                            t.IsSubclassOf(ctxType) ||
                                            ctxType.IsSubclassOf(t));

                        var style = new GUIStyle(EditorStyles.label) { richText = true };
                        string path = AssetDatabase.GetAssetPath(a);

                        string label;
                        if (disabled)
                        {
                            // 이미 붙어있는 타입은 회색 + 안내
                            label =
                                $"<color=#777777>{a.name} </color>" +
                                $"<size=10><color=#555>[{path}]</color></size>";
                        }
                        else
                        {
                            label =
                                $"{a.name} <size=10><color=#888>[{path}]</color></size>";
                        }

                        using (new EditorGUI.DisabledScope(disabled))
                        {
                            EditorGUI.LabelField(r, label, style);

                            if (!disabled &&
                                Event.current.type == EventType.MouseDown &&
                                r.Contains(Event.current.mousePosition))
                            {
                                _onPick?.Invoke(a);
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    if (list.Count == 0)
                    {
                        GUILayout.FlexibleSpace();
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("No ContextAsset found", EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
            }

            void OnLostFocus() => Close();

            // ContextAsset이 만들어내는 IContext 타입 추론
            static Type? TryResolveContextType(ContextAsset asset)
            {
                if (asset == null) return null;
                var aType = asset.GetType();

                // 1) ContextType 프로퍼티 관례
                var prop = aType.GetProperty("ContextType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (prop != null && typeof(Type).IsAssignableFrom(prop.PropertyType))
                {
                    var v = prop.GetValue(prop.GetGetMethod(true)?.IsStatic == true ? null : asset) as Type;
                    if (v != null && typeof(IContext).IsAssignableFrom(v))
                        return v;
                }

                // 2) GetContextType() 메서드 관례
                var mGet = aType.GetMethod("GetContextType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                    null, Type.EmptyTypes, null);
                if (mGet != null && typeof(Type).IsAssignableFrom(mGet.ReturnType))
                {
                    var v = mGet.Invoke(mGet.IsStatic ? null : asset, Array.Empty<object>()) as Type;
                    if (v != null && typeof(IContext).IsAssignableFrom(v))
                        return v;
                }

                // 3) Create/Build/Instantiate/Make/ToInstance 반환 타입으로 추론
                var names = new[] { "Create", "Build", "Instantiate", "Make", "ToInstance" };
                foreach (var name in names)
                {
                    var methods = aType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(mi => mi.Name == name);
                    foreach (var mi in methods)
                    {
                        var rt = mi.ReturnType;
                        if (typeof(IContext).IsAssignableFrom(rt))
                            return rt;
                    }
                }

                return null;
            }
        }

        static bool HasVisibleContextMembers(Type ctxType)
        {
            if (ctxType == null) return false;

            // 🔹 이 타입에 "직접 선언된" public 필드만
            foreach (var f in ctxType.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (Attribute.IsDefined(f, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(f, typeof(HideInInspector), inherit: true)) continue;
                return true;
            }

            // 🔹 이 타입에 "직접 선언된" public 프로퍼티만
            foreach (var p in ctxType.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length != 0) continue;
                if (Attribute.IsDefined(p, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(p, typeof(HideInInspector), inherit: true)) continue;
                return true;
            }

            return false;
        }

        void DrawContextsList(IWorld world, Entity e, (Type type, object? boxed)[] ctxs)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, inst) in ctxs)
                {
                    if (t == null) continue;

                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:CTX";

                    if (!_contextFold.TryGetValue(ck, out var open))
                    {
                        open = false;
                        _contextFold[ck] = open;
                    }

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var headRect = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));

                        // 헤더: ZenFoldoutHeader 사용 (컴포넌트/바인더와 동일 폰트 + 네임스페이스)
                        ZenFoldoutHeader.DrawRow(
                            ref open,
                            headRect,
                            t.Name,
                            t.Namespace,
                            rRight =>
                            {
                                var style = EditorStyles.miniButton;

                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                // 오른쪽 끝: 삭제 버튼
                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // CKWORK
                                using (new EditorGUI.DisabledScope(!_editMode))
                                {
                                    if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Context",
                                                $"Remove this context?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                "Yes", "No"))
                                        {
                                            if (inst is IContext ctxInstance)
                                            {
                                                ContextApi.Remove(world, e, ctxInstance);
                                                _contextFold.Remove(ck);
                                            }

                                            Repaint();
                                        }
                                    }

                                    var pingStyle = new GUIStyle(EditorStyles.iconButton)
                                    {
                                        alignment = TextAnchor.LowerCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    var pauseStyle = new GUIStyle("Button")
                                    {
                                        alignment = TextAnchor.MiddleCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    if (inst is IContextReinitialize)
                                    {
                                        if (GUI.Button(rR1, "R", pauseStyle))
                                        {
                                            var s = $"Reinitialize {t.Name}";
                                            if (world.ReinitializeContext(e, (IContext)inst))
                                            {
                                                s += " success";
                                            }
                                            else
                                            {
                                                s += " failed";
                                            }

                                            Debug.Log(s);
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR2, icon, pingStyle))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                    else
                                    {
                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR1, icon, pingStyle))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                }
                            },
                            foldable: true,
                            noMarginTitle: false // ← 화살표 + 2줄(이름/네임스페이스) 레이아웃
                        );

                        _contextFold[ck] = open;

                        // 닫혀 있으면 필드 목록은 안 그림 (하지만 이름/네임스페이스는 항상 출력됨)
                        if (!open || inst == null)
                            continue;

                        // 실제 컨텍스트 필드/프로퍼티 읽기 전용 렌더링
                        DrawContextFieldsReadonly(inst, t);
                    }
                }
            }
        }

        void DrawContextFieldsReadonly(object ctxInstance, Type ctxType)
        {
            if (ctxInstance == null || ctxType == null)
                return;

            // 공용 인스턴스 멤버 수집: Field + Property(get 가능, 인덱서 제외)
            var members = new List<(string name, Type type, Func<object?> getter)>();

            // 1) 이 타입에 "직접 선언된" public 필드
            foreach (var f in ctxType.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (Attribute.IsDefined(f, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(f, typeof(HideInInspector), inherit: true)) continue;

                var localField = f;
                members.Add((
                    localField.Name,
                    localField.FieldType,
                    () => localField.GetValue(ctxInstance)
                ));
            }

            // 2) 이 타입에 "직접 선언된" public 프로퍼티 (getter만, 인덱서 제외)
            foreach (var p in ctxType.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length != 0) continue;
                if (Attribute.IsDefined(p, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(p, typeof(HideInInspector), inherit: true)) continue;

                var localProp = p;
                members.Add((
                    localProp.Name,
                    localProp.PropertyType,
                    () => localProp.GetValue(ctxInstance, null)
                ));
            }

            if (members.Count == 0)
                return;

            // 이름 순으로 정렬
            members.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (name, mType, getter) in members)
                {
                    object? value;
                    try
                    {
                        value = getter();
                    }
                    catch
                    {
                        value = null;
                    }

                    // UnityEngine.Object 계열이면 Ping 지원
                    if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                    {
                        var obj = value as UnityEngine.Object;
                        var rect = EditorGUILayout.GetControlRect();

                        // 라벨/값 영역 분리
                        var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                        var valRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                            rect.width - EditorGUIUtility.labelWidth, rect.height);

                        // 이름
                        EditorGUI.LabelField(labelRect, name);

                        // Object 아이콘 + 이름을 레이블로만 표시 (ObjectField 사용 안 함 → 피커 안 뜸)
                        GUIContent content;
                        if (obj != null)
                            content = EditorGUIUtility.ObjectContent(obj, mType);
                        else
                            content = new GUIContent("None", EditorGUIUtility.IconContent("Prefab Icon").image);

                        EditorGUI.LabelField(valRect, content);

                        // 라벨/값을 클릭하면 Ping만 수행
                        if (obj != null &&
                            Event.current.type == EventType.MouseDown &&
                            (labelRect.Contains(Event.current.mousePosition) ||
                             valRect.Contains(Event.current.mousePosition)))
                        {
                            EditorGUIUtility.PingObject(obj);
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        // 일반 값 타입/문자열 등은 그냥 읽기 전용 텍스트
                        string text;
                        if (value == null) text = "null";
                        else if (mType == typeof(float)) text = ((float)value).ToString("0.###");
                        else if (mType == typeof(double)) text = ((double)value).ToString("0.###");
                        else text = value.ToString() ?? "null";

                        EditorGUILayout.LabelField(name, text);
                    }
                }
            }
        }

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
            var watchedCount = ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCountByWatch(sys, world);
            if (watchedCount > 0)
                typeName = $"{typeName} ({watchedCount})";

            GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.button);
            centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            centeredLabelStyle.fontStyle = FontStyle.Normal;
            centeredLabelStyle.fontSize = 10;

            bool selected = _selSystem == index;
            bool clicked = GUI.Toggle(sysRect, selected, typeName, centeredLabelStyle);
            if (clicked && !selected)
            {
                _selSystem = index;
                _hasSelectedSingleton = false;
                _selSysEntityCount = 0;
                _entityFold.Clear();
                _binderFold.Clear();
                _componentFold.Clear();
                _contextFold.Clear();
                _watchedFold.Clear();
                _cache.Clear();
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
            using (new EditorGUI.DisabledScope(!_editMode))
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

                        _selSystem = -1;
                        _hasSelectedSingleton = false;
                        _selSysEntityCount = 0;
                        _cache.Clear();
                        _entityFold.Clear();
                        _binderFold.Clear();
                        _componentFold.Clear();
                        _contextFold.Clear();
                        _watchedFold.Clear();

                        Repaint();
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

            if (!_groupFold.TryGetValue(group, out var openGroup))
                openGroup = true;

            openGroup = EditorGUILayout.Foldout(openGroup, label, true, systemTreeToggleStyle);
            _groupFold[group] = openGroup;
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

        private Color systemMetaTextColor = Color.lightGray;
        private Color systemTreeTextColor = Color.lightGray;

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
            if (!_phaseFold.TryGetValue(key, out var openPhase))
                openPhase = true;

            openPhase = EditorGUILayout.Foldout(openPhase, label, true, systemTreeToggleStyle);
            _phaseFold[key] = openPhase;
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
                _deterministicFold = EditorGUILayout.Foldout(_deterministicFold, "Deterministic", true, foldStyle);

                if (_deterministicFold)
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
                _nonDeterministicFold =
                    EditorGUILayout.Foldout(_nonDeterministicFold, "Non-deterministic", true, foldStyle);

                if (_nonDeterministicFold)
                {
                    EditorGUI.indentLevel++;

                    // 2-1. Begin (Input + Sync)
                    bool hasBegin = HasAny(nonDetGroups, SystemGroup.FrameInput, SystemGroup.FrameSync);
                    if (hasBegin)
                    {
                        _beginFold = EditorGUILayout.Foldout(_beginFold, "Begin", true, foldStyle);
                        if (_beginFold)
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
                        _lateFold = EditorGUILayout.Foldout(_lateFold, "Late", true, foldStyle);
                        if (_lateFold)
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
                        _singletonsFold = EditorGUILayout.Foldout(_singletonsFold, "Singletons", true, foldStyle);
                        if (_singletonsFold)
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
                _unknownFold = EditorGUILayout.Foldout(_unknownFold, "Unknown", true, foldStyle);

                if (_unknownFold)
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

            if (!_groupFold.TryGetValue(group, out var open))
                open = true;

            open = EditorGUILayout.Foldout(open, label, true, foldStyle);
            _groupFold[group] = open;
            if (!open) return;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            foreach (var (index, sys, type) in list)
                DrawSystemRow(index, sys, type, world);

            EditorGUI.indentLevel = prevIndent;
        }

        List<(ISystem sys, Type type)> CollectWatchedSystemsForEntity(
            IWorld world,
            Entity entity,
            IReadOnlyList<ISystem> systems)
        {
            var result = new List<(ISystem, Type)>();

            if (systems == null || systems.Count == 0)
                return result;

            foreach (var sys in systems)
            {
                if (sys == null) continue;
                var tSys = sys.GetType();

                // 이 시스템이 [Watch] 속성을 가지고 있는지 먼저 거칠게 필터링
                bool hasWatchAttribute = false;
                try
                {
                    hasWatchAttribute = tSys.GetCustomAttributes(typeof(ZenSystemWatchAttribute), false).Any();
                }
                catch
                {
                    // 리플렉션 실패 시 그냥 계속 진행
                }

                if (!hasWatchAttribute)
                    continue;

                // WatchQueryRunner를 통해 이 시스템이 감시하는 엔티티 목록 수집
                var tmp = new List<Entity>();
                if (!ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world, tmp))
                    continue;

                // 현재 Find 뷰의 엔티티가 포함되어 있으면 목록에 추가
                if (tmp.Contains(entity))
                {
                    result.Add((sys, tSys));
                }
            }

            return result;
        }

        static class SystemTypeFinder
        {
            private static List<Type>? _cache;

            public static IEnumerable<Type> All()
            {
                if (_cache != null) return _cache;

                var list = new List<Type>(256);
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var asm in asms)
                {
                    var n = asm.GetName().Name;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var t in types)
                    {
                        if (t == null) continue;
                        if (t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        // ISystem 구현 여부
                        if (!typeof(ISystem).IsAssignableFrom(t)) continue;

                        // 기본 생성자 필수 (Activator.CreateInstance용)
                        if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                        list.Add(t);
                    }
                }

                _cache = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();

                return _cache;
            }
        }

        /// <summary>
        /// Find all struct types that implement IWorldSingletonComponent.
        /// </summary>
        static class SingletonTypeFinder
        {
            private static List<Type>? _cache;

            public static IEnumerable<Type> All()
            {
                if (_cache != null) return _cache;

                var list = new List<Type>(128);
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var asm in asms)
                {
                    var n = asm.GetName().Name;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var t in types)
                    {
                        if (t == null) continue;
                        if (!t.IsValueType) continue;          // struct only
                        if (t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        if (!typeof(IWorldSingletonComponent).IsAssignableFrom(t)) continue;

                        list.Add(t);
                    }
                }

                _cache = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();

                return _cache;
            }
        }
        
        void DrawSingletonRow(Type type, Entity owner, IWorld world)
        {
            var typeName = type.Name;

            // === 한 줄 Rect 계산 ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Indent 반영
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float iconW = 24f;
            const float gap = 1f;

            // 오른쪽 끝: X
            var removeRect = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
            var pingRect = new Rect(removeRect.x - iconW, rowRect.y, iconW, rowRect.height);

            // 가운데: Singleton 버튼
            float sysX = rowRect.x;
            float sysRight = pingRect.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Singleton 버튼 =====
            string label = $"{typeName}  (Entity #{owner.Id}:{owner.Gen})";

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };

            bool selected =
                _hasSelectedSingleton &&
                _selectedSingletonType == type &&
                _selectedSingletonEntity.Id == owner.Id &&
                _selectedSingletonEntity.Gen == owner.Gen;

            bool clicked = GUI.Toggle(sysRect, selected, label, btnStyle);
            if (clicked && !selected)
            {
                _selSystem = -1;
                _hasSelectedSingleton = true;
                _selectedSingletonType = type;
                _selectedSingletonEntity = owner;

                _selSysEntityCount = 0;
                _entityFold.Clear();
                _binderFold.Clear();
                _componentFold.Clear();
                _contextFold.Clear();
                _watchedFold.Clear();
                _cache.Clear();
            }

            // ===== 돋보기 버튼 (컴포넌트 타입 핑) =====
            {
                var pingBtnRect = new Rect(
                    pingRect.x,
                    pingRect.y + 1f,
                    pingRect.width,
                    pingRect.height - 2f
                );

                var searchContent = GetSearchIconContent("Ping singleton component script asset");
                var iconStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(3, 3, 3, 3),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 10
                };

                if (GUI.Button(pingBtnRect, searchContent, iconStyle))
                {
                    PingComponentType(type);
                }
            }
            
            // 🔸 삭제 버튼 (기존 그대로)
            using (new EditorGUI.DisabledScope(!_editMode))
            {
                var delStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };

                var gcDel = new GUIContent("X", "Remove this singleton from Entity");
                if (GUI.Button(removeRect, gcDel, delStyle))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Singleton",
                            $"Remove this {label} singleton?",
                            "Yes", "No"))
                    {
                        ZenEcsEditor.CommandQueue.Enqueue(EditorCommand. RemoveSingleton(type));
                        Repaint();
                    }
                }
            }
        }

        void DrawSingletonDetail(IWorld world, Type? type, Entity owner)
        {
            // 간단한 Meta 박스
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        string title = type != null ? type.Name : "Singleton";
                        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                        if (type != null)
                        {
                            string ns = string.IsNullOrEmpty(type.Namespace) ? "(global)" : type.Namespace;
                            EditorGUILayout.LabelField(ns, EditorStyles.miniLabel);
                        }

                        EditorGUILayout.LabelField(
                            $"Entity #{owner.Id}:{owner.Gen}",
                            EditorStyles.miniLabel);
                    }

                    GUILayout.FlexibleSpace();

                    if (type != null)
                    {
                        var pingContent = GetSearchIconContent("Ping singleton component script asset");
                        if (GUILayout.Button(pingContent, EditorStyles.iconButton, GUILayout.Width(20),
                                GUILayout.Height(18)))
                        {
                            PingComponentType(type);
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);

            // 실제 엔티티 정보는 기존 DrawOneEntity를 재사용
            DrawOneEntity(world, owner);
        }

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
                        _selSystem = -1;
                        _hasSelectedSingleton = false;
                        _selSysEntityCount = 0;
                        _cache.Clear();
                        _entityFold.Clear();
                        _binderFold.Clear();
                        _componentFold.Clear();
                        _contextFold.Clear();
                        _watchedFold.Clear();
                        Repaint();
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