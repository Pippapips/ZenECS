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
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        private Color systemMetaTextColor = Color.lightGray;
        private Color systemTreeTextColor = Color.lightGray;

        private List<(ISystem sys, Type type)> CollectWatchedSystemsForEntity(
            IWorld world,
            Entity entity,
            IReadOnlyList<ISystem>? systems)
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
                if (!TryCollectEntitiesBySystemWatched(world, sys, tmp))
                    continue;

                // 현재 Find 뷰의 엔티티가 포함되어 있으면 목록에 추가
                if (tmp.Contains(entity))
                {
                    result.Add((sys, tSys));
                }
            }

            return result;
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
                        if (!t.IsValueType) continue; // struct only
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
                if (!_entityPanel.ContextFold.TryGetValue(key, out var open) || !open)
                    return false;
            }

            return any && true;
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
        
        bool AreAllBindersOpen_VisibleOnly(Entity e, (Type type, object? boxed)[] binders)
        {
            bool any = false;
            foreach (var (t, boxed) in binders)
            {
                if (boxed != null && !CanShowBinderBody(t, boxed)) continue;
                any = true;
                string key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                if (!_entityPanel.BinderFold.TryGetValue(key, out bool open) || !open)
                    return false;
            }

            return any && true;
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
        
        // =====================================================================
        //  GUI HELPERS
        // =====================================================================

        /// <summary>
        /// Draws a foldout header with an optional right-side area (e.g. counter, buttons).
        /// Returns the new foldout state.
        /// </summary>
        bool FoldoutHeader(
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
        
        void PingSystemTypeNoSelect(Type t)
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

        void PingSystemType(Type t)
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

        void PingComponentType(Type t)
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

        void PingContextType(Type t)
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
    }
}
#endif