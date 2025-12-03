#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public enum EEntitySection
    {
        Components,
        Contexts,
        Binders,
    }

    public sealed partial class ZenEcsDebugWindow
    {
        private Dictionary<EEntitySection, List<string>> _debugMap = new()
        {
            [EEntitySection.Components] = new List<string>()
            {
                "AComponent",
                "BComponent",
                "CComponent",
            },
            [EEntitySection.Contexts] = new List<string>()
            {
                "AContext",
                "BContext",
                "CContext",
            },
            [EEntitySection.Binders] = new List<string>()
            {
                "ABinder",
                "BBinder",
                "CBinder",
            },
        };

        private readonly Dictionary<EEntitySection, bool> _debugGroupFold = new();

        private Vector2 _debugScroll;
        private bool _debugFold;

        readonly struct EntityTypeKey : IEquatable<EntityTypeKey>
        {
            public readonly int Id;
            public readonly int Gen;
            public readonly Type Type;

            public EntityTypeKey(Entity entity, Type type)
            {
                Id = entity.Id;
                Gen = entity.Gen;
                Type = type;
            }

            public bool Equals(EntityTypeKey other)
                => Id == other.Id && Gen == other.Gen && Type == other.Type;

            public override bool Equals(object? obj)
                => obj is EntityTypeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id;
                    hashCode = (hashCode * 397) ^ Gen;
                    hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private readonly Dictionary<EntityTypeKey, bool> _debugComponentFold = new();
        private readonly Dictionary<EntityTypeKey, bool> _debugContextFold = new();

        void DrawRightDebug()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_debugScroll))
                {
                    _debugScroll = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        // 상단 Close 버튼
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(ZenStringTable.BtnClose, GUILayout.Width(80)))
                            {
                                return;
                            }
                        }

                        // 결과 표시
                        DrawDebugResult();
                    }
                }
            }
        }

        private void DrawDebugResult()
        {
            EditorGUILayout.LabelField("DebugView", ZenGUIStyles.LabelBold14);

            GUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUI.indentLevel = 0;

                DebugFoldoutHeader(ref _debugFold, "Entity", null, null, ZenGUIStyles.SystemFoldout);
                if (_debugFold)
                {
                    EditorGUI.indentLevel++;

                    DebugDrawGroupLeaf(EEntitySection.Components, "Components", _debugMap, ZenGUIStyles.SystemFoldout);
                    DebugDrawGroupLeaf(EEntitySection.Contexts, "Contexts", _debugMap, ZenGUIStyles.SystemFoldout);
                    DebugDrawGroupLeaf(EEntitySection.Binders, "Binders", _debugMap, ZenGUIStyles.SystemFoldout);

                    EditorGUI.indentLevel++;
                }
            }
        }

        bool DebugFoldoutHeader(
            ref bool isOpen,
            string label,
            string? rightLabel = null,
            Action? rightGui = null,
            GUIStyle? style = null)
        {
            style ??= EditorStyles.foldoutHeader;

            isOpen = EditorGUILayout.Foldout(isOpen, label, true, style);

            if (!string.IsNullOrEmpty(rightLabel))
            {
                EditorGUILayout.LabelField(rightLabel, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            }

            rightGui?.Invoke();

            return isOpen;
        }

        void DebugDrawGroupLeaf(
            EEntitySection section,
            string label,
            Dictionary<EEntitySection, List<string>> map,
            GUIStyle foldStyle)
        {
            if (!map.TryGetValue(section, out var list) || list.Count == 0)
                return;

            bool open = _debugGroupFold.GetValueOrDefault(section, true);
            open = EditorGUILayout.Foldout(open, label, true, foldStyle);
            _debugGroupFold[section] = open;
            if (!open) return;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            if (section == EEntitySection.Components)
            {
                DebugDrawComponents();
            }
            else if (section == EEntitySection.Contexts)
            {
                DebugDrawContexts();
            }
            else
            {
                DebugDrawComponents();
            }

            // foreach (string? s in list)
            //     DebugDrawSystemRow(s);

            EditorGUI.indentLevel = prevIndent;
        }

        bool DrawFoldoutWithRightButtons(
            ref bool isOpen,
            string label,
            Action rightButtonsGui,
            GUIStyle foldStyle)
        {
            var rowRect = GUILayoutUtility.GetRect(
                0,
                EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true));

            rowRect = EditorGUI.IndentedRect(rowRect);

            const float buttonWidth = 20f;
            const float gap = 2f;

            // 오른쪽 버튼들 자리 확보
            var rightRect = new Rect(
                rowRect.xMax - buttonWidth,
                rowRect.y,
                buttonWidth,
                rowRect.height);

            // foldout은 오른쪽 버튼 영역을 제외한 왼쪽만 사용
            var foldRect = new Rect(
                rowRect.x,
                rowRect.y,
                rowRect.width - buttonWidth - gap,
                rowRect.height);

            // toggleOnLabelClick은 true로 써도 괜찮음 (foldRect 안에서만 먹음)
            isOpen = EditorGUI.Foldout(foldRect, isOpen, label, true, foldStyle);

            // 오른쪽 버튼은 foldout rect와 안 겹치므로 클릭이 정상 작동
            GUILayout.BeginArea(rightRect);
            rightButtonsGui?.Invoke();
            GUILayout.EndArea();

            return isOpen;
        }

        void DebugDrawComponents()
        {
            if (_kernel == null || _world == null) return;
            int id = 1;
            int gen = 0;
            bool valid = _world.IsAlive(id, gen);
            if (!valid) return;

            ZenGUIContents.DrawLine();

            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                var comps = _world.GetAllComponents(e).ToArray();
                foreach (var (t, boxed) in comps)
                {
                    var hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                    if (!hasFields)
                    {
                        EditorGUILayout.LabelField(foldoutName + "<no-fields>", ZenGUIStyles.LabelMLNormal9);
                    }

                    if (hasFields && boxed != null)
                    {
                        var key = new EntityTypeKey(e, t);
                        var isOpen = _debugComponentFold.GetValueOrDefault(key, true);
                        isOpen = EditorGUILayout.Foldout(isOpen, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        _debugComponentFold[key] = isOpen;

                        var prevIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;

                        if (isOpen)
                        {
                            using (new LabelScope(ZenGUIStyles.LabelMLNormal9, 300))
                            {
                                try
                                {
                                    object obj = CopyBox(boxed, t);
                                    float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                                    bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                                    var body = GUILayoutUtility.GetRect(0, bodyH, GUILayout.ExpandWidth(true));
                                    var bodyInner = new Rect(body.x, body.y, body.width, body.height + 4f);

                                    EditorGUI.BeginChangeCheck();
                                    ZenComponentFormGUI.DrawSmallForm(bodyInner, obj, t);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        _world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, obj));
                                    }
                                }
                                catch (KeyNotFoundException)
                                {
                                    // 컴포넌트 타입이 레지스트리에 없는 경우는 무시
                                }
                            }
                        }

                        EditorGUI.indentLevel = prevIndent;
                    }

                    // === 한 줄 Rect 계산 ===
                    var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                    var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                    if (GUI.Button(rR2, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
                    {
                        Debug.Log("R2 clicked");
                    }

                    if (GUI.Button(rR1, "R", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }

                    ZenGUIContents.DrawLine();

                    // 아래쪽 여유
                    GUILayout.Space(2);
                }
            }
        }


        void DebugDrawContexts()
        {
            if (_kernel == null || _world == null) return;
            int id = 1;
            int gen = 0;
            bool valid = _world.IsAlive(id, gen);
            if (!valid) return;

            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                if (!ContextApi.TryGetAll(_world, e, out var ctxs))
                {
                    EditorGUILayout.HelpBox("Contexts API has been disconnected.", MessageType.None);
                    return;
                }

                foreach (var (t, boxed) in ctxs)
                {
                    string ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    string fname = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";
                    var key = new EntityTypeKey(e, t);
                    var isOpen = _debugContextFold.GetValueOrDefault(key, true);
                    isOpen = EditorGUILayout.Foldout(isOpen, fname, true, ZenGUIStyles.SystemFoldout10);
                    _debugContextFold[key] = isOpen;
                    //bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    if (!isOpen || boxed == null) continue;

                    var ctxType = t;
                    var members = new List<(string name, Type type, Func<object?> getter)>();

                    // 필드
                    foreach (var f in ctxType.GetFields(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly))
                    {
                        if (Attribute.IsDefined(f, typeof(ZenEcsExplorerHiddenAttribute), true)) continue;
                        if (Attribute.IsDefined(f, typeof(HideInInspector), true)) continue;

                        var lf = f;
                        members.Add((
                            lf.Name,
                            lf.FieldType,
                            () => lf.GetValue(boxed)
                        ));
                    }

                    // 프로퍼티
                    foreach (var p in ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                            BindingFlags.DeclaredOnly))
                    {
                        if (!p.CanRead) continue;
                        if (p.GetIndexParameters().Length > 0) continue;
                        if (Attribute.IsDefined(p, typeof(ZenEcsExplorerHiddenAttribute), true)) continue;
                        if (Attribute.IsDefined(p, typeof(HideInInspector), true)) continue;

                        var lp = p;
                        members.Add((
                            lp.Name,
                            lp.PropertyType,
                            () =>
                            {
                                try
                                {
                                    return lp.GetValue(boxed);
                                }
                                catch
                                {
                                    return null;
                                }
                            }
                        ));
                    }

                    if (members.Count == 0)
                    {
                        EditorGUILayout.LabelField("— (no public fields / properties)");
                        continue;
                    }

                    int prevIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;

                    using (new LabelScope(ZenGUIStyles.LabelMLNormal9, 300))
                    {
                        foreach (var (ctxName, mType, getter) in members)
                        {
                            object? value = null;
                            try
                            {
                                value = getter();
                            }
                            catch
                            {
                                /* ignore */
                            }

                            var rect = GUILayoutUtility.GetRect(
                                0,
                                EditorGUIUtility.singleLineHeight,
                                GUILayout.ExpandWidth(true));

                            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                            var valRect = new Rect(
                                rect.x + EditorGUIUtility.labelWidth,
                                rect.y,
                                rect.width - EditorGUIUtility.labelWidth,
                                rect.height);

                            EditorGUI.LabelField(labelRect, ctxName);

                            if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                            {
                                var obj = value as UnityEngine.Object;
                                var content = EditorGUIUtility.ObjectContent(obj, mType);

                                // 링크 커서
                                EditorGUIUtility.AddCursorRect(valRect, MouseCursor.Link);

                                // 여기에서 LabelField 대신 Button으로 그린다 = hover 색 적용됨
                                if (GUI.Button(valRect, content, ZenGUIStyles.LinkLabel))
                                {
                                    if (obj != null)
                                    {
                                        Selection.activeObject = obj;
                                        EditorGUIUtility.PingObject(obj);
                                    }
                                }
                            }
                            else
                            {
                                var text = value != null ? (value.ToString() ?? "null") : "null";
                                EditorGUI.LabelField(valRect, text);
                            }
                        }
                    }

                    EditorGUI.indentLevel = prevIndent;
                }
            }
        }

        // STYLE

        public readonly struct LabelScope : IDisposable
        {
            private readonly GUIStyle _backupStyle;
            private readonly float _backupLabelWidth;
            private readonly bool _hasCustomWidth;

            public LabelScope(GUIStyle style, float? labelWidth = null)
            {
                _backupStyle = new GUIStyle(EditorStyles.label);
                _backupLabelWidth = EditorGUIUtility.labelWidth;
                _hasCustomWidth = labelWidth.HasValue;

                ApplyStyle(style);

                if (labelWidth.HasValue)
                    EditorGUIUtility.labelWidth = labelWidth.Value;
            }

            private static void ApplyStyle(GUIStyle src)
            {
                EditorStyles.label.font = src.font;
                EditorStyles.label.fontSize = src.fontSize;
                EditorStyles.label.fontStyle = src.fontStyle;
                EditorStyles.label.alignment = src.alignment;
                EditorStyles.label.normal.textColor = src.normal.textColor;
                EditorStyles.label.richText = src.richText;
            }

            public void Dispose()
            {
                ApplyStyle(_backupStyle);
                if (_hasCustomWidth)
                    EditorGUIUtility.labelWidth = _backupLabelWidth;
            }
        }

        // COMPONENTS

        static object CopyBox(object? src, Type t)
        {
            if (src == null) return SafeNew.New(t);
            if (t.IsValueType) return src;
            var dst = SafeNew.New(t);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(dst, f.GetValue(src));
            return dst;
        }

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

        // CONTEXTS

        static class ContextApi
        {
            static MethodInfo? _miGetAllContexts; // (Entity) -> (Type, object)[] 또는 IEnumerable
            static MethodInfo? _miAddFromAsset; // (Entity, ContextAsset) -> void
            static MethodInfo? _miAttachContext; // (Entity, IContext) -> void
            static MethodInfo? _miRemoveContext; // (Entity, Type) -> void

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
                            if (ctx != null)
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


        void DebugDrawSystemRow(string s)
        {
            // === 한 줄 Rect 계산 ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Indent 반영
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float iconW = 24f; // 돋보기 / X 공통 폭
            const float gap = 1f;

            var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
            var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
            var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
            var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);

            // 가운데: System 버튼
            float sysX = rL1.xMax + gap;
            float sysRight = rR1.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Pause (Enabled 토글) =====
            using (new EditorGUI.DisabledScope(false))
            {
                var oldBg = GUI.backgroundColor;
                var oldCont = GUI.contentColor;

                // bg/content color
                // if (false)
                // {
                //     GUI.backgroundColor = EditorGUIUtility.isProSkin
                //         ? new Color(0.24f, 0.48f, 0.90f, 1f)
                //         : new Color(0.20f, 0.45f, 0.90f, 1f);
                //     GUI.contentColor = Color.white;
                // }

                if (GUI.Button(rL0, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    Debug.Log("L0 clicked");
                }

                if (GUI.Button(rL1, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    Debug.Log("L1 clicked");
                }

                GUI.backgroundColor = oldBg;
                GUI.contentColor = oldCont;
            }

            bool selected = false;
            bool clicked = GUI.Toggle(sysRect, selected, s, ZenGUIStyles.ButtonMLNormal10);
            if (clicked && !selected)
            {
                ClearState();
                //_systemTree.SelectedSystemIndex = index;
            }

            if (GUI.Button(rR1, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
            {
                Debug.Log("R1 clicked");
            }

            using (new EditorGUI.DisabledScope(false))
            {
                if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                {
                    Debug.Log("R0 clicked");
                }
            }
        }
    }
}