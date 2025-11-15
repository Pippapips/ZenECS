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
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;
using ZenECS.EditorTools;
using Object = UnityEngine.Object;

namespace ZenECS.EditorWindows
{
    public sealed class EcsExplorerWindow : EditorWindow
    {
        [MenuItem("ZenECS/Tools/ECS Explorer")]
        public static void Open() => GetWindow<EcsExplorerWindow>("ECS Explorer");

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

        bool _findMode = false;   // single view mode on/off
        Entity _foundEntity;      // resolved entity
        bool _foundValid = false; // found in world?

        // --- Other UI/layout state ---
        Vector2 _left, _right;
        int _selSystem = -1;
        readonly List<Entity> _cache = new(256);
        double _nextRepaint;
        private int _selSysEntityCount;
        private static GUIStyle? _bigPlusButton;
        static bool _bigPlusReady;

        // System.Enabled 리플렉션 캐시
        static readonly Dictionary<Type, PropertyInfo?> _systemEnabledPropCache = new();

        readonly Dictionary<Entity, bool> _entityFold = new();    // entityId → fold
        readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _binderFold = new();    // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _contextFold = new();   // $"{entityId}:{typeName}:CTX" → fold
        bool _editMode = true;

        static EcsDriver? _driver;

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
            _selSysEntityCount = 0;
            _cache.Clear();
            _entityFold.Clear();
            _binderFold.Clear();
            _componentFold.Clear();
            _contextFold.Clear();
            _findMode = false;
            _findEntityId = null;
            _foundValid = false;
        }

