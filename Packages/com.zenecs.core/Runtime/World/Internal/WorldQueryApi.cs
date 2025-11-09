// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Query API)
// File: WorldQueryApi.cs
// Purpose: Ref-based component iteration with seed-from-smallest optimization.
// Key concepts:
//   • Type-safe builders: Query<T1..T8>(Filter) produce struct enumerables.
//   • ResolvedFilter: pre-resolves component pools for fast membership tests.
//   • Smallest-pool seeding: minimizes scan set before applying filters.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldQueryApi"/> – strongly typed query entry points.
    /// </summary>
    internal sealed partial class World : IWorldQueryApi
    {
        /// <summary>
        /// Build a query over entities that have <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="f">Optional composable filter (with/without any/all).</param>
        public QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx1<T1>(a, rf);
            return new QueryEnumerable<T1>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1 and T2.</summary>
        public QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx2<T1, T2>(a, b, rf);
            return new QueryEnumerable<T1, T2>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1, T2 and T3.</summary>
        public QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx3<T1, T2, T3>(a, b, c, rf);
            return new QueryEnumerable<T1, T2, T3>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1…T4.</summary>
        public QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var d  = _componentPoolRepository.TryGetPool<T4>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx4<T1, T2, T3, T4>(a, b, c, d, rf);
            return new QueryEnumerable<T1, T2, T3, T4>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1…T5.</summary>
        public QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var d  = _componentPoolRepository.TryGetPool<T4>();
            var e  = _componentPoolRepository.TryGetPool<T5>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx5<T1, T2, T3, T4, T5>(a, b, c, d, e, rf);
            return new QueryEnumerable<T1, T2, T3, T4, T5>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1…T6.</summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var d  = _componentPoolRepository.TryGetPool<T4>();
            var e  = _componentPoolRepository.TryGetPool<T5>();
            var f6 = _componentPoolRepository.TryGetPool<T6>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx6<T1, T2, T3, T4, T5, T6>(a, b, c, d, e, f6, rf);
            return new QueryEnumerable<T1, T2, T3, T4, T5, T6>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1…T7.</summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var d  = _componentPoolRepository.TryGetPool<T4>();
            var e  = _componentPoolRepository.TryGetPool<T5>();
            var f6 = _componentPoolRepository.TryGetPool<T6>();
            var g  = _componentPoolRepository.TryGetPool<T7>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx7<T1, T2, T3, T4, T5, T6, T7>(a, b, c, d, e, f6, g, rf);
            return new QueryEnumerable<T1, T2, T3, T4, T5, T6, T7>(this, in ctx);
        }

        /// <summary>Build a query over entities that have T1…T8.</summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct where T8 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var c  = _componentPoolRepository.TryGetPool<T3>();
            var d  = _componentPoolRepository.TryGetPool<T4>();
            var e  = _componentPoolRepository.TryGetPool<T5>();
            var f6 = _componentPoolRepository.TryGetPool<T6>();
            var g  = _componentPoolRepository.TryGetPool<T7>();
            var h  = _componentPoolRepository.TryGetPool<T8>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8>(a, b, c, d, e, f6, g, h, rf);
            return new QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>(this, in ctx);
        }
    }
}
