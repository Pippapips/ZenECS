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
using ZenECS.Core;
using ZenECS.Core.Binding;
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

        readonly Dictionary<Entity, bool> _entityFold = new();    // entityId → fold
        readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
        readonly Dictionary<string, bool> _binderFold = new();    // $"{entityId}:{typeName}" → fold
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
            _componentFold.Clear();
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
                            var s = systems[i];
                            var typeName = s.GetType().Name;
                            if (GUILayout.Toggle(_selSystem == i, typeName, "Button")) _selSystem = i;
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
                        if (_selSysEntityCount > 0)
                        {
                            EditorGUILayout.LabelField($"Entities ({_selSysEntityCount})", EditorStyles.boldLabel);
                        }
                        else
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
                var systemCount = systems?.Count ?? 0;
                var entityCount = world?.GetAllEntities()?.Count ?? 0;

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
                        _foundEntity = _foundValid ? (Entity)Activator.CreateInstance(typeof(Entity), id, gen) : default;
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

                    // label/icon candidates
                    var addLong = EditorGUIUtility.TrTextContent("+");
                    var selLong = EditorGUIUtility.TrTextContent("•");

                    float BtnH(GUIContent? gc)
                    {
                        if (gc == null) return 0f;
                        var sz = style.CalcSize(gc);
                        return Mathf.Ceil(Mathf.Max(EditorGUIUtility.singleLineHeight + 2f,
                            sz.y + style.margin.vertical + 6f));
                    }

                    const float wAdd = 20;
                    const float wSel = 20f;

                    var useAdd = addLong;
                    var useSel = selLong;

                    var hAdd = BtnH(useAdd);
                    var hSel = BtnH(useSel);
                    var hBtn = Mathf.Max(hAdd, hSel);
                    var yBtn = rRight.y + Mathf.Max(0f, (rRight.height - hBtn) * 0.5f);

                    var right = rRight.xMax;
                    var rDel = new Rect(right - 20, yBtn, 20, hBtn);
                    var rAdd = new Rect(right - (wAdd + 22.5f), yBtn, wAdd, hBtn);
                    right = rAdd.x - (useSel != null ? 3f : 0f);

                    using (new EditorGUI.DisabledScope(!_editMode))
                    {
                        if (GUI.Button(rDel, "x", style))
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

                        if (GUI.Button(rAdd, useAdd, style))
                        {
                            var all = ZenECS.EditorCommon.ZenComponentPickerWindow.FindAllZenComponents().ToList();
                            var disabled = new HashSet<Type>();
                            foreach (var (tHave, _) in world.GetAllComponents(e)) disabled.Add(tHave);

                            ZenComponentPickerWindow.Show(
                                all, disabled,
                                picked =>
                                {
                                    var inst = ZenDefaults.CreateWithDefaults(picked);
                                    world.AddComponentBoxed(e, inst);
                                    Repaint();
                                },
                                rAdd,
                                $"Entity #{e.Id}:{e.Gen} Add Component",
                                ZenComponentPickerWindow.PickerOpenMode.UtilityFixedWidth
                            );
                        }
                    }

                    if (EcsExplorerActions.TryGetEntityMainView(world, e, out var go))
                    {
                        var rSel = new Rect(right - (wSel), yBtn, wSel, hBtn);
                        if (GUI.Button(rSel, useSel, style))
                        {
                            EcsExplorerActions.TrySelectEntityMainView(go);
                        }
                    }
                }, true, false);
                _entityFold[e] = openE;

                if (!openE) return;

                // ===== Summary line =====
                var line = EditorGUIUtility.singleLineHeight;
                var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

                var compsEnum = world.GetAllComponents(e);
                var arr = compsEnum.ToArray();

                // Arrow toggle (open/close all visible components)
                var rArrow = new Rect(r.x + 3, r.y + 1, 18f, r.height - 2);
                var rLabel = new Rect(rArrow.xMax - 1f, r.y, r.width - (rArrow.width + 4f), r.height);
                var allOpen = AreAllComponentsOpen_VisibleOnly(e, arr);

                EditorGUI.BeginChangeCheck();
                var visNext = EditorGUI.Foldout(rArrow, allOpen, GUIContent.none, false);
                EditorGUIUtility.AddCursorRect(rArrow, MouseCursor.Link);
                if (EditorGUI.EndChangeCheck())
                {
                    SetAllComponentsFold(world, e, visNext);
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                EditorGUI.LabelField(rLabel, $"Components: {arr.Length}");
                DrawComponentsList(world, e, arr);

                {
                    // ===== Binders Summary line =====
                    var bindersOk = BinderApi.TryGetAll(world, e, out var binders);
                    if (!bindersOk)
                    {
                        EditorGUILayout.HelpBox("Binders API가 연결되지 않았습니다. (GetAllBinders/AddBinderBoxed/RemoveBinderBoxed/ReplaceBinderBoxed 탐색 실패)", MessageType.None);
                    }
                    else
                    {
                        var line2 = EditorGUIUtility.singleLineHeight;
                        var r2 = GUILayoutUtility.GetRect(10, line2, GUILayout.ExpandWidth(true));

                        // Fold-all for binders (보이는 것만)
                        var rArrow2 = new Rect(r2.x + 3, r2.y + 1, 18f, r2.height - 2);
                        var rLabel2 = new Rect(rArrow2.xMax - 1f, r2.y, r2.width - (rArrow2.width + 4f), r2.height);

                        bool allOpenB = AreAllBindersOpen_VisibleOnly(e, binders);
                        EditorGUI.BeginChangeCheck();
                        var visNextB = EditorGUI.Foldout(rArrow2, allOpenB, GUIContent.none, false);
                        EditorGUIUtility.AddCursorRect(rArrow2, MouseCursor.Link);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetAllBindersFold(world, e, visNextB);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        EditorGUI.LabelField(rLabel2, $"Binders: {binders.Length}");

                        // 실제 리스트
                        DrawBindersList(world, e, binders);
                    }
                }
            }
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

        bool AreAllBindersOpen_VisibleOnly(Entity e, (Type type, object boxed)[] binders)
        {
            bool any = false;
            foreach (var (t, boxed) in binders)
            {
                if (!CanShowBinderBody(t, boxed)) continue;
                any = true;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                if (!_binderFold.TryGetValue(key, out bool open) || !open)
                    return false;
            }
            return any && true;
        }

        static class BinderIntrospection
        {
            // “IBinds”, “IRequireContext” 네이밍을 느슨하게 매칭 (Generic 정의 이름 기준)
            static bool IsBindsInterface(Type t)
                => t.IsInterface && t.IsGenericType && t.Name.StartsWith("IBinds", StringComparison.Ordinal);

            static bool IsRequireCtxInterface(Type t)
                => t.IsInterface && t.IsGenericType && t.Name.StartsWith("IRequireContext", StringComparison.Ordinal);

            public static IReadOnlyList<Type> ExtractObservedComponentTypes(Type binderType)
            {
                var set = new HashSet<Type>();
                foreach (var itf in binderType.GetInterfaces())
                {
                    if (!IsBindsInterface(itf)) continue;

                    // 패턴 허용: IBinds<T>, IBinds<TDelta>, IBinds<TComp,TDelta> 등
                    foreach (var ga in itf.GetGenericArguments())
                    {
                        // 컨텍스트/바인더/시스템 등은 제외하려 시도(휴리스틱)
                        if (ga.IsAbstract) continue;
                        if (ga.IsInterface && ga.Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (ga.Namespace?.EndsWith(".Editor", StringComparison.Ordinal) == true) continue;

                        set.Add(ga);
                    }
                }
                return set.OrderBy(t => t.Name).ToArray();
            }

            public static IReadOnlyList<Type> ExtractRequiredContexts(Type binderType)
            {
                var set = new HashSet<Type>();
                foreach (var itf in binderType.GetInterfaces())
                {
                    if (!IsRequireCtxInterface(itf)) continue;

                    foreach (var ga in itf.GetGenericArguments())
                        set.Add(ga);
                }
                return set.OrderBy(t => t.Name).ToArray();
            }
        }

        static bool TryHasContext(IWorld w, Entity e, Type ctxType, out bool available)
        {
            available = w.HasContext(e, ctxType);
            return available;
        }

        // 클래스 내부(어디든)
        static class OkLabel
        {
            static GUIStyle _rightMini;
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

            public static void DrawOK(float width = 50f)
            {
                Ensure();
                GUILayout.Label(new GUIContent("<b><color=#27AE60>OK</color></b>"), _rightMini, GUILayout.Width(width));
            }
        }

        static class NotAssignLabel
        {
            static GUIStyle _rightMini;
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

            public static void Draw(float width = 50f)
            {
                Ensure();
                GUILayout.Label(new GUIContent("<b><color=#990000>Not Assigned</color></b>"), _rightMini, GUILayout.Width(width));
            }
        }

        static class ItalicLabel
        {
            static GUIStyle _leftMiniItalic;
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
            static GUIStyle _pill;
            static GUIStyle _pillRight;
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

        void DrawBinderMeta(IWorld world, Entity e, object binder)
        {
            var t = binder.GetType();

            // 1) 관찰 컴포넌트(IBinds 기반) 리스트
            var observed = BinderIntrospection.ExtractObservedComponentTypes(t);

            // Has<T> 델리게이트(이미 EcsExplorer에서 사용하던 헬퍼를 재사용)
            // 없다면: Func<Entity,bool>? GetHas(IWorld,Type) 를 구현해 캐싱하세요.
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Observing (IBinds)", EditorStyles.boldLabel);

                if (observed.Count == 0)
                {
                    EditorGUILayout.LabelField("— (no IBinds<> found)");
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
                                OkLabel.DrawOK(36f); // AVAILABLE/PRESENT → OK
                            }
                            else
                            {
                                NotAssignLabel.Draw(80);
                            }
                            // ABSENT → 라벨 없음 (이탤릭 텍스트만)
                        }
                    }
                }
            }

            // 2) 요구 컨텍스트(IRequireContext) 리스트
            var req = BinderIntrospection.ExtractRequiredContexts(t);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Required Contexts (IRequireContext)", EditorStyles.boldLabel);

                if (req.Count == 0)
                {
                    EditorGUILayout.LabelField("— (no IRequireContext<> found)");
                }
                else
                {
                    foreach (var ctx in req)
                    {
                        bool hasApi = TryHasContext(world, e, ctx, out var avail);
                        bool isOk = hasApi && avail; // API 없으면 미확인 → not OK 취급

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (isOk)
                                EditorGUILayout.LabelField(ctx.Name, GUILayout.MinWidth(80));
                            else
                                ItalicLabel.DrawLeft(ctx.Name, GUILayout.MinWidth(80));

                            GUILayout.FlexibleSpace();

                            if (isOk)
                            {
                                OkLabel.DrawOK(36f); // AVAILABLE → OK
                            }
                            else
                            {
                                // MISSING/미확인 → 라벨 없음 (이탤릭 텍스트만)
                                NotAssignLabel.Draw(36);
                            }
                        }
                    }
                }
            }
        }

        static class BinderTypeFinder
        {
            static List<Type> _cache;

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

            static bool LooksLikeBinder(Type t)
            {
                // 1) 인터페이스 이름에 Binder 포함 (e.g., IBinder, IEntityBinder 등)
                if (t.GetInterfaces().Any(i => i.Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                // 2) 특성 이름에 Binder 포함 (e.g., [ZenBinder], [Binder] 등)
                if (t.GetCustomAttributes(inherit: true).Any(a => a.GetType().Name.IndexOf("Binder", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                // 3) 타입명 규칙상 Binder 접미사
                if (t.Name.EndsWith("Binder", StringComparison.Ordinal))
                    return true;

                return false;
            }
        }

        static bool CanShowBinderBody(Type t, object boxed)
        {
            // 1) 에디터 폼으로 그릴 수 있는 필드가 있거나
            if (ZenComponentFormGUI.HasDrawableFields(t))
                return true;

            // 2) 메타(IBinds / IRequireContext)라도 있으면 바디를 보여줄 가치가 있음
            var hasObserved = BinderIntrospection.ExtractObservedComponentTypes(t).Count > 0;
            if (hasObserved) return true;

            var hasReq = BinderIntrospection.ExtractRequiredContexts(t).Count > 0;
            if (hasReq) return true;

            return false;
        }

        void DrawBindersList(IWorld world, Entity e, (Type type, object boxed)[] bindersArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                // 상단에 Add 버튼 (Binder Picker)
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(!_editMode || !BinderApi.CanAdd(world)))
                    {
                        //TODO: 이건 SO로 바인더를 추가하도록 하자 
                        if (GUILayout.Button("Add Binder", GUILayout.Width(100)))
                        {
                            // 1) 전체 바인더 타입 수집
                            var allBinders = BinderTypeFinder.All();

                            // 2) 이미 붙어있는 타입은 disabled 처리
                            var disabledB = new HashSet<Type>(bindersArray.Select(x => x.type));

                            // 3) ZenBinderPickerWindow 호출 (네가 올려준 시그니처와 동일)
                            var activatorRect = GUILayoutUtility.GetLastRect(); // 버튼 렌더 직후 rect
                            ZenBinderPickerWindow.Show(
                                allBinderTypes: allBinders,
                                disabled: disabledB,
                                onPick: picked =>
                                {
                                    var inst = ZenDefaults.CreateWithDefaults(picked); // 기존 컴포넌트 폼과 동일한 기본값 팩토리
                                    BinderApi.Add(world, e, inst);
                                    Repaint();
                                },
                                activatorRectGui: activatorRect,
                                title: $"Entity #{e.Id}:{e.Gen} - Add Binder"
                            );
                        }
                    }
                }

                foreach (var (t, boxed) in bindersArray)
                {
                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                    if (!_binderFold.ContainsKey(ck)) _binderFold[ck] = false;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    bool hasMetaOrFields = CanShowBinderBody(t, boxed); // 메타 or 필드가 하나라도 있으면 true

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // ===== Binder header =====
                        var headRectB = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
                        bool openB = _binderFold[ck];

                        ZenFoldoutHeader.DrawRow(
                            ref openB,
                            headRectB,
                            t.Name,
                            nameSpace: t.Namespace,
                            rRight =>
                            {
                                var rReset = new Rect(rRight.xMax - 42.5f, rRight.y, 20, rRight.height);
                                var rRemove = new Rect(rRight.xMax - 20, rRight.y, 20, rRight.height);

                                using (new EditorGUI.DisabledScope(!_editMode))
                                {
                                    //TODO: 이건 Pause/Unpause로 하자
                                    // using (new EditorGUI.DisabledScope(!hasFields || !BinderApi.CanReplace(world)))
                                    // {
                                    //     if (GUI.Button(rReset, "R", EditorStyles.miniButton))
                                    //     {
                                    //         if (EditorUtility.DisplayDialog(
                                    //                 "Reset Binder",
                                    //                 $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                    //                 "Yes", "No"))
                                    //         {
                                    //             var def = ZenDefaults.CreateWithDefaults(t);
                                    //             BinderApi.Replace(world, e, def);
                                    //             Repaint();
                                    //         }
                                    //     }
                                    // }

                                    using (new EditorGUI.DisabledScope(!BinderApi.CanRemove(world)))
                                    {
                                        if (GUI.Button(rRemove, "X", EditorStyles.miniButton))
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
                                }
                            },
                            foldable: hasMetaOrFields,
                            false
                        );

                        _binderFold[ck] = openB;

                        // ===== body =====
                        if (!_binderFold[ck]) continue;

                        // ... 바인더 헤더/폴드아웃 처리 이후, 바디 영역 진입 직후:
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
                        catch (KeyNotFoundException) { }
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
                                            if (EditorUtility.DisplayDialog(
                                                    "Reset Component",
                                                    $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                    "Yes", "No"))
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
    }
}
#endif