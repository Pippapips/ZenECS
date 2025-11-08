#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.ComponentPooling;

namespace ZenECS.Core
{
    // ---------- Filter DSL ----------

    /// <summary>
    /// Immutable value describing a query filter with include/exclude constraints.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Builder"/> via <see cref="New"/> to compose filters fluently,
    /// then pass the result to <c>world.Query&lt;...>(filter)</c>.
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
        /// Entry point for the fluent filter builder.
        /// </summary>
        public static Builder New => default;

        /// <summary>
        /// Fluent, immutable builder used to compose <see cref="Filter"/> instances.
        /// </summary>
        /// <remarks>
        /// Each method returns a new builder that includes the requested constraint; the original builder remains unchanged.
        /// </remarks>
        public readonly struct Builder
        {
            private readonly List<Type> wa;
            private readonly List<Type> wo;
            private readonly List<List<Type>> wan;
            private readonly List<List<Type>> won;

            /// <summary>
            /// Requires that entities include component <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">Component value type.</typeparam>
            /// <returns>A new builder with the constraint added.</returns>
            public Builder With<T>() where T : struct => new(Append(wa, typeof(T)), wo, wan, won);

            /// <summary>
            /// Requires that entities exclude component <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">Component value type.</typeparam>
            /// <returns>A new builder with the constraint added.</returns>
            public Builder Without<T>() where T : struct => new(wa, Append(wo, typeof(T)), wan, won);

            /// <summary>
            /// Adds a logical OR group: the entity passes if it contains <em>any one</em> of the specified types.
            /// </summary>
            /// <param name="types">One or more component types to OR together.</param>
            /// <returns>A new builder with the constraint added.</returns>
            public Builder WithAny(params Type[] types) => new(wa, wo, AppendBucket(wan, types), won);

            /// <summary>
            /// Adds a negative logical OR group: the entity fails if it contains <em>any one</em> of the specified types.
            /// </summary>
            /// <param name="types">One or more component types to OR together for exclusion.</param>
            /// <returns>A new builder with the constraint added.</returns>
            public Builder WithoutAny(params Type[] types) => new(wa, wo, wan, AppendBucket(won, types));

            /// <summary>
            /// Finalizes the builder into an immutable <see cref="Filter"/>.
            /// </summary>
            /// <returns>The composed filter.</returns>
            public Filter Build()
            {
                return new Filter(
                    wa?.ToArray() ?? Array.Empty<Type>(),
                    wo?.ToArray() ?? Array.Empty<Type>(),
                    ToJagged(wan),
                    ToJagged(won));
            }

            private Builder(List<Type> wa, List<Type> wo, List<List<Type>> wan, List<List<Type>> won)
            {
                this.wa = wa;
                this.wo = wo;
                this.wan = wan;
                this.won = won;
            }

            private static List<Type> Append(List<Type> list, Type t)
            {
                var l = list ?? new List<Type>(4);
                l.Add(t);
                return l;
            }

            private static List<List<Type>> AppendBucket(List<List<Type>> list, Type[] types)
            {
                var l = list ?? new List<List<Type>>(2);
                var b = new List<Type>(types.Length);
                foreach (var t in types)
                    if (t != null)
                        b.Add(t);
                if (b.Count > 0) l.Add(b);
                return l;
            }

            private static Type[][] ToJagged(List<List<Type>> src)
            {
                if (src == null || src.Count == 0) return Array.Empty<Type[]>();
                var arr = new Type[src.Count][];
                for (int i = 0; i < src.Count; i++) arr[i] = src[i].ToArray();
                return arr;
            }
        }
    }
    
    #region Seed picker (internal)

    internal static class QuerySeed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IComponentPool? Pick(params IComponentPool?[] pools)
        {
            IComponentPool? best = null;
            int bestCap = int.MaxValue;
            for (int i = 0; i < pools.Length; i++)
            {
                var p = pools[i];
                if (p == null) continue;
                var cap = p.Capacity; // 가능하면 ActiveCount로 바꿔도 됨
                if (cap < bestCap) { best = p; bestCap = cap; }
            }
            return best;
        }
    }

    #endregion

    #region T1

    internal readonly struct QueryCtx1<T1> where T1 : struct
    {
        public readonly IComponentPool? A;
        public readonly World.ResolvedFilter RF;
        public QueryCtx1(IComponentPool? a, World.ResolvedFilter rf) { A = a; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1> where T1 : struct
    {
        private readonly World _w;
        private readonly QueryCtx1<T1> _ctx;

        internal QueryEnumerable(World w, in QueryCtx1<T1> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx1<T1> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _rf = ctx.RF;
                var seed = _a;
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) && World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1,T2

    internal readonly struct QueryCtx2<T1, T2>
        where T1 : struct where T2 : struct
    {
        public readonly IComponentPool? A, B;
        public readonly World.ResolvedFilter RF;
        public QueryCtx2(IComponentPool? a, IComponentPool? b, World.ResolvedFilter rf)
        { A = a; B = b; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2>
        where T1 : struct where T2 : struct
    {
        private readonly World _w;
        private readonly QueryCtx2<T1, T2> _ctx;

        internal QueryEnumerable(World w, in QueryCtx2<T1, T2> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx2<T1, T2> ctx)
            {
                _w  = w;
                _a  = ctx.A;
                _b  = ctx.B;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T3

    internal readonly struct QueryCtx3<T1, T2, T3>
        where T1 : struct where T2 : struct where T3 : struct
    {
        public readonly IComponentPool? A, B, C;
        public readonly World.ResolvedFilter RF;
        public QueryCtx3(IComponentPool? a, IComponentPool? b, IComponentPool? c, World.ResolvedFilter rf)
        { A = a; B = b; C = c; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3>
        where T1 : struct where T2 : struct where T3 : struct
    {
        private readonly World _w;
        private readonly QueryCtx3<T1, T2, T3> _ctx;

        internal QueryEnumerable(World w, in QueryCtx3<T1, T2, T3> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx3<T1, T2, T3> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T4

    internal readonly struct QueryCtx4<T1, T2, T3, T4>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        public readonly IComponentPool? A, B, C, D;
        public readonly World.ResolvedFilter RF;
        public QueryCtx4(IComponentPool? a, IComponentPool? b, IComponentPool? c, IComponentPool? d, World.ResolvedFilter rf)
        { A = a; B = b; C = c; D = d; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3, T4>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        private readonly World _w;
        private readonly QueryCtx4<T1, T2, T3, T4> _ctx;

        internal QueryEnumerable(World w, in QueryCtx4<T1, T2, T3, T4> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx4<T1, T2, T3, T4> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C; _d = ctx.D;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c, _d);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        (_d == null || _d.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T5

    internal readonly struct QueryCtx5<T1, T2, T3, T4, T5>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        public readonly IComponentPool? A, B, C, D, E;
        public readonly World.ResolvedFilter RF;
        public QueryCtx5(IComponentPool? a, IComponentPool? b, IComponentPool? c, IComponentPool? d, IComponentPool? e, World.ResolvedFilter rf)
        { A = a; B = b; C = c; D = d; E = e; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        private readonly World _w;
        private readonly QueryCtx5<T1, T2, T3, T4, T5> _ctx;

        internal QueryEnumerable(World w, in QueryCtx5<T1, T2, T3, T4, T5> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx5<T1, T2, T3, T4, T5> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C; _d = ctx.D; _e = ctx.E;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        (_d == null || _d.Has(id)) &&
                        (_e == null || _e.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T6

    internal readonly struct QueryCtx6<T1, T2, T3, T4, T5, T6>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F;
        public readonly World.ResolvedFilter RF;
        public QueryCtx6(IComponentPool? a, IComponentPool? b, IComponentPool? c, IComponentPool? d, IComponentPool? e, IComponentPool? f, World.ResolvedFilter rf)
        { A = a; B = b; C = c; D = d; E = e; F = f; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        private readonly World _w;
        private readonly QueryCtx6<T1, T2, T3, T4, T5, T6> _ctx;

        internal QueryEnumerable(World w, in QueryCtx6<T1, T2, T3, T4, T5, T6> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx6<T1, T2, T3, T4, T5, T6> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C; _d = ctx.D; _e = ctx.E; _f = ctx.F;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        (_d == null || _d.Has(id)) &&
                        (_e == null || _e.Has(id)) &&
                        (_f == null || _f.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T7

    internal readonly struct QueryCtx7<T1, T2, T3, T4, T5, T6, T7>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F, G;
        public readonly World.ResolvedFilter RF;
        public QueryCtx7(IComponentPool? a, IComponentPool? b, IComponentPool? c, IComponentPool? d, IComponentPool? e, IComponentPool? f, IComponentPool? g, World.ResolvedFilter rf)
        { A = a; B = b; C = c; D = d; E = e; F = f; G = g; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
    {
        private readonly World _w;
        private readonly QueryCtx7<T1, T2, T3, T4, T5, T6, T7> _ctx;

        internal QueryEnumerable(World w, in QueryCtx7<T1, T2, T3, T4, T5, T6, T7> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f, _g;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx7<T1, T2, T3, T4, T5, T6, T7> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C; _d = ctx.D; _e = ctx.E; _f = ctx.F; _g = ctx.G;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f, _g);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        (_d == null || _d.Has(id)) &&
                        (_e == null || _e.Has(id)) &&
                        (_f == null || _f.Has(id)) &&
                        (_g == null || _g.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion

    #region T1..T8

    internal readonly struct QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
        public readonly IComponentPool? A, B, C, D, E, F, G, H;
        public readonly World.ResolvedFilter RF;
        public QueryCtx8(IComponentPool? a, IComponentPool? b, IComponentPool? c, IComponentPool? d,
                         IComponentPool? e, IComponentPool? f, IComponentPool? g, IComponentPool? h,
                         World.ResolvedFilter rf)
        { A = a; B = b; C = c; D = d; E = e; F = f; G = g; H = h; RF = rf; }
    }

    public readonly struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
        private readonly World _w;
        private readonly QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> _ctx;

        internal QueryEnumerable(World w, in QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> ctx) { _w = w; _ctx = ctx; }

        public Enumerator GetEnumerator() => new Enumerator(_w, _ctx);

        public struct Enumerator
        {
            private readonly World _w;
            private readonly IComponentPool? _a, _b, _c, _d, _e, _f, _g, _h;
            private readonly World.ResolvedFilter _rf;
            private PoolEnumerator _it;
            private Entity _cur;

            internal Enumerator(World w, in QueryCtx8<T1, T2, T3, T4, T5, T6, T7, T8> ctx)
            {
                _w  = w;
                _a  = ctx.A; _b = ctx.B; _c = ctx.C; _d = ctx.D; _e = ctx.E; _f = ctx.F; _g = ctx.G; _h = ctx.H;
                _rf = ctx.RF;
                var seed = QuerySeed.Pick(_a, _b, _c, _d, _e, _f, _g, _h);
                _it  = seed != null ? seed.EnumerateIds() : PoolEnumerator.Empty;
                _cur = default;
            }

            public Entity Current => _cur;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_it.MoveNext())
                {
                    int id = _it.CurrentId;
                    if ((_a == null || _a.Has(id)) &&
                        (_b == null || _b.Has(id)) &&
                        (_c == null || _c.Has(id)) &&
                        (_d == null || _d.Has(id)) &&
                        (_e == null || _e.Has(id)) &&
                        (_f == null || _f.Has(id)) &&
                        (_g == null || _g.Has(id)) &&
                        (_h == null || _h.Has(id)) &&
                        World.MeetsFilter(id, in _rf))
                    { _cur = new Entity(id, _w.GenerationOf(id)); return true; }
                }
                return false;
            }
        }
    }

    #endregion    
}