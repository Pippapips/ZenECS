// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Query Enumerables
// File: QueryEnumerables.cs
// Purpose: Define strongly-typed enumerables for component queries, and the
//          filter DSL used to constrain entity iteration.
// Key concepts:
//   • Fluent immutable Filter builder (With / Without / WithAny / WithoutAny)
//   • QueryEnumerable<T1..T8>: zero-allocation foreach enumerators
//   • QuerySeed: chooses smallest pool as iteration seed for efficiency
//   • MeetsFilter: per-entity evaluation via World.ResolvedFilter
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Internal;
using ZenECS.Core.ComponentPooling.Internal;

namespace ZenECS.Core
{
    // --------------------------------------------------------------------------
    // FILTER DSL
    // --------------------------------------------------------------------------

    /// <summary>
    /// Immutable value describing a query filter with include/exclude constraints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filters are created via the fluent <see cref="Builder"/> exposed by
    /// <see cref="New"/>. Use them to refine queries passed into
    /// <c>world.Query&lt;...&gt;(filter)</c>.
    /// </para>
    /// </remarks>
    public readonly struct Filter
    {
        internal readonly Type[] withAll;
        internal readonly Type[] withoutAll;
        internal readonly Type[][] withAny;
        internal readonly Type[][] withoutAny;

        internal Filter(Type[] wa, Type[] wo, Type[][] wan, Type[][] won)
        {
            withAll = wa;
            withoutAll = wo;
            withAny = wan;
            withoutAny = won;
        }

        /// <summary>
        /// Entry point for creating a new fluent <see cref="Builder"/>.
        /// </summary>
        public static Builder New => default;

        /// <summary>
        /// Fluent, immutable builder used to compose <see cref="Filter"/> instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each method returns a new builder that includes the requested constraint;
        /// the original builder remains unchanged.
        /// </para>
        /// </remarks>
        public readonly struct Builder
        {
            private readonly List<Type>? wa;
            private readonly List<Type>? wo;
            private readonly List<List<Type>>? wan;
            private readonly List<List<Type>>? won;

            private Builder(List<Type>? wa, List<Type>? wo, List<List<Type>>? wan, List<List<Type>>? won)
            {
                this.wa = wa;
                this.wo = wo;
                this.wan = wan;
                this.won = won;
            }

            /// <summary>
            /// Requires that entities include component <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">Component type that must be present.</typeparam>
            /// <returns>A new builder that includes this constraint.</returns>
            public Builder With<T>() where T : struct =>
                new(Append(wa, typeof(T)), wo, wan, won);

            /// <summary>
            /// Requires that entities exclude component <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">Component type that must be absent.</typeparam>
            /// <returns>A new builder that includes this constraint.</returns>
            public Builder Without<T>() where T : struct =>
                new(wa, Append(wo, typeof(T)), wan, won);

            /// <summary>
            /// Adds an OR group: entity passes if it contains <em>any one</em> of these types.
            /// </summary>
            /// <param name="types">Component types that form the OR group.</param>
            /// <returns>A new builder that includes this OR-group constraint.</returns>
            public Builder WithAny(params Type[] types) =>
                new(wa, wo, AppendBucket(wan, types), won);

            /// <summary>
            /// Adds a NOT-OR group: entity fails if it contains <em>any one</em> of these types.
            /// </summary>
            /// <param name="types">Component types that form the NOT-OR group.</param>
            /// <returns>A new builder that includes this NOT-OR-group constraint.</returns>
            public Builder WithoutAny(params Type[] types) =>
                new(wa, wo, wan, AppendBucket(won, types));

            /// <summary>
            /// Finalizes this builder into an immutable <see cref="Filter"/>.
            /// </summary>
            /// <returns>A fully constructed <see cref="Filter"/>.</returns>
            public Filter Build() =>
                new Filter(
                    wa?.ToArray() ?? Array.Empty<Type>(),
                    wo?.ToArray() ?? Array.Empty<Type>(),
                    ToJagged(wan),
                    ToJagged(won));

            private static List<Type> Append(List<Type>? list, Type t)
            {
                var l = list ?? new List<Type>(4);
                l.Add(t);
                return l;
            }

            private static List<List<Type>> AppendBucket(List<List<Type>>? list, Type[] types)
            {
                var l = list ?? new List<List<Type>>(2);
                var bucket = new List<Type>(types.Length);
                foreach (var t in types)
                {
                    if (t != null)
                        bucket.Add(t);
                }

                if (bucket.Count > 0)
                    l.Add(bucket);

                return l;
            }

            private static Type[][] ToJagged(List<List<Type>>? src)
            {
                if (src == null || src.Count == 0)
                    return Array.Empty<Type[]>();

                var arr = new Type[src.Count][];
                for (int i = 0; i < src.Count; i++)
                    arr[i] = src[i].ToArray();

                return arr;
            }
        }
    }

