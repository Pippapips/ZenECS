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

        bool _findMode = false; // single view mode on/off
        Entity _foundEntity; // resolved entity
        bool _foundValid = false; // found in world?

        // --- Other UI/layout state ---
        Vector2 _left, _right;
        int _selSystem = -1;
        readonly List<Entity> _cache = new(256);
        double _nextRepaint;
        private int _selSysEntityCount;

        readonly Dictionary<Entity, bool> _entityFold = new(); // entityId → fold
        readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
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
                        catch (KeyNotFoundException)
                        {
                        }
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