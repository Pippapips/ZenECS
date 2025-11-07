﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Query.cs
// Purpose: Query builder and iterator for ref-based component enumeration.
// Key concepts:
//   • Seeds enumeration from the smallest pool for efficiency.
//   • Filter WithAny/WithoutAny to constrain sets.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.ComponentPooling;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldQueryApi
    {
        public QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx1<T1>(a, rf);
            return new QueryEnumerable<T1>(this, in ctx);
        }

        public QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct
        {
            var a  = _componentPoolRepository.TryGetPool<T1>();
            var b  = _componentPoolRepository.TryGetPool<T2>();
            var rf = ResolveFilter(f);
            var ctx = new QueryCtx2<T1, T2>(a, b, rf);
            return new QueryEnumerable<T1, T2>(this, in ctx);
        }

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