        void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                _selSystem = -1;
                _selSysEntityCount = 0;
                _cache.Clear();
                _findMode = false;
                _findEntityId = null;
                _foundValid = false;
                Repaint();
            }
        }

        void OnGUI()
        {
            IWorld? world = null;
            if (!_driver)
            {
                _driver = Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Exclude);
            }

            if (_driver && _driver.Kernel != null)
            {
                world = _driver.Kernel.CurrentWorld;
            }

            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            // ====== Vertical Splitter Layout ======
            using (new EditorGUILayout.HorizontalScope())
            {
                // ---------- Left: Systems ----------
                using (var sv = new EditorGUILayout.ScrollViewScope(_left, GUILayout.Width(300)))
                {
                    _left = sv.scrollPosition;

                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Systems", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Clear", GUILayout.Width(60)))
                        {
                            _selSystem = -1;
                            _selSysEntityCount = 0;
                            _cache.Clear();
                        }
                    }

                    EditorGUILayout.Space(4);

                    if (systems == null || systems.Count == 0)
                        EditorGUILayout.HelpBox("No systems registered. Ensure init & attach.", MessageType.Info);

                    if (systems != null)
                    {
                        for (int i = 0; i < systems.Count; i++)
                        {
                            var sys = systems[i];
                            if (sys == null) continue;

                            var tSys = sys.GetType();
                            var typeName = tSys.Name;

                            // --- ISystemEnabledFlag 구현 여부 및 Enabled 값 ---
                            bool hasEnabled = sys is ISystemEnabledFlag;
                            bool enabledValue = hasEnabled && ((ISystemEnabledFlag)sys).Enabled;

                            // ===== 한 줄 전체 Rect =====
                            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
                            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

                            const float pauseW = 24f;
                            const float gap = 4f;

                            var pauseRect = new Rect(rowRect.x, rowRect.y, pauseW, rowRect.height);
                            var sysRect = new Rect(
                                pauseRect.xMax + gap,
                                rowRect.y,
                                rowRect.width - (pauseRect.width + gap),
                                rowRect.height
                            );

                            // ===== Enabled 토글 (일시정지 버튼 스타일) =====
                            using (new EditorGUI.DisabledScope(!hasEnabled))
                            {
                                // 시스템 선택 버튼과 동일한 높이/위치로 맞춘 버튼 영역
                                var btnRect = new Rect(
                                    pauseRect.x,
                                    pauseRect.y + 1f,
                                    pauseRect.width,
                                    pauseRect.height - 2f
                                );

                                // Pause 아이콘
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

                                if (hasEnabled && !enabledValue)
                                {
                                    // 🔹 Unity 툴바 일시정지 느낌의 파란색 (필요하면 살짝씩 조정해도 됨)
                                    GUI.backgroundColor = EditorGUIUtility.isProSkin
                                        ? new Color(0.24f, 0.48f, 0.90f, 1f)  // Dark Skin
                                        : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                                    GUI.contentColor = Color.white; // 파란 배경 위 아이콘/글자
                                }
                                else
                                {
                                    GUI.backgroundColor = oldBg;
                                    GUI.contentColor = oldCont;
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

                            // ===== System 선택 버튼 =====

                            var watchedCount = ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCountByWatch(sys, world);
                            typeName = $"{typeName} ({watchedCount})";
                            bool selected = _selSystem == i;
                            bool clicked_ = GUI.Toggle(sysRect, selected, typeName, "Button");
                            if (clicked_)
                                _selSystem = i;
                        }
                    }
                }

                // ---------- Right: Entities ----------
                using (var sv = new EditorGUILayout.ScrollViewScope(_right))
                {
                    _right = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    // --- Toolbar (Find / Clear Filter / Edit Mode) ---
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // if (_selSysEntityCount > 0)
                        // {
                        //     EditorGUILayout.LabelField($"Entities ({_selSysEntityCount})", EditorStyles.boldLabel);
                        // }
                        // else
                        {
                            EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.Space(4);

                    var done = false;

                    // --- Single-entity view takes precedence ---
                    if (_findMode)
                    {
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            if (_findEntityId.HasValue && _findEntityGen.HasValue)
                            {
                                if (world == null)
                                {
                                    EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
                                }
                                else if (_foundValid)
                                {
                                    EditorGUILayout.LabelField($"Entity #{_findEntityId.Value}:{_findEntityGen.Value}",
                                        EditorStyles.boldLabel);
                                    GUILayout.Space(2);
                                    DrawOneEntity(world, _foundEntity);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox(
                                        $"No entity with ID {_findEntityId.Value}:{_findEntityGen.Value} in this World.",
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

                            GUILayout.Space(4);
                            if (GUILayout.Button("Back to List"))
                            {
                                _entityIdText = "";
                                _findEntityId = null;

                                _entityGenText = "0";
                                _findEntityGen = null;

                                _foundValid = false;
                                _findMode = false;
                            }
                        }

                        done = true; // stop here in single view mode
                    }

                    if (!done)
                    {
                        // --- Normal (list) mode below ---
                        if (systems == null || _selSystem < 0 || _selSystem >= systems.Count)
                        {
                            EditorGUILayout.HelpBox("Select a system to inspect.", MessageType.None);
                            done = true;
                        }
                    }

                    if (!done)
                    {
                        if (world == null)
                        {
                            EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
                            done = true;
                        }
                    }

                    if (!done)
                    {
                        var sys = systems?[_selSystem];
                        _cache.Clear();

                        if (!ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world, _cache))
                            EditorGUILayout.HelpBox("No inspector. Implement IInspectableSystem or add [Watch].",
                                MessageType.Info);

                        _selSysEntityCount = _cache.Count;

                        foreach (var e in _cache.Distinct())
                            if (world != null)
                                DrawOneEntity(world, e);
                    }
                }
            }

            GUILayout.Space(4);
            DrawFooter();
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

        private void DrawFooter()
        {
            IWorld? world = null;
            if (!_driver)
            {
                _driver = Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Exclude);
            }

            if (_driver && _driver.Kernel != null)
            {
                world = _driver.Kernel.CurrentWorld;
            }

            var systems = world?.GetAllSystems(); // running system only (not init/deinit)

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
// ===== Global Pause 토글 버튼 (커널 기반, System Pause와 동일 스타일) =====
                var hasKernel = _driver != null && _driver.Kernel != null;
                var isPaused = hasKernel && _driver!.Kernel!.IsPaused;

                // 툴바 라인 높이에 맞춘 Rect
                var rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                var pauseRect = GUILayoutUtility.GetRect(24f, rowHeight, GUILayout.Width(24f));

                using (new EditorGUI.DisabledScope(!hasKernel))
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

                    if (hasKernel && isPaused)
                    {
                        // Unity 툴바 Pause랑 비슷한 파란색
                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                            ? new Color(0.24f, 0.48f, 0.90f, 1f)  // Dark Skin
                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                        GUI.contentColor = Color.white;
                    }

                    if (GUI.Button(btnRect, pauseContent, pauseStyle))
                    {
                        if (hasKernel)
                        {
                            _driver!.Kernel!.TogglePause();
                        }
                    }

                    GUI.backgroundColor = oldBg;
                    GUI.contentColor = oldCont;
                }

                GUILayout.Space(8);

                // ===== 기존 정보 라벨들 =====
                var elapsed = _driver?.Kernel?.SimulationAccumulatorSeconds ?? 0;
                var systemCount = systems?.Count ?? 0;
                var entityCount = world?.GetAllEntities()?.Count ?? 0;

                GUILayout.Label($"Since running in seconds: {elapsed:0}");
                GUILayout.Space(12);
                GUILayout.Label($"Systems: {systemCount}");
                GUILayout.Space(12);
                GUILayout.Label($"Total Entities: {entityCount}");

                GUILayout.FlexibleSpace();

                GUILayout.Label(LABEL_ENTITY_ID, GUILayout.Width(80));
                _entityIdText = GUILayout.TextField(_entityIdText, GUILayout.Width(80));
                _entityIdText = new string(_entityIdText.Where(char.IsDigit).ToArray());

                _entityGenText = GUILayout.TextField(_entityGenText, GUILayout.Width(40));
                _entityGenText = new string(_entityGenText.Where(char.IsDigit).ToArray());

                if (int.TryParse(_entityGenText, out var gen))
                {
                    if (gen < 0) _entityGenText = "0";
                }

                // Find => enter single-entity view (no system switching)
                var contentFind = new GUIContent(BTN_FIND, TIP_FIND);
                if (GUILayout.Button(contentFind, GUILayout.Width(56)))
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
                        _findMode = true;
                    }
                    else
                    {
                        _findEntityId = null;
                        _findEntityGen = null;
                        _foundValid = false;
                        _findMode = true; // still enter to show guidance
                    }

                    Repaint();
                }

                // Clear Filter => exit single-entity view
                var contentClear = new GUIContent(BTN_CLEAR_FILTER, TIP_CLEAR);
                if (GUILayout.Button(contentClear, GUILayout.Width(60)))
                {
                    _entityIdText = "";
                    _entityGenText = "0";
                    _findEntityId = null;
                    _findEntityGen = null;
                    _foundValid = false;
                    _findMode = false;
                    Repaint();
                }

                _editMode = GUILayout.Toggle(_editMode, "Edit", "Button", GUILayout.Width(60));
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

                ZenFoldoutHeader.DrawRow(ref openE, headRect, $"Entity #{e.Id}:{e.Gen}", "", rRight =>
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

                    // 그 왼쪽: 메인 뷰 선택 버튼 (•)
                    Rect? rSel = null;
                    if (EcsExplorerActions.TryGetEntityMainView(world, e, out var go))
                    {
                        rSel = new Rect(right - wBtn + 2, yBtn + 1, wBtn, hBtn);
                        right = rSel.Value.x - gap;
                    }

                    using (new EditorGUI.DisabledScope(!_editMode))
                    {
                        if (GUI.Button(rDel, "X", style))
                        {
                            if (EditorUtility.DisplayDialog(
                                    "Remove Entity",
                                    $"Remove this entity?\n\nEntity #{e.Id}:{e.Gen}",
                                    "Yes", "No"))
                            {
                                world.DespawnEntity(e);
                                Repaint();
                            }
                        }
                    }

                    var gcPing = GetSearchIconContent("Ping entity view in Hierarchy");
                    if (rSel.HasValue && GUI.Button(rSel.Value, gcPing, EditorStyles.iconButton))
                    {
                        EcsExplorerActions.TrySelectEntityMainView(go);
                    }
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
                EditorGUI.LabelField(rLabel, $"Components: {arr.Length}");

                // Add Component 버튼 (이제 여기!)
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
                                world.AddComponentBoxed(e, inst);
                                Repaint();
                            },
                            rAddComp, // 이제 Components 줄 오른쪽 Rect 기준으로
                            $"Entity #{e.Id}:{e.Gen} Add Component"
                        );
                    }
                }

                // 실제 리스트 렌더링
                DrawComponentsList(world, e, arr);

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
                            "Contexts API가 연결되지 않았습니다. (GetAllContexts / AddContextFromAsset / AttachContext / RemoveContext)",
                            MessageType.None);
                    }
                }


                {
                    // ===== Binders Summary line =====
                    var bindersOk = BinderApi.TryGetAll(world, e, out var binders);
                    if (!bindersOk)
                    {
                        EditorGUILayout.HelpBox(
                            "Binders API가 연결되지 않았습니다. (GetAllBinders/AddBinderBoxed/RemoveBinderBoxed/ReplaceBinderBoxed 탐색 실패)",
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
                                var rRemove = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rPause = new Rect(rRemove.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // binder 활성 상태
                                var hasBinder = binder != null;
                                bool isEnabled = hasBinder && binder is { Enabled: true };
                                bool canToggle = hasBinder && _editMode; // 읽기전용일 땐 토글 비활성

                                // 🔹 Enabled Pause 스타일 (System / Global Pause와 동일)
                                using (new EditorGUI.DisabledScope(!canToggle))
                                {
                                    // 버튼 영역 보정
                                    var btnRect = new Rect(
                                        rPause.x,
                                        rPause.y,
                                        rPause.width,
                                        rPause.height
                                    );

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

                                    if (GUI.Button(btnRect, pauseContent, pauseStyle) && canToggle)
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
                                    if (GUI.Button(rRemove, gcDel, style))
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


        void DrawComponentsList(IWorld world, Entity e, (Type type, object? boxed)[] compsArray)
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
                                var rReset = new Rect(rRight.xMax - 42.5f, rRight.y, 20, rRight.height);
                                var rRemove = new Rect(rRight.xMax - 20, rRight.y, 20, rRight.height);

                                using (new EditorGUI.DisabledScope(!_editMode))
                                {
                                    using (new EditorGUI.DisabledScope(!hasFields))
                                    {
                                        if (GUI.Button(rReset, "R", EditorStyles.miniButton))
                                        {
                                            // if (EditorUtility.DisplayDialog(
                                            //         "Reset Component",
                                            //         $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                            //         "Yes", "No"))
                                            {
                                                var def = ZenDefaults.CreateWithDefaults(t);
                                                world.ReplaceComponentBoxed(e, def);
                                                Repaint();
                                            }
                                        }
                                    }

                                    if (GUI.Button(rRemove, "X", EditorStyles.miniButton))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Component",
                                                $"Remove this component?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                "Yes", "No"))
                                        {
                                            world.RemoveComponentBoxed(e, t);
                                            _componentFold.Remove(ck);
                                            Repaint();
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
                            if (EditorGUI.EndChangeCheck() && _editMode) world.ReplaceComponentBoxed(e, obj);
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
        public void SelectEntity(IWorld? world, int entityId, int entityGen)
        {
            if (world != null)
            {
                _foundValid = world?.IsAlive(entityId, entityGen) ?? false;
                if (_foundValid)
                {
                    _entityIdText = entityId.ToString();
                    _entityGenText = entityGen.ToString();
                    _findEntityId = entityId;
                    _findEntityGen = entityGen;
                    _foundEntity = _foundValid
                        ? (Entity)Activator.CreateInstance(typeof(Entity), entityId, entityGen)
                        : default;
                    _findMode = true;
                    Repaint();
                    return;
                }
            }

            _findEntityId = null;
            _findEntityGen = null;
            _foundValid = false;
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
                        var ctx = perEntityAsset.CreateContextForEntity(w, e);
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
                                var rRemove = new Rect(rRight.xMax - 20f, rRight.y, 20f, rRight.height);

                                using (new EditorGUI.DisabledScope(!_editMode))
                                {
                                    if (GUI.Button(rRemove, "X", EditorStyles.miniButton))
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
    }
}
#endif