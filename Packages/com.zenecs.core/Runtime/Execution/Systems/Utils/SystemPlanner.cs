// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemPlanner.cs
// Purpose: Determine deterministic execution order for ECS systems by grouping,
//          validating, and topologically sorting constraints.
// Key concepts:
//   • Groups: FixedInput / FixedDecision / FixedSimulation / FixedPost
//             FrameInput / FrameView / FrameUI / Presentation buckets.
//   • Constraints: OrderBefore / OrderAfter honored only within the same group.
//   • Deterministic: Kahn topological sort with lexical tie-break by type name.
//   • Lifecycle views: forward order for Initialize, reverse for Shutdown.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZenECS.Core.Config;

namespace ZenECS.Core.Systems.Internal
{
    /// <summary>
    /// Provides deterministic planning and ordering for ECS systems. Systems are
    /// grouped by phase, validated for marker/attribute consistency, and then
    /// topologically sorted within each group.
    /// </summary>
    public static class SystemPlanner
    {
        /// <summary>
        /// Immutable plan describing execution order per phase and lifecycle views.
        /// </summary>
        public sealed class Plan
        {
            /// <summary>
            /// Create a new plan with ordered sequences for all phases.
            /// </summary>
            public Plan(
                IReadOnlyList<ISystem>? fixedInput,
                IReadOnlyList<ISystem>? fixedDecision,
                IReadOnlyList<ISystem>? fixedSimulation,
                IReadOnlyList<ISystem>? fixedPost,
                IReadOnlyList<ISystem>? frameInput,
                IReadOnlyList<ISystem>? frameSync,
                IReadOnlyList<ISystem>? frameView,
                IReadOnlyList<ISystem>? frameUI,
                IReadOnlyList<ISystem>? unknown)
            {
                FixedInput      = fixedInput      ?? Array.Empty<ISystem>();
                FixedDecision   = fixedDecision   ?? Array.Empty<ISystem>();
                FixedSimulation = fixedSimulation ?? Array.Empty<ISystem>();
                FixedPost       = fixedPost       ?? Array.Empty<ISystem>();

                FrameInput      = frameInput      ?? Array.Empty<ISystem>();
                FrameSync       = frameSync       ?? Array.Empty<ISystem>();
                FrameView       = frameView       ?? Array.Empty<ISystem>();
                FrameUI         = frameUI         ?? Array.Empty<ISystem>();
                Unknown         = unknown         ?? Array.Empty<ISystem>();
            }

            // ───── Fixed-step deterministic groups ─────

            /// <summary>Execution order for the <see cref="SystemGroup.FixedInput"/> phase.</summary>
            public IReadOnlyList<ISystem> FixedInput { get; }

            /// <summary>Execution order for the <see cref="SystemGroup.FixedDecision"/> phase.</summary>
            public IReadOnlyList<ISystem> FixedDecision { get; }

            /// <summary>Execution order for the <see cref="SystemGroup.FixedSimulation"/> phase.</summary>
            public IReadOnlyList<ISystem> FixedSimulation { get; }

            /// <summary>Execution order for the <see cref="SystemGroup.FixedPost"/> phase.</summary>
            public IReadOnlyList<ISystem> FixedPost { get; }

            // ───── Variable-step frame groups ─────

            /// <summary>Execution order for the <see cref="SystemGroup.FrameInput"/> phase.</summary>
            public IReadOnlyList<ISystem> FrameInput { get; }
            public IReadOnlyList<ISystem> FrameSync { get; }
            public IReadOnlyList<ISystem> FrameView { get; }
            public IReadOnlyList<ISystem> FrameUI { get; }
            public IReadOnlyList<ISystem> Unknown { get; }

            /// <summary>
            /// Combined forward execution order across all phases.
            /// <para>
            /// Order matches the typical runner pipeline:
            /// FrameInput → FrameSync → FixedInput → FixedDecision → FixedSimulation
            /// → FixedPost → FrameView → FrameUI
            /// </para>
            /// </summary>
            public IEnumerable<ISystem> AllInExecutionOrder =>
                FrameInput
                    .Concat(FrameSync)
                    .Concat(FrameView)
                    .Concat(FrameUI)
                    .Concat(FixedInput)
                    .Concat(FixedDecision)
                    .Concat(FixedSimulation)
                    .Concat(FixedPost);

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
        /// <param name="w">The world instance to use for system planning.</param>
        /// <param name="systems">System instances to analyze; may be <c>null</c>.</param>
        /// <returns>A <see cref="Plan"/> or <c>null</c> if <paramref name="systems"/> is <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when conflicting markers/attributes exist, or a cycle is detected inside a group.
        /// </exception>
        public static Plan? Build(IWorld w, IEnumerable<ISystem>? systems)
        {
            if (systems == null) return null;

            var buckets = new Dictionary<SystemGroup, List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)>>()
            {
                [SystemGroup.Unknown]         = new(),
                [SystemGroup.FixedInput]      = new(),
                [SystemGroup.FixedDecision]   = new(),
                [SystemGroup.FixedSimulation] = new(),
                [SystemGroup.FixedPost]       = new(),

                [SystemGroup.FrameInput]      = new(),
                [SystemGroup.FrameSync]       = new(),
                [SystemGroup.FrameView]       = new(),
                [SystemGroup.FrameUI]         = new(),
            };

