#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    // Fixed / Variable / Presentation 구분
    public enum PhaseKind
    {
        Unknown,
        Deterministic,
        NonDeterministic,
    }
    
    public static class ZenUtil
    {
        public static void PingType(Type? t)
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

            Debug.Log($"ZenEcsExplorer: Unable to locate a script asset for component type {t.FullName}.\nIt may not exist, or a matching type name is required to ping the script source.");
        }
        
        public static class SingletonTypeFinder
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
        
        public static class SystemTypeFinder
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
        
        public static class BinderTypeFinder
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
        }
        
        public static class ContextTypeFinder
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
                        if (LooksLikeContext(t))
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

            static bool LooksLikeContext(Type t) => typeof(ContextAsset).IsAssignableFrom(t);
        }
        
        public static List<ContextAsset> LoadAllAssets()
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
        
        public static void ResolveSystemGroupAndPhase(Type t, out SystemGroup group, out PhaseKind phase)
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
        
        /// <summary>[Watch]의 AllOf 컴포넌트를 모두 가진 엔티티를 수집(항상 동작)</summary>
        public static bool TryCollectEntitiesBySystemWatched(IWorld w, object system, List<Entity> outList)
        {
            var attrs = system.GetType().GetCustomAttributes(typeof(ZenSystemWatchAttribute), false)
                .Cast<ZenSystemWatchAttribute>().ToArray();
            if (attrs.Length == 0) return false;

            var all = w.GetAllEntities();
            foreach (var a in attrs)
            {
                var allOf = a.AllOf ?? Array.Empty<Type>();
                if (allOf.Length == 0) continue;

                foreach (var e in all)
                {
                    bool ok = true;
                    for (int i = 0; i < allOf.Length && ok; i++)
                    {
                        var component = allOf[i];
                        ok &= w.HasComponentBoxed(e, component);
                    }
                    if (ok) outList.Add(e);
                }
            }

            // 중복 제거(간단/할당 적음)
            if (outList.Count > 1)
            {
                var seen = new HashSet<int>(outList.Count);
                int write = 0;
                for (int i = 0; i < outList.Count; i++)
                    if (seen.Add(outList[i].Id))
                        outList[write++] = outList[i];
                if (write < outList.Count) outList.RemoveRange(write, outList.Count - write);
            }

            return outList.Count > 0;
        }
        
        public static List<(ISystem sys, Type type)> CollectWatchedSystemsForEntity(
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
    }
}
#endif