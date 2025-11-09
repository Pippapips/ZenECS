// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemPlanner.cs
// Purpose: Determine deterministic execution order for ECS systems by grouping,
//          validating, and topologically sorting constraints.
// Key concepts:
//   • Groups: FrameSetup / Simulation / Presentation buckets.
//   • Constraints: OrderBefore / OrderAfter honored only within the same group.
//   • Deterministic: Kahn topological sort with lexical tie-break by type name.
//   • Lifecycle views: forward order for Initialize, reverse for Shutdown.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal.Systems
{
    /// <summary>
    /// Provides deterministic planning and ordering for ECS systems. Systems are
    /// grouped by phase, validated for marker/attribute consistency, and then
    /// topologically sorted within each group.
    /// </summary>
    internal static class SystemPlanner
    {
        /// <summary>
        /// Immutable plan describing execution order per phase and lifecycle views.
        /// </summary>
        public sealed class Plan
        {
            /// <summary>
            /// Create a new plan with ordered sequences for all phases.
            /// </summary>
            /// <param name="frameSetup">Ordered systems for <c>FrameSetup</c>.</param>
            /// <param name="simulation">Ordered systems for <c>Simulation</c>.</param>
            /// <param name="presentation">Ordered systems for <c>Presentation</c>.</param>
            public Plan(IReadOnlyList<ISystem>? frameSetup,
                        IReadOnlyList<ISystem>? simulation,
                        IReadOnlyList<ISystem>? presentation)
            {
                FrameSetup   = frameSetup   ?? Array.Empty<ISystem>();
                Simulation   = simulation   ?? Array.Empty<ISystem>();
                Presentation = presentation ?? Array.Empty<ISystem>();
            }

            /// <summary>Execution order for the <c>FrameSetup</c> phase.</summary>
            public IReadOnlyList<ISystem> FrameSetup { get; }

            /// <summary>Execution order for the <c>Simulation</c> phase.</summary>
            public IReadOnlyList<ISystem> Simulation { get; }

            /// <summary>Execution order for the <c>Presentation</c> phase.</summary>
            public IReadOnlyList<ISystem> Presentation { get; }

            /// <summary>
            /// Combined forward execution order across all phases.
            /// </summary>
            public IEnumerable<ISystem> AllInExecutionOrder =>
                FrameSetup.Concat(Simulation).Concat(Presentation);

            /// <summary>
            /// Ordered view of systems implementing <see cref="ISystemLifecycle"/> for initialization
            /// (forward execution order).
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleInitializeOrder =>
                AllInExecutionOrder.OfType<ISystemLifecycle>();

            /// <summary>
            /// Ordered view of systems implementing <see cref="ISystemLifecycle"/> for shutdown
            /// (reverse execution order).
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleShutdownOrder =>
                AllInExecutionOrder.Reverse().OfType<ISystemLifecycle>();
        }

        /// <summary>
        /// Build a plan by grouping, validating, and sorting the given systems.
        /// </summary>
        /// <param name="systems">System instances to analyze; may be <c>null</c>.</param>
        /// <param name="warn">Optional sink for non-fatal warnings (e.g., cross-group constraints).</param>
        /// <returns>A <see cref="Plan"/> or <c>null</c> if <paramref name="systems"/> is <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when conflicting markers/attributes exist, or a cycle is detected inside a group.
        /// </exception>
        public static Plan? Build(IEnumerable<ISystem>? systems, Action<string>? warn = null)
        {
            if (systems == null) return null;

            var buckets = new Dictionary<SystemGroup, List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)>>()
            {
                [SystemGroup.FrameSetup]   = new(),
                [SystemGroup.Simulation]   = new(),
                [SystemGroup.Presentation] = new()
            };

            // 1) Classification + constraint collection
            foreach (ISystem s in systems)
            {
                Type t = s.GetType();

                // Validate markers and attributes
                ValidatePhaseMarkers(t);

                SystemGroup group = ResolveGroup(t);
                var before = t.GetCustomAttributes<OrderBeforeAttribute>().Select(a => a.Target).ToHashSet();
                var after  = t.GetCustomAttributes<OrderAfterAttribute>().Select(a => a.Target).ToHashSet();
                buckets[group].Add((t, s, before, after));
            }

            // 2) Sort each group independently
            List<ISystem> setup = TopoSortWithinGroup(buckets[SystemGroup.FrameSetup], warn);
            List<ISystem> simu  = TopoSortWithinGroup(buckets[SystemGroup.Simulation], warn);
            List<ISystem> pres  = TopoSortWithinGroup(buckets[SystemGroup.Presentation], warn);

            return new Plan(setup, simu, pres);
        }

        /// <summary>
        /// Resolve the execution group for a system type.
        /// </summary>
        /// <param name="t">System type.</param>
        /// <returns>The resolved <see cref="SystemGroup"/>.</returns>
        private static SystemGroup ResolveGroup(Type t)
        {
            if (t.IsDefined(typeof(FrameSetupGroupAttribute), false))   return SystemGroup.FrameSetup;
            if (t.IsDefined(typeof(PresentationGroupAttribute), false)) return SystemGroup.Presentation;
            if (t.IsDefined(typeof(SimulationGroupAttribute), false))   return SystemGroup.Simulation;

            if (typeof(IPresentationSystem).IsAssignableFrom(t)) return SystemGroup.Presentation;
            if (typeof(IFrameSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFixedRunSystem).IsAssignableFrom(t))     return SystemGroup.Simulation;
            if (typeof(IVariableRunSystem).IsAssignableFrom(t))  return SystemGroup.Simulation;

            return SystemGroup.Simulation;
        }

        /// <summary>
        /// Validate that a system type has consistent phase markers and group attributes.
        /// </summary>
        /// <param name="t">System type.</param>
        /// <exception cref="InvalidOperationException">Thrown if multiple markers or conflicting attributes exist.</exception>
        private static void ValidatePhaseMarkers(Type t)
        {
            int phaseCount =
                (typeof(IFixedRunSystem).IsAssignableFrom(t)     ? 1 : 0) +
                (typeof(IVariableRunSystem).IsAssignableFrom(t)  ? 1 : 0) +
                (typeof(IPresentationSystem).IsAssignableFrom(t) ? 1 : 0);
            if (phaseCount > 1)
                throw new InvalidOperationException($"{t.Name} implements multiple phase markers.");

            int groupAttrCount =
                (t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(SimulationGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(PresentationGroupAttribute), false) ? 1 : 0);
            if (groupAttrCount > 1)
                throw new InvalidOperationException($"{t.Name} has multiple group attributes.");

            if (groupAttrCount == 1)
            {
                var inferred =
                    typeof(IPresentationSystem).IsAssignableFrom(t) ? SystemGroup.Presentation :
                    typeof(IFrameSetupSystem).IsAssignableFrom(t)   ? SystemGroup.FrameSetup :
                    (typeof(IFixedRunSystem).IsAssignableFrom(t) || typeof(IVariableRunSystem).IsAssignableFrom(t))
                        ? SystemGroup.Simulation : (SystemGroup?)null;

                var attrGroup =
                    t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? SystemGroup.FrameSetup :
                    t.IsDefined(typeof(PresentationGroupAttribute), false) ? SystemGroup.Presentation :
                    SystemGroup.Simulation;

                if (inferred.HasValue && inferred.Value != attrGroup)
                    throw new InvalidOperationException($"{t.Name} group attribute conflicts with its marker interface.");
            }
        }

        /// <summary>
        /// Topologically sort one group (Kahn's algorithm). Cross-group edges are ignored
        /// but can be reported via <paramref name="warn"/> as non-fatal notes.
        /// </summary>
        /// <param name="list">System nodes with within-group Before/After constraints.</param>
        /// <param name="warn">Optional sink for ignored cross-group references.</param>
        /// <returns>Deterministically ordered systems for that group.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a cycle is detected.</exception>
        /// <remarks>
        /// Tie-breaker uses the type's full name for deterministic ordering.
        /// </remarks>
        private static List<ISystem> TopoSortWithinGroup(
            List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)> list,
            Action<string>? warn)
        {
            var nodes = list.ToDictionary(x => x.type, x => x.inst);
            var edges = new Dictionary<Type, HashSet<Type>>();
            var indeg = new Dictionary<Type, int>();

            void ensure(Type t)
            {
                edges.TryAdd(t, new HashSet<Type>());
                indeg.TryAdd(t, 0);
            }

            foreach ((Type type, ISystem _, HashSet<Type> before, HashSet<Type> after) in list)
            {
                ensure(type);

                foreach (Type target in before)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderBefore] {type.Name} → {target.Name} ignored (not in same group)");
                        continue;
                    }
                    if (edges[type].Add(target))
                        indeg[target] = indeg.GetValueOrDefault(target) + 1;
                }

                foreach (Type target in after)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderAfter] {type.Name} ← {target.Name} ignored (not in same group)");
                        continue;
                    }
                    if (!edges.TryGetValue(target, out HashSet<Type>? set))
                    {
                        set = new HashSet<Type>();
                        edges[target] = set;
                        indeg.TryAdd(target, 0);
                    }
                    if (set.Add(type))
                        indeg[type] = indeg.GetValueOrDefault(type) + 1;
                }
            }

            var q = new SortedSet<Type>(
                indeg.Where(p => p.Value == 0).Select(p => p.Key),
                Comparer<Type>.Create((a, b) => string.CompareOrdinal(a.FullName, b.FullName))
            );

            var result = new List<ISystem>();
            while (q.Count > 0)
            {
                Type u = q.Min!;
                q.Remove(u);
                result.Add(nodes[u]);

                if (!edges.TryGetValue(u, out HashSet<Type>? tos))
                    continue;

                foreach (Type v in tos)
                {
                    indeg[v]--;
                    if (indeg[v] == 0) q.Add(v);
                }
            }

            if (result.Count != nodes.Count)
                throw new InvalidOperationException("Detected a cyclic dependency among systems within the same group.");

            return result;
        }
    }
}