            // 1) Classification + constraint collection
            foreach (ISystem s in systems)
            {
                Type t = s.GetType();

                // Validate markers and attributes (no conflicting phase markers, no multi-group attributes)
                ValidatePhaseMarkers(t);

                SystemGroup group = SystemUtil.ResolveGroup(t);

                var before = t.GetCustomAttributes<OrderBeforeAttribute>()
                              .Select(a => a.Target)
                              .ToHashSet();
                var after  = t.GetCustomAttributes<OrderAfterAttribute>()
                              .Select(a => a.Target)
                              .ToHashSet();

                if (!buckets.TryGetValue(group, out var list))
                {
                    list = new List<(Type, ISystem, HashSet<Type>, HashSet<Type>)>();
                    buckets[group] = list;
                }

                // Emit a warning if this is still an Unknown group.
                if (group == SystemGroup.Unknown)
                {
                    EcsRuntimeOptions.Log.Warn($"[ZenECS][SystemPlanner] System ‘{t.FullName}’ belongs to the Unknown group and will not be executed. Please assign a valid SystemGroup attribute.");
                }
                
                list.Add((t, s, before, after));
            }

            // 2) Sort each group independently
            List<ISystem> unknown         = TopoSortWithinGroup(buckets[SystemGroup.Unknown]);
            List<ISystem> fixedInput      = TopoSortWithinGroup(buckets[SystemGroup.FixedInput]);
            List<ISystem> fixedDecision   = TopoSortWithinGroup(buckets[SystemGroup.FixedDecision]);
            List<ISystem> fixedSimulation = TopoSortWithinGroup(buckets[SystemGroup.FixedSimulation]);
            List<ISystem> fixedPost       = TopoSortWithinGroup(buckets[SystemGroup.FixedPost]);

            List<ISystem> frameInput      = TopoSortWithinGroup(buckets[SystemGroup.FrameInput]);
            List<ISystem> frameSync       = TopoSortWithinGroup(buckets[SystemGroup.FrameSync]);
            List<ISystem> frameView       = TopoSortWithinGroup(buckets[SystemGroup.FrameView]);
            List<ISystem> frameUI         = TopoSortWithinGroup(buckets[SystemGroup.FrameUI]);

            return new Plan(
                fixedInput,
                fixedDecision,
                fixedSimulation,
                fixedPost,
                frameInput,
                frameSync,
                frameView,
                frameUI,
                unknown);
        }

        /// <summary>
        /// Validate that a system type has consistent phase markers and group attributes.
        /// </summary>
        /// <param name="t">System type.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if multiple markers or conflicting group attributes exist.
        /// </exception>
        private static void ValidatePhaseMarkers(Type t)
        {
            // A system must not declare more than one SimulationGroupAttribute
            // (including derived attributes like FixedInputGroupAttribute, etc.).
            int groupAttrCount = t.GetCustomAttributes<GroupAttribute>(false).Count();
            if (groupAttrCount > 1)
                throw new InvalidOperationException($"{t.Name} has multiple SimulationGroup-derived group attributes.");

            // We intentionally do NOT enforce a strict mapping between marker interfaces
            // and specific SystemGroup values here, because some markers (e.g. IFixedSetupSystem)
            // may be valid for multiple groups (FixedInput, FixedDecision). The actual
            // mapping is handled by SystemUtil.ResolveGroup(..) using attributes + markers.
        }

        /// <summary>
        /// Topologically sort one group (Kahn's algorithm). Cross-group edges are ignored.
        /// </summary>
        /// <param name="list">System nodes with within-group Before/After constraints.</param>
        /// <returns>Deterministically ordered systems for that group.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a cycle is detected.</exception>
        /// <remarks>
        /// Tie-breaker uses the type's full name for deterministic ordering.
        /// </remarks>
        private static List<ISystem> TopoSortWithinGroup(
            List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)> list)
        {
            if (list.Count == 0)
                return new List<ISystem>();

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

                // this -> target  (this must run before target)
                foreach (Type target in before)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        EcsRuntimeOptions.Log.Warn(
                            $"[OrderBefore] {type.Name} → {target.Name} ignored (not in same group or not present in world)");
                        continue;
                    }
                    if (edges[type].Add(target))
                        indeg[target] = indeg.GetValueOrDefault(target) + 1;
                }

                // target -> this  (this must run after target)
                foreach (Type target in after)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        EcsRuntimeOptions.Log.Warn(
                            $"[OrderAfter] {type.Name} ← {target.Name} ignored (not in same group or system not added in the world)");
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