    // --------------------------------------------------------------------------
    // QUERY SEED PICKER
    // --------------------------------------------------------------------------

    /// <summary>
    /// Helper for selecting the smallest component pool as the iteration seed.
    /// </summary>
    internal static class QuerySeed
    {
        /// <summary>
        /// Chooses the smallest-capacity component pool as the seed for iteration.
        /// Null pools are ignored.
        /// </summary>
        /// <param name="pools">Candidate pools for seeding iteration.</param>
        /// <returns>
        /// The pool with the smallest capacity, or <see langword="null"/> if all are
        /// <see langword="null"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IComponentPool? Pick(params IComponentPool?[] pools)
        {
            IComponentPool? best = null;
            int bestCap = int.MaxValue;

            for (int i = 0; i < pools.Length; i++)
            {
                var p = pools[i];
                if (p == null) continue;

                var cap = p.Capacity;
                if (cap < bestCap)
                {
                    best = p;
                    bestCap = cap;
                }
            }

            return best;
        }
    }

    // --------------------------------------------------------------------------
    // GENERIC QUERY ENUMERABLES
    // --------------------------------------------------------------------------
    //
    // Each QueryEnumerable<T1..Tn> implements a zero-allocation foreach pattern:
    //
    //   foreach (var e in world.Query<T1, T2>(filter)) { ... }
    //
    // Enumerator logic:
    //   • Picks smallest seed pool for iteration
    //   • Checks Has(id) across all component pools
    //   • Validates entity against resolved filter
    //   • Returns living entity handles (id, generation)
    //
    // For multi-component queries (T2..T8) we enforce that all requested
    // component pools must exist. If any pool is missing, the query is treated
    // as empty. This prevents "accidentally relaxed" queries where a missing
    // pool would otherwise be interpreted as "no requirement".
    //
    // --------------------------------------------------------------------------

    #region T1

    /// <summary>
    /// Internal context for single-component queries.
    /// </summary>
    internal readonly struct QueryCtx1<T1> where T1 : struct
    {
        /// <summary>Primary component pool.</summary>
        public readonly IComponentPool? A;

        /// <summary>Resolved filter instance for fast evaluation.</summary>
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for single-component queries.
        /// </summary>
        public QueryCtx1(IComponentPool? a, World.ResolvedFilter rf)
        {
            A  = a;
            RF = rf;
        }
    }

