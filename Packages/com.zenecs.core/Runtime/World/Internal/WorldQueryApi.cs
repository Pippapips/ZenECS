// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Query API)
// File: WorldQueryApi.cs
// Purpose: Ref-based component iteration with seed-from-smallest optimization.
// Key concepts:
//   • Type-safe builders: Query<T1..T8>(Filter) produce struct enumerables.
//   • ResolvedFilter: pre-resolves component pools for fast membership tests.
//   • Smallest-pool seeding: minimizes scan set before applying filters.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldQueryApi"/> – strongly typed query entry points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <c>Query&lt;…&gt;(Filter)</c> method builds a small context struct
    /// (for example <c>QueryCtx3&lt;T1, T2, T3&gt;</c>) that captures references
    /// to the relevant component pools plus a pre-resolved <c>Filter</c>
    /// (<c>ResolvedFilter</c>), and returns a <c>QueryEnumerable&lt;…&gt;</c>.
    /// </para>
    /// <para>
    /// The enumerable chooses a seed pool (typically the smallest) to minimize
    /// the scan set before applying the remaining pool membership tests and
    /// composable filter constraints, keeping iteration efficient.
    /// </para>
    /// </remarks>
    internal sealed partial class World : IWorldQueryApi
    {
        /// <summary>
        /// Builds a query over entities that have component <typeparamref name="T1"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all). When left as
        /// <c>default</c>, only the presence of <typeparamref name="T1"/> is
        /// considered.
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1}"/> value-type enumerable bound to
        /// this world.
        /// </returns>
        public QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx1<T1>(a, rf);
            return new QueryEnumerable<T1>(this, in ctx);
        }

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2}"/> value-type enumerable.
        /// </returns>
        public QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx2<T1, T2>(a, b, rf);
            return new QueryEnumerable<T1, T2>(this, in ctx);
        }

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/>, <typeparamref name="T2"/>, and
        /// <typeparamref name="T3"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3}"/> value-type enumerable.
        /// </returns>
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

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T4"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3, T4}"/> value-type enumerable.
        /// </returns>
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

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T5"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3, T4, T5}"/> value-type enumerable.
        /// </returns>
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

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T6"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6}"/> value-type enumerable.
        /// </returns>
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

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T7"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <typeparam name="T7">Seventh required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6, T7}"/> value-type enumerable.
        /// </returns>
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

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T8"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <typeparam name="T7">Seventh required component type.</typeparam>
        /// <typeparam name="T8">Eighth required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter (with/without any/all).
        /// </param>
        /// <returns>
        /// A <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6, T7, T8}"/>
        /// value-type enumerable.
        /// </returns>
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
