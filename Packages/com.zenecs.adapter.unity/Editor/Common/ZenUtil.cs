// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenUtil.cs
// Purpose: Common utility functions for ZenECS editor tooling, including type
//          finders, system analysis, and entity collection helpers.
// Key concepts:
//   • Type finders: Singleton, System, Binder, Context discovery with caching.
//   • System analysis: group/phase resolution, watch attribute processing.
//   • Entity collection: gather entities watched by systems via attributes.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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
    /// <summary>
    /// Represents the kind of system execution phase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enumeration that represents the distinction between Fixed / Variable / Presentation phases.
    /// </para>
    /// </remarks>
    public enum PhaseKind
    {
        /// <summary>
        /// Unknown phase.
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Deterministic phase. Represents Fixed tick phase.
        /// </summary>
        Deterministic,
        
        /// <summary>
        /// Non-deterministic phase. Represents Frame-based phase.
        /// </summary>
        NonDeterministic,
    }
    
    public static class ZenUtil
    {
        /// <summary>
        /// Finds and pings the MonoScript corresponding to the given type in the Unity editor.
        /// </summary>
        /// <param name="t">The type to ping. If <c>null</c>, no operation is performed.</param>
        /// <remarks>
        /// <para>
        /// Finds the MonoScript corresponding to the type and selects and highlights it in the Project window.
        /// If not found, outputs a warning message to the Unity console.
        /// </para>
        /// </remarks>
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
        
        /// <summary>
        /// Static utility class for finding singleton component types.
        /// </summary>
        public static class SingletonTypeFinder
        {
            /// <summary>
            /// Returns all singleton component types.
            /// </summary>
            /// <returns>
            /// An enumeration of all value types (structs) that implement <see cref="IWorldSingletonComponent"/>.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Results are cached, and subsequent calls return the cached results.
            /// </para>
            /// </remarks>
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Singleton",
                    baseType: typeof(IWorldSingletonComponent),
                    predicate: t => t.IsValueType, // struct only
                    initialCapacity: 128);
        }
        
        /// <summary>
        /// Static utility class for finding system types.
        /// </summary>
        public static class SystemTypeFinder
        {
            /// <summary>
            /// Returns all system types.
            /// </summary>
            /// <returns>
            /// An enumeration of all concrete types that implement <see cref="ISystem"/>.
            /// Only types with parameterless constructors are returned.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Results are cached, and subsequent calls return the cached results.
            /// </para>
            /// </remarks>
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "System",
                    baseType: typeof(ISystem),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        /// <summary>
        /// Static utility class for finding binder types.
        /// </summary>
        public static class BinderTypeFinder
        {
            /// <summary>
            /// Returns all binder types.
            /// </summary>
            /// <returns>
            /// An enumeration of all concrete types that implement <see cref="IBinder"/>.
            /// Only types with parameterless constructors are returned.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Results are cached, and subsequent calls return the cached results.
            /// </para>
            /// </remarks>
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Binder",
                    baseType: typeof(IBinder),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        /// <summary>
        /// Static utility class for finding context asset types.
        /// </summary>
        public static class ContextTypeFinder
        {
            /// <summary>
            /// Returns all context asset types.
            /// </summary>
            /// <returns>
            /// An enumeration of all concrete types that inherit from <see cref="ContextAsset"/>.
            /// Only types with parameterless constructors are returned.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Results are cached, and subsequent calls return the cached results.
            /// </para>
            /// </remarks>
            public static IEnumerable<Type> All() =>
                TypeFinderCore.FindTypes(
                    cacheKey: "Context",
                    baseType: typeof(ContextAsset),
                    predicate: t => t.GetConstructor(Type.EmptyTypes) != null, // Parameterless constructor required
                    initialCapacity: 256);
        }
        
        /// <summary>
        /// Loads all context assets.
        /// </summary>
        /// <returns>
        /// A list of all <see cref="ContextAsset"/> instances in the project.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Finds and loads all assets of type <see cref="ContextAsset"/> from the Unity asset database.
        /// </para>
        /// </remarks>
        public static List<ContextAsset> LoadAllAssets()
        {
            return ZenAssetDatabase.FindAndLoadAllAssets<ContextAsset>();
        }
        
        /// <summary>
        /// Determines the system group and execution phase from a system type.
        /// </summary>
        /// <param name="t">The system type to analyze.</param>
        /// <param name="group">
        /// When this method returns, contains the <see cref="SystemGroup"/> of the system.
        /// </param>
        /// <param name="phase">
        /// When this method returns, contains the <see cref="PhaseKind"/> of the system.
        /// </param>
        /// <remarks>
        /// <para>
        /// The execution phase is determined based on the system group:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Fixed groups (FixedInput, FixedDecision, FixedSimulation, FixedPost) →
        /// <see cref="PhaseKind.Deterministic"/>
        /// </description></item>
        /// <item><description>
        /// Frame groups (FrameInput, FrameSync, FrameView, FrameUI) →
        /// <see cref="PhaseKind.NonDeterministic"/>
        /// </description></item>
        /// <item><description>
        /// Others → <see cref="PhaseKind.Unknown"/>
        /// </description></item>
        /// </list>
        /// </remarks>
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
        
        /// <summary>
        /// Collects entities that have all AllOf components specified in the system's [Watch] attribute.
        /// </summary>
        /// <param name="w">The world to search for entities.</param>
        /// <param name="system">The system instance to check for [Watch] attributes.</param>
        /// <param name="outList">
        /// When this method returns, contains a list of entities that satisfy the conditions.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if one or more entities were collected; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Examines the <see cref="ZenSystemWatchAttribute"/> applied to the system type to
        /// retrieve the list of AllOf components. Among all entities in the world, only
        /// entities that have all of those components are collected.
        /// </para>
        /// <para>
        /// Duplicate entities are automatically removed.
        /// </para>
        /// </remarks>
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
        
        /// <summary>
        /// Collects systems that observe the specified entity.
        /// </summary>
        /// <param name="world">The world that the entity belongs to.</param>
        /// <param name="entity">The entity to check for observation.</param>
        /// <param name="systems">
        /// The list of systems to examine. Returns an empty list if <c>null</c> or empty.
        /// </param>
        /// <returns>
        /// A list of tuples containing systems and their types that observe the entity.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Checks each system's <see cref="ZenSystemWatchAttribute"/> to determine
        /// if the entity is included in the list of entities observed by the system.
        /// </para>
        /// </remarks>
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
                    // Log and continue if reflection fails
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
