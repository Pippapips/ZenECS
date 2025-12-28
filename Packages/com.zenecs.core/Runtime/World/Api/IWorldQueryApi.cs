// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Query API
// File: IWorldQueryApi.cs
// Purpose: Allocation-free, type-safe entity queries with composable filters.
// Key concepts:
//   • Value-type enumerables: foreach-friendly without GC.
//   • Filter buckets: WithAll/WithoutAll + WithAny/WithoutAny.
//   • Arity up to 8: T1…T8 entry points for common tuples.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core
{
    /// <summary>
    /// Read-only entity query surface that returns allocation-free value-type
    /// enumerables consumable directly in <c>foreach</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queries are strongly typed on the component tuple they require
    /// (T1 through T8). Each
    /// <c>Query&lt;…&gt;(Filter)</c> call builds a <c>QueryEnumerable&lt;…&gt;</c>
    /// that can be iterated without allocating managed enumerators.
    /// </para>
    /// <para>
    /// The optional <see cref="Filter"/> parameter allows additional constraints
    /// to be expressed (for example, "with any of", "without all", etc.) beyond
    /// the base component set described by the generic parameters.
    /// </para>
    /// </remarks>
    public interface IWorldQueryApi
    {
        /// <summary>
        /// Builds a query over entities that have component <typeparamref name="T1"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of <typeparamref name="T1"/> is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1}"/> suitable for use in
        /// <c>foreach</c> loops without allocations.
        /// </returns>
        QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct;

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of <typeparamref name="T1"/> and <typeparamref name="T2"/>
        /// is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2}"/> for iteration.
        /// </returns>
        QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct;

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/>, <typeparamref name="T2"/>,
        /// and <typeparamref name="T3"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the three components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3}"/> for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct;

        /// <summary>
        /// Builds a query over entities that have components
        /// <typeparamref name="T1"/> through <typeparamref name="T4"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <param name="f">
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the four components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3, T4}"/> for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

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
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the five components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3, T4, T5}"/> for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

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
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the six components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6}"/> for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

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
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the seven components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6, T7}"/>
        /// for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct;

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
        /// Optional composable filter. When left as <c>default</c>, only the
        /// presence of the eight components is considered.
        /// </param>
        /// <returns>
        /// A value-type <see cref="QueryEnumerable{T1, T2, T3, T4, T5, T6, T7, T8}"/>
        /// for iteration.
        /// </returns>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct;
    }

    // Notes:
    // • QueryEnumerable<…> and Filter are public value-type helpers defined in runtime.
    // • Passing default filter means “no extra constraints” beyond required components.
}
