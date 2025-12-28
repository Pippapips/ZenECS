// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Query Filter API)
// File: WorldQueryFilterApi.cs
// Purpose: Compose, resolve, and cache query filters against component pools.
// Key concepts:
//   • WithAll/WithoutAll & WithAny/WithoutAny bucket semantics (AND of ORs).
//   • Order-independent FilterKey hashing for cache lookups.
//   • ResolvedFilter stores pool arrays for branchless membership tests.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using ZenECS.Core.ComponentPooling.Internal;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World
    {
        /// <summary>
        /// Pool-level representation of a filter used by the query engine.
        /// </summary>
        internal sealed class ResolvedFilter
        {
            /// <summary>Pools that must all be present on the entity.</summary>
            public IComponentPool[] withAll = Array.Empty<IComponentPool>();
            /// <summary>Pools that must all be absent from the entity.</summary>
            public IComponentPool[] withoutAll = Array.Empty<IComponentPool>();
            /// <summary>At least one pool in each bucket must be present (OR per bucket, AND across buckets).</summary>
            public IComponentPool[][] withAny = Array.Empty<IComponentPool[]>();
            /// <summary>Any present pool in a bucket causes exclusion (OR per bucket, AND across buckets).</summary>
            public IComponentPool[][] withoutAny = Array.Empty<IComponentPool[]>();
        }

        /// <summary>
        /// Clear cached filter resolutions (call after pool rebuilds/resets).
        /// </summary>
        private void ResetQueryCaches()
        {
            filterCache?.Clear();
        }

        /// <summary>
        /// Order-independent cache key for a composed filter.
        /// </summary>
        internal struct FilterKey : IEquatable<FilterKey>
        {
            /// <summary>Precomputed hash value representing the filter.</summary>
            public readonly ulong Hash;

            /// <summary>Create a key from a hash.</summary>
            public FilterKey(ulong hash) { Hash = hash; }

            /// <inheritdoc/>
            public bool Equals(FilterKey other) => Hash == other.Hash;
            /// <inheritdoc/>
            public override bool Equals(object? obj) => obj is FilterKey fk && fk.Hash == Hash;
            /// <inheritdoc/>
            public override int GetHashCode() => Hash.GetHashCode();
        }

        /// <summary>
        /// Cache of resolved filters keyed by <see cref="FilterKey"/>.
        /// </summary>
        private readonly ConcurrentDictionary<FilterKey, ResolvedFilter> filterCache = new();

        /// <summary>
        /// Compute an order-independent key for the given filter.
        /// </summary>
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
        /// Resolve a high-level <see cref="Filter"/> to concrete pool references and cache it.
        /// </summary>
        /// <param name="f">Filter to resolve.</param>
        /// <returns>Resolved filter ready for fast evaluation.</returns>
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
                    var p = _componentPoolRepository.GetOrCreatePoolByType(types[i]);
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
                        var p = _componentPoolRepository.GetOrCreatePoolByType(tset[j]);
                        if (p == null) return null;
                        ps[j] = p;
                    }
                    arr[i] = ps;
                }
                return arr;
            }

            var withAll    = ToPools(f.withAll);
            var withoutAll = ToPools(f.withoutAll);
            var withAny    = ToPoolBuckets(f.withAny);
            var withoutAny = ToPoolBuckets(f.withoutAny);

            var rf = new ResolvedFilter
            {
                withAll    = withAll    ?? Array.Empty<IComponentPool>(),
                withoutAll = withoutAll ?? Array.Empty<IComponentPool>(),
                withAny    = withAny    ?? Array.Empty<IComponentPool[]>(),
                withoutAny = withoutAny ?? Array.Empty<IComponentPool[]>(),
            };

            // Do not cache if any referenced component type does not yet have a pool.
            if (withAll == null || withoutAll == null || withAny == null || withoutAny == null)
                return rf;

            filterCache[key] = rf;
            return rf;
        }

        /// <summary>
        /// Test whether the entity with internal id <paramref name="id"/> satisfies <paramref name="r"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the entity matches; otherwise <see langword="false"/>.</returns>
        internal static bool MeetsFilter(int id, in ResolvedFilter r)
        {
            var wa = r.withAll;
            for (int i = 0; i < wa.Length; i++)
                if (!wa[i].Has(id))
                    return false;

            var wo = r.withoutAll;
            for (int i = 0; i < wo.Length; i++)
                if (wo[i].Has(id))
                    return false;

            var wan = r.withAny;
            for (int b = 0; b < wan.Length; b++)
            {
                var bucket = wan[b];
                bool any = false;
                for (int i = 0; i < bucket.Length; i++)
                    if (bucket[i] != null && bucket[i].Has(id)) { any = true; break; }
                if (!any) return false;
            }

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
