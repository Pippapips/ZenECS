﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Query.Filter.cs
// Purpose: Composable filter definitions for queries (include/exclude component sets).
// Key concepts:
//   • WithAny / WithoutAny fluent API for logical OR groups.
//   • Used by Query to test entity membership efficiently.
//   • Cached per filter key for reuse.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.ComponentPooling;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World
    {
        /*
         Example:
         var f = World.Filter.New
             .With<Owner>()
             .Without<DeadTag>()
             .WithAny(typeof(Burning), typeof(Poisoned))   // Match if any of these exist
             .WithoutAny(typeof(Shielded), typeof(Invuln)) // Exclude if any of these exist
             .Build();

         foreach (var e in world.Query<Position, Velocity>(f))
         {
             ref var p = ref world.RefExisting<Position>(e);
             var  v    =  world.RefExisting<Velocity>(e);
             p.Value += v.Value * world.DeltaTime;
         }
        */

        /// <summary>
        /// Resolved, pool-level representation of a filter used by the query engine.
        /// </summary>
        internal sealed class ResolvedFilter
        {
            /// <summary>Pools that must all be present on the entity.</summary>
            public IComponentPool[] withAll = Array.Empty<IComponentPool>();
            /// <summary>Pools that must all be absent from the entity.</summary>
            public IComponentPool[] withoutAll = Array.Empty<IComponentPool>();
            /// <summary>
            /// Buckets where at least one pool in each bucket must be present (logical OR per bucket, AND across buckets).
            /// </summary>
            public IComponentPool[][] withAny = Array.Empty<IComponentPool[]>();
            /// <summary>
            /// Buckets where any present pool in a bucket causes exclusion (logical OR per bucket, AND across buckets).
            /// </summary>
            public IComponentPool[][] withoutAny = Array.Empty<IComponentPool[]>();
        }
        
        /// <summary>
        /// Clears all cached query filters and their resolved component pool masks.
        /// </summary>
        /// <remarks>
        /// Call this when component pools are rebuilt or the world is reset in a way that invalidates cached lookups.
        /// </remarks>
        private void ResetQueryCaches()
        {
            filterCache?.Clear();
        }

        // ---------- Filter Key / Cache ----------
        /// <summary>
        /// Order-independent cache key for a composed filter.
        /// </summary>
        internal struct FilterKey : System.IEquatable<FilterKey>
        {
            /// <summary>Precomputed hash value representing the filter.</summary>
            public readonly ulong Hash;

            /// <summary>Creates a new <see cref="FilterKey"/> with the given <paramref name="hash"/>.</summary>
            public FilterKey(ulong hash) { Hash = hash; }

            /// <inheritdoc/>
            public bool Equals(FilterKey other) => Hash == other.Hash;
            /// <inheritdoc/>
            public override bool Equals(object? obj) => obj is FilterKey fk && fk.Hash == Hash;
            /// <inheritdoc/>
            public override int GetHashCode() => Hash.GetHashCode();
        }


        /// <summary>
        /// Filter cache keyed by <see cref="FilterKey"/> (computed from included/excluded component types).
        /// </summary>
        private readonly ConcurrentDictionary<FilterKey, ResolvedFilter> filterCache = new();

        /// <summary>
        /// Computes an order-independent key for the given filter, suitable for caching.
        /// </summary>
        /// <param name="f">The filter to hash.</param>
        /// <returns>A stable <see cref="FilterKey"/>.</returns>
        internal static FilterKey MakeKey(in Filter f)
        {
            unchecked
            {
                ulong h = 1469598103934665603ul; // FNV offset
                void Mix(Type t)
                {
                    h ^= (ulong)t.GetHashCode();
                    h *= 1099511628211ul;
                }
                void MixTypeSet(Type[] arr)
                {
                    if (arr == null) return;
                    foreach (var t in arr.OrderBy(x => x.FullName)) Mix(t);
                    h ^= 0x9E3779B185EBCA87ul;
                }
                void MixBuckets(Type[][] buckets)
                {
                    if (buckets == null) return;
                    foreach (var set in buckets)
                    {
                        MixTypeSet(set);
                        h ^= 0xC2B2AE3D27D4EB4Ful;
                    }
                }

                MixTypeSet(f.withAll);
                MixTypeSet(f.withoutAll);
                MixBuckets(f.withAny);
                MixBuckets(f.withoutAny);
                return new FilterKey(h);
            }
        }

        /// <summary>
        /// Resolves a high-level <see cref="Filter"/> to concrete component pool references and caches the result.
        /// </summary>
        /// <param name="f">The filter to resolve.</param>
        /// <returns>A <see cref="ResolvedFilter"/> ready for fast evaluation.</returns>
        /// <remarks>
        /// If a referenced component type does not have a registered pool, the corresponding section becomes empty,
        /// which may cause the filter to match no entities (for <c>With</c>/<c>WithAny</c>) or to skip checks.
        /// </remarks>
        internal ResolvedFilter ResolveFilter(in Filter f)
        {
            var key = MakeKey(f);
            if (filterCache.TryGetValue(key, out var cached)) return cached;

            IComponentPool[]? ToPools(Type[] types)
            {
                if (types == null || types.Length == 0) return Array.Empty<IComponentPool>();
                var arr = new IComponentPool[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    var p = _componentPoolRepository.GetPool(types[i]);
                    if (p == null) return null;
                    arr[i] = p;
                }
                return arr;
            }
            IComponentPool[][]? ToPoolBuckets(Type[][]? buckets)
            {
                if (buckets == null || buckets.Length == 0) return Array.Empty<IComponentPool[]>();
                var arr = new IComponentPool[buckets.Length][];
                for (int i = 0; i < buckets.Length; i++)
                {
                    var tset = buckets[i];
                    var ps = new IComponentPool[tset.Length];
                    for (int j = 0; j < tset.Length; j++)
                    {
                        var p = _componentPoolRepository.GetPool(tset[j]);
                        if (p == null) return null;
                        ps[j] = p;
                    }
                    arr[i] = ps;
                }
                return arr;
            }

            var rf = new ResolvedFilter
            {
                withAll = ToPools(f.withAll) ?? Array.Empty<IComponentPool>(),
                withoutAll = ToPools(f.withoutAll) ?? Array.Empty<IComponentPool>(),
                withAny = ToPoolBuckets(f.withAny) ?? Array.Empty<IComponentPool[]>(),
                withoutAny = ToPoolBuckets(f.withoutAny) ?? Array.Empty<IComponentPool[]>(),
            };
            if (rf.withAll == null)
            {
                rf.withAll = Array.Empty<IComponentPool>();
                rf.withAny = Array.Empty<IComponentPool[]>();
            }

            filterCache[key] = rf;
            return rf;
        }

        /// <summary>
        /// Tests whether the entity identified by <paramref name="id"/> satisfies the conditions of a resolved filter.
        /// </summary>
        /// <param name="id">Internal entity id (index into component pools).</param>
        /// <param name="r">Resolved filter to evaluate.</param>
        /// <returns><see langword="true"/> if the entity matches; otherwise <see langword="false"/>.</returns>
        internal static bool MeetsFilter(int id, in ResolvedFilter r)
        {
            // WithAll: must contain all
            var wa = r.withAll;
            for (int i = 0; i < wa.Length; i++)
                if (!wa[i].Has(id))
                    return false;

            // WithoutAll: must not contain any
            var wo = r.withoutAll;
            for (int i = 0; i < wo.Length; i++)
                if (wo[i].Has(id))
                    return false;

            // WithAny: must contain at least one from each bucket
            var wan = r.withAny;
            for (int b = 0; b < wan.Length; b++)
            {
                var bucket = wan[b];
                bool any = false;
                for (int i = 0; i < bucket.Length; i++)
                    if (bucket[i] != null && bucket[i].Has(id))
                    {
                        any = true;
                        break;
                    }
                if (!any) return false;
            }

            // WithoutAny: fails if any from each bucket exist
            var won = r.withoutAny;
            for (int b = 0; b < won.Length; b++)
            {
                var bucket = won[b];
                for (int i = 0; i < bucket.Length; i++)
                    if (bucket[i] != null && bucket[i].Has(id))
                        return false;
            }

            return true;
        }
    }
}