    /// <summary>
    /// Single-component query enumerable.
    /// Each item represents an entity and its <typeparamref name="T1"/> component.
    /// </summary>
    /// <typeparam name="T1">Component type to query for.</typeparam>
    /// <remarks>
    /// <para>
    /// Typical usage:
    /// </para>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1) in world.Query<T1>())
    /// {
    ///     // Use e and c1
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1> where T1 : struct
    {
        private readonly World _w;
        private readonly QueryCtx1<T1> _ctx;

        internal QueryEnumerable(World w, in QueryCtx1<T1> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Single-item result for <see cref="QueryEnumerable{T1}"/>.
        /// Supports tuple deconstruction: <c>var (e, c1) = item;</c>.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>The entity handle.</summary>
            public readonly Entity Entity;

            /// <summary>The component value for <typeparamref name="T1"/>.</summary>
            public readonly T1 Component;

            /// <summary>
            /// Creates a new result with entity and component value.
            /// </summary>
            /// <param name="entity">Entity handle.</param>
            /// <param name="component">Component value.</param>
            public Result(in Entity entity, in T1 component)
            {
                Entity    = entity;
                Component = component;
            }

            /// <summary>
            /// Deconstructs this result into its components.
            /// </summary>
            public void Deconstruct(out Entity entity, out T1 component)
            {
                entity   = Entity;
                component = Component;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates over matching entities.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx1<T1> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _rf = ctx.RF;

                // For single-component queries, a missing pool means empty query.
                var seed = _a;
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1) pair.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Advances to the next matching entity, if any.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if another entity is available; otherwise
            /// <see langword="false"/>.
            /// </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    // Pool existence and filter check.
                    if ((_a == null || _a.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        // World guarantees component existence, so it is safe to read.
                        var value  = _w.ReadComponent<T1>(entity);
                        _cur = new Result(entity, value);
                        return true;
                    }
                }

                return false;
            }
        }
    }
    
    #endregion

    #region T1,T2

    /// <summary>
    /// Internal context for two-component queries.
    /// </summary>
    internal readonly struct QueryCtx2<T1, T2>
        where T1 : struct where T2 : struct
    {
        /// <summary>First component pool.</summary>
        public readonly IComponentPool? A;

        /// <summary>Second component pool.</summary>
        public readonly IComponentPool? B;

        /// <summary>Resolved filter instance for fast evaluation.</summary>
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for two-component queries.
        /// </summary>
        public QueryCtx2(IComponentPool? a, IComponentPool? b, World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            RF = rf;
        }
    }

    /// <summary>
    /// Two-component query enumerable.
    /// Each item represents an entity and its <typeparamref name="T1"/>
    /// and <typeparamref name="T2"/> components.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <remarks>
    /// <para>Usage:</para>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1, c2) in world.Query<T1, T2>())
    /// {
    ///     // Use e, c1, c2
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1, T2>
        where T1 : struct where T2 : struct
    {
        private readonly World _w;
        private readonly QueryCtx2<T1, T2> _ctx;

        internal QueryEnumerable(World w, in QueryCtx2<T1, T2> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Single-item result for <see cref="QueryEnumerable{T1,T2}"/>.
        /// Supports tuple deconstruction: <c>var (e, c1, c2) = item;</c>.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>The entity handle.</summary>
            public readonly Entity Entity;

            /// <summary>The first component value.</summary>
            public readonly T1 Component1;

            /// <summary>The second component value.</summary>
            public readonly T2 Component2;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(in Entity entity, in T1 c1, in T2 c2)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(out Entity entity, out T1 c1, out T2 c2)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
            }
        }

        /// <summary>
        /// Returns an enumerator over the query results.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx2<T1, T2> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _rf = ctx.RF;

                // If any required pool is missing, this query is empty.
                if (_a == null || _b == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1, T2) triple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Advances to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    // All pools are guaranteed non-null here.
                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        _cur = new Result(entity, c1, c2);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T3

    /// <summary>
    /// Internal context for three-component queries.
    /// </summary>
    internal readonly struct QueryCtx3<T1, T2, T3>
        where T1 : struct where T2 : struct where T3 : struct
    {
        public readonly IComponentPool? A, B, C;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for three-component queries.
        /// </summary>
        public QueryCtx3(IComponentPool? a, IComponentPool? b, IComponentPool? c, World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            RF = rf;
        }
    }

    /// <summary>
    /// Three-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <remarks>
    /// <para>Usage:</para>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1, c2, c3) in world.Query<T1, T2, T3>())
    /// {
    ///     // ...
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1, T2, T3>
        where T1 : struct where T2 : struct where T3 : struct
    {
        private readonly World _w;
        private readonly QueryCtx3<T1, T2, T3> _ctx;

        internal QueryEnumerable(World w, in QueryCtx3<T1, T2, T3> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for three-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(in Entity entity, in T1 c1, in T2 c2, in T3 c3)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(out Entity entity, out T1 c1, out T2 c2, out T3 c3)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx3<T1, T2, T3> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>Current (Entity, T1, T2, T3) tuple.</summary>
            public Result Current => _cur;

            /// <summary>Moves to the next result if available.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        _cur = new Result(entity, c1, c2, c3);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T4

    /// <summary>
    /// Internal context for four-component queries.
    /// </summary>
    internal readonly struct QueryCtx4<T1, T2, T3, T4>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        public readonly IComponentPool? A, B, C, D;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for four-component queries.
        /// </summary>
        public QueryCtx4(
            IComponentPool? a,
            IComponentPool? b,
            IComponentPool? c,
            IComponentPool? d,
            World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            D  = d;
            RF = rf;
        }
    }

    /// <summary>
    /// Four-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <remarks>
    /// <para>Usage:</para>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1, c2, c3, c4) in world.Query<T1, T2, T3, T4>())
    /// {
    ///     // ...
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1, T2, T3, T4>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        private readonly World _w;
        private readonly QueryCtx4<T1, T2, T3, T4> _ctx;

        internal QueryEnumerable(World w, in QueryCtx4<T1, T2, T3, T4> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for four-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;
            public readonly T4     Component4;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(in Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
                Component4 = c4;
            }

            /// <summary>
            /// Deconstructs the result into its values.
            /// </summary>
            public void Deconstruct(out Entity entity, out T1 c1, out T2 c2, out T3 c3, out T4 c4)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
                c4     = Component4;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3,T4}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx4<T1, T2, T3, T4> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _d  = ctx.D;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null || _d == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c, _d);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1..T4) tuple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Moves to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        _d!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        var c4     = _w.ReadComponent<T4>(entity);
                        _cur = new Result(entity, c1, c2, c3, c4);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T5

    /// <summary>
    /// Internal context for five-component queries.
    /// </summary>
    internal readonly struct QueryCtx5<T1, T2, T3, T4, T5>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        public readonly IComponentPool? A, B, C, D, E;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for five-component queries.
        /// </summary>
        public QueryCtx5(
            IComponentPool? a,
            IComponentPool? b,
            IComponentPool? c,
            IComponentPool? d,
            IComponentPool? e,
            World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            D  = d;
            E  = e;
            RF = rf;
        }
    }

    /// <summary>
    /// Five-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <typeparam name="T5">Fifth component type.</typeparam>
    /// <remarks>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1, c2, c3, c4, c5) in world.Query<T1, T2, T3, T4, T5>())
    /// {
    ///     // ...
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        private readonly World _w;
        private readonly QueryCtx5<T1, T2, T3, T4, T5> _ctx;

        internal QueryEnumerable(World w, in QueryCtx5<T1, T2, T3, T4, T5> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for five-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;
            public readonly T4     Component4;
            public readonly T5     Component5;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(in Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
                Component4 = c4;
                Component5 = c5;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(out Entity entity, out T1 c1, out T2 c2, out T3 c3, out T4 c4, out T5 c5)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
                c4     = Component4;
                c5     = Component5;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3,T4,T5}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx5<T1, T2, T3, T4, T5> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _d  = ctx.D;
                _e  = ctx.E;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null || _d == null || _e == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1..T5) tuple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Moves to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        _d!.Has(id) &&
                        _e!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        var c4     = _w.ReadComponent<T4>(entity);
                        var c5     = _w.ReadComponent<T5>(entity);
                        _cur = new Result(entity, c1, c2, c3, c4, c5);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T6

    /// <summary>
    /// Internal context for six-component queries.
    /// </summary>
    internal readonly struct QueryCtx6<T1, T2, T3, T4, T5, T6>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for six-component queries.
        /// </summary>
        public QueryCtx6(
            IComponentPool? a,
            IComponentPool? b,
            IComponentPool? c,
            IComponentPool? d,
            IComponentPool? e,
            IComponentPool? f,
            World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            D  = d;
            E  = e;
            F  = f;
            RF = rf;
        }
    }

    /// <summary>
    /// Six-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <typeparam name="T5">Fifth component type.</typeparam>
    /// <typeparam name="T6">Sixth component type.</typeparam>
    /// <remarks>
    /// <code language="csharp"><![CDATA[
    /// foreach (var (e, c1..c6) in world.Query<T1, T2, T3, T4, T5, T6>())
    /// {
    ///     // ...
    /// }
    /// ]]></code>
    /// </remarks>
    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct
    {
        private readonly World _w;
        private readonly QueryCtx6<T1, T2, T3, T4, T5, T6> _ctx;

        internal QueryEnumerable(World w, in QueryCtx6<T1, T2, T3, T4, T5, T6> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for six-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;
            public readonly T4     Component4;
            public readonly T5     Component5;
            public readonly T6     Component6;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(
                in Entity entity,
                in T1 c1,
                in T2 c2,
                in T3 c3,
                in T4 c4,
                in T5 c5,
                in T6 c6)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
                Component4 = c4;
                Component5 = c5;
                Component6 = c6;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(
                out Entity entity,
                out T1 c1,
                out T2 c2,
                out T3 c3,
                out T4 c4,
                out T5 c5,
                out T6 c6)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
                c4     = Component4;
                c5     = Component5;
                c6     = Component6;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3,T4,T5,T6}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx6<T1, T2, T3, T4, T5, T6> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _d  = ctx.D;
                _e  = ctx.E;
                _f  = ctx.F;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null ||
                    _d == null || _e == null || _f == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1..T6) tuple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Moves to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        _d!.Has(id) &&
                        _e!.Has(id) &&
                        _f!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        var c4     = _w.ReadComponent<T4>(entity);
                        var c5     = _w.ReadComponent<T5>(entity);
                        var c6     = _w.ReadComponent<T6>(entity);
                        _cur = new Result(entity, c1, c2, c3, c4, c5, c6);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T7

    /// <summary>
    /// Internal context for seven-component queries.
    /// </summary>
    internal readonly struct QueryCtx7<T1, T2, T3, T4, T5, T6, T7>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F, G;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for seven-component queries.
        /// </summary>
        public QueryCtx7(
            IComponentPool? a,
            IComponentPool? b,
            IComponentPool? c,
            IComponentPool? d,
            IComponentPool? e,
            IComponentPool? f,
            IComponentPool? g,
            World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            D  = d;
            E  = e;
            F  = f;
            G  = g;
            RF = rf;
        }
    }

    /// <summary>
    /// Seven-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <typeparam name="T5">Fifth component type.</typeparam>
    /// <typeparam name="T6">Sixth component type.</typeparam>
    /// <typeparam name="T7">Seventh component type.</typeparam>
    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct
    {
        private readonly World _w;
        private readonly QueryCtx7<T1, T2, T3, T4, T5, T6, T7> _ctx;

        internal QueryEnumerable(World w, in QueryCtx7<T1, T2, T3, T4, T5, T6, T7> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for seven-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;
            public readonly T4     Component4;
            public readonly T5     Component5;
            public readonly T6     Component6;
            public readonly T7     Component7;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(
                in Entity entity,
                in T1 c1,
                in T2 c2,
                in T3 c3,
                in T4 c4,
                in T5 c5,
                in T6 c6,
                in T7 c7)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
                Component4 = c4;
                Component5 = c5;
                Component6 = c6;
                Component7 = c7;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(
                out Entity entity,
                out T1 c1,
                out T2 c2,
                out T3 c3,
                out T4 c4,
                out T5 c5,
                out T6 c6,
                out T7 c7)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
                c4     = Component4;
                c5     = Component5;
                c6     = Component6;
                c7     = Component7;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3,T4,T5,T6,T7}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f, _g;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx7<T1, T2, T3, T4, T5, T6, T7> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _d  = ctx.D;
                _e  = ctx.E;
                _f  = ctx.F;
                _g  = ctx.G;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null ||
                    _d == null || _e == null || _f == null || _g == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f, _g);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1..T7) tuple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Moves to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        _d!.Has(id) &&
                        _e!.Has(id) &&
                        _f!.Has(id) &&
                        _g!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        var c4     = _w.ReadComponent<T4>(entity);
                        var c5     = _w.ReadComponent<T5>(entity);
                        var c6     = _w.ReadComponent<T6>(entity);
                        var c7     = _w.ReadComponent<T7>(entity);
                        _cur = new Result(entity, c1, c2, c3, c4, c5, c6, c7);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion

    #region T1..T8

    /// <summary>
    /// Internal context for eight-component queries.
    /// </summary>
    internal readonly struct QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F, G, H;
        public readonly World.ResolvedFilter RF;

        /// <summary>
        /// Creates a new query context for eight-component queries.
        /// </summary>
        public QueryCtx8(
            IComponentPool? a,
            IComponentPool? b,
            IComponentPool? c,
            IComponentPool? d,
            IComponentPool? e,
            IComponentPool? f,
            IComponentPool? g,
            IComponentPool? h,
            World.ResolvedFilter rf)
        {
            A  = a;
            B  = b;
            C  = c;
            D  = d;
            E  = e;
            F  = f;
            G  = g;
            H  = h;
            RF = rf;
        }
    }

    /// <summary>
    /// Eight-component query enumerable.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <typeparam name="T5">Fifth component type.</typeparam>
    /// <typeparam name="T6">Sixth component type.</typeparam>
    /// <typeparam name="T7">Seventh component type.</typeparam>
    /// <typeparam name="T8">Eighth component type.</typeparam>
    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
        private readonly World _w;
        private readonly QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> _ctx;

        internal QueryEnumerable(World w, in QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> ctx)
        {
            _w   = w;
            _ctx = ctx;
        }

        /// <summary>
        /// Result item for eight-component queries.
        /// </summary>
        public readonly struct Result
        {
            public readonly Entity Entity;
            public readonly T1     Component1;
            public readonly T2     Component2;
            public readonly T3     Component3;
            public readonly T4     Component4;
            public readonly T5     Component5;
            public readonly T6     Component6;
            public readonly T7     Component7;
            public readonly T8     Component8;

            /// <summary>
            /// Creates a new result container.
            /// </summary>
            public Result(
                in Entity entity,
                in T1 c1,
                in T2 c2,
                in T3 c3,
                in T4 c4,
                in T5 c5,
                in T6 c6,
                in T7 c7,
                in T8 c8)
            {
                Entity     = entity;
                Component1 = c1;
                Component2 = c2;
                Component3 = c3;
                Component4 = c4;
                Component5 = c5;
                Component6 = c6;
                Component7 = c7;
                Component8 = c8;
            }

            /// <summary>
            /// Deconstructs this result into its values.
            /// </summary>
            public void Deconstruct(
                out Entity entity,
                out T1 c1,
                out T2 c2,
                out T3 c3,
                out T4 c4,
                out T5 c5,
                out T6 c6,
                out T7 c7,
                out T8 c8)
            {
                entity = Entity;
                c1     = Component1;
                c2     = Component2;
                c3     = Component3;
                c4     = Component4;
                c5     = Component5;
                c6     = Component6;
                c7     = Component7;
                c8     = Component8;
            }
        }

        /// <summary>Returns an enumerator over results.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        /// <summary>
        /// Enumerator for <see cref="QueryEnumerable{T1,T2,T3,T4,T5,T6,T7,T8}"/>.
        /// </summary>
        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f, _g, _h;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Result _cur;

            internal Enumerator(World w, in QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _c  = ctx.C;
                _d  = ctx.D;
                _e  = ctx.E;
                _f  = ctx.F;
                _g  = ctx.G;
                _h  = ctx.H;
                _rf = ctx.RF;

                if (_a == null || _b == null || _c == null || _d == null ||
                    _e == null || _f == null || _g == null || _h == null)
                {
                    _it  = PoolEnumerator.Empty;
                    _cur = default;
                    return;
                }

                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f, _g, _h);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            /// <summary>
            /// Current (Entity, T1..T8) tuple.
            /// </summary>
            public Result Current => _cur;

            /// <summary>
            /// Moves to the next matching entity, if any.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;

                    if (_a!.Has(id) &&
                        _b!.Has(id) &&
                        _c!.Has(id) &&
                        _d!.Has(id) &&
                        _e!.Has(id) &&
                        _f!.Has(id) &&
                        _g!.Has(id) &&
                        _h!.Has(id) &&
                        World.MeetsFilter(id, in _rf))
                    {
                        var entity = new Entity(id, _w.GenerationOf(id));
                        var c1     = _w.ReadComponent<T1>(entity);
                        var c2     = _w.ReadComponent<T2>(entity);
                        var c3     = _w.ReadComponent<T3>(entity);
                        var c4     = _w.ReadComponent<T4>(entity);
                        var c5     = _w.ReadComponent<T5>(entity);
                        var c6     = _w.ReadComponent<T6>(entity);
                        var c7     = _w.ReadComponent<T7>(entity);
                        var c8     = _w.ReadComponent<T8>(entity);
                        _cur = new Result(entity, c1, c2, c3, c4, c5, c6, c7, c8);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    #endregion
}
