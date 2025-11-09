// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Query API
// File: IWorldQueryApi.cs
// Purpose: Allocation-free, type-safe entity queries with composable filters.
// Key concepts:
//   • Value-type enumerables: foreach-friendly without GC.
//   • Filter buckets: WithAll/WithoutAll + WithAny/WithoutAny.
//   • Arity up to 8: T1…T8 entry points for common tuples.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core
{
    /// <summary>
    /// Read-only entity query surface. Returns allocation-free value-type enumerables
    /// that can be used directly in <c>foreach</c>, e.g.:
    /// <code>foreach (var e in world.Query&lt;Position, Rotation, Scale&gt;()) { ... }</code>
    /// </summary>
    public interface IWorldQueryApi
    {
        /// <summary>Query entities having <typeparamref name="T1"/>.</summary>
        QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/>, <typeparamref name="T2"/>.</summary>
        QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>.</summary>
        QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T4"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T5"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T6"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T7"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T8"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct;
    }

    // Notes:
    // • QueryEnumerable<…> and Filter are public value-type helpers defined in runtime.
    // • Passing default filter means “no extra constraints”.
}
