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
    // Fixed / Variable / Presentation distinction
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

            if (ZenAssetDatabase.PingMonoScript(t))
                return;

            Debug.Log($"ZenEcsExplorer: Unable to locate a script asset for component type {t.FullName}.\nIt may not exist, or a matching type name is required to ping the script source.");
        }

        // ──────────────────────────────────────────────
        // Common TypeFinder Infrastructure
        // ──────────────────────────────────────────────

        /// <summary>
        /// Common type search infrastructure. Provides logic shared by all TypeFinder classes.
        /// </summary>
        private static class TypeFinderCore
        {
            private static readonly Dictionary<string, List<Type>> _cache = new();

            /// <summary>
            /// Returns a list of relevant assemblies.
            /// </summary>
            private static IEnumerable<System.Reflection.Assembly> GetRelevantAssemblies()
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in asms)
                {
                    var n = asm.GetName().Name;
                    if (n == null) continue;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

                    yield return asm;
                }
            }

            /// <summary>
            /// Searches for types matching the specified conditions.
            /// </summary>
            /// <param name="cacheKey">Cache key (unique identifier)</param>
            /// <param name="baseType">Base type (e.g., ISystem, IBinder)</param>
            /// <param name="predicate">Additional filtering condition</param>
            /// <param name="initialCapacity">Initial list capacity</param>
            /// <returns>List of found types</returns>
            public static IEnumerable<Type> FindTypes(
                string cacheKey,
                Type baseType,
                Func<Type, bool> predicate,
                int initialCapacity = 256)
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                    return cached;

                var list = new List<Type>(initialCapacity);

                foreach (var asm in GetRelevantAssemblies())
                {
                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[TypeFinderCore] Failed to get types from assembly {asm.GetName().Name}: {ex.Message}");
                        continue;
                    }

                    foreach (var t in types)
                    {
                        if (t == null) continue;
                        if (t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        if (!baseType.IsAssignableFrom(t)) continue;
                        if (!predicate(t)) continue;

                        list.Add(t);
                    }
                }

                var result = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();

                _cache[cacheKey] = result;
                return result;
            }

            /// <summary>
            /// Invalidates the cache. Must be called on assembly reload.
            /// </summary>
            public static void ClearCache()
            {
                _cache.Clear();
            }
        }
        
        public static class SingletonTypeFinder
        {
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Singleton",
                    baseType: typeof(IWorldSingletonComponent),
                    predicate: t => t.IsValueType, // struct only
                    initialCapacity: 128);
        }
        
        public static class SystemTypeFinder
        {
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "System",
                    baseType: typeof(ISystem),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        public static class BinderTypeFinder
        {
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Binder",
                    baseType: typeof(IBinder),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        public static class ContextTypeFinder
        {
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Context",
                    baseType: typeof(ContextAsset),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        public static List<ContextAsset> LoadAllAssets()
        {
            return ZenAssetDatabase.FindAndLoadAllAssets<ContextAsset>();
        }
        
        public static void ResolveSystemGroupAndPhase(Type t, out SystemGroup group, out PhaseKind phase)
        {
            group = SystemUtil.ResolveGroup(t);

            switch (group)
            {
                // Fixed tick = Deterministic
                case SystemGroup.FixedInput:
                case SystemGroup.FixedDecision:
                case SystemGroup.FixedSimulation:
                case SystemGroup.FixedPost:
                    phase = PhaseKind.Deterministic;
                    break;

                // Frame-based = Non-deterministic
                case SystemGroup.FrameInput:
                case SystemGroup.FrameSync:
                case SystemGroup.FrameView:
                case SystemGroup.FrameUI:
                    phase = PhaseKind.NonDeterministic;
                    break;

                default:
                    // If group is not specified, categorize as Unknown
                    phase = PhaseKind.Unknown;
                    break;
            }
        }
        
        /// <summary>Collects entities that have all AllOf components from [Watch] (always active)</summary>
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

            // Remove duplicates (simple/low allocation)
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

                // First, roughly filter whether this system has [Watch] attribute
                bool hasWatchAttribute = false;
                try
                {
                    hasWatchAttribute = tSys.GetCustomAttributes(typeof(ZenSystemWatchAttribute), false).Any();
                }
                catch (Exception ex)
                {
                    // 리플렉션 실패 시 로깅하고 계속 진행
                    Debug.LogWarning($"[ZenUtil] Failed to get custom attributes for type {tSys.FullName}: {ex.Message}");
                }

                if (!hasWatchAttribute)
                    continue;

                // Collect list of entities watched by this system via WatchQueryRunner
                var tmp = new List<Entity>();
                if (!TryCollectEntitiesBySystemWatched(world, sys, tmp))
                    continue;

                // Add to list if current Find view's entity is included
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
