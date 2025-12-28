// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Query • Span utilities)
// File: WorldQuerySpanApi.cs
// Purpose: Span-based zero-allocation entity collection and ref-processing utilities.
// Key concepts:
//   • Span-fill queries: write matching entities directly into Span<Entity>.
//   • Zero-GC hot path: iterate refs without boxing or heap churn.
//   • Batch ops: process ref components across a span in tight loops.
//   • Safety: skips dead/missing components; uses RefComponentExisting for correctness.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Span-oriented query helpers for zero-allocation entity collection and batch processing.
    /// </summary>
    /// <remarks>
    /// <para><b>Typical usage</b></para>
    /// <code language="csharp"><![CDATA[
    /// Span<Entity> tmp = stackalloc Entity[2048];
    /// int n = world.QueryToSpan<Health, Damage, Owner, Team>(tmp, f);
    /// world.Process<Health>(tmp[..n], (ref Health h) => { h.Value = Math.Max(0, h.Value - 5); });
    /// ]]></code>
    /// </remarks>
    internal sealed partial class World : IWorldQuerySpanApi
    {
        // ---- QueryToSpan T1..T8 ----

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/> into a destination span.
        /// </summary>
        /// <typeparam name="T1">Required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// The number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        public int QueryToSpan<T1>(Span<Entity> dst, Filter f = default) where T1 : struct
        {
            int n = 0;
            foreach (var (e, _) in Query<T1>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        public int QueryToSpan<T1, T2>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct
        {
            int n = 0;
            foreach (var (e, _, _) in Query<T1, T2>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>,
        /// <typeparamref name="T2"/> and <typeparamref name="T3"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _) in Query<T1, T2, T3>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>…<typeparamref name="T4"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3, T4>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _, _) in Query<T1, T2, T3, T4>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>…<typeparamref name="T5"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3, T4, T5>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _, _, _) in Query<T1, T2, T3, T4, T5>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>…<typeparamref name="T6"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3, T4, T5, T6>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _, _, _, _) in Query<T1, T2, T3, T4, T5, T6>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>…<typeparamref name="T7"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3, T4, T5, T6, T7>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _, _, _, _, _) in Query<T1, T2, T3, T4, T5, T6, T7>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Collect entities that have <typeparamref name="T1"/>…<typeparamref name="T8"/>.
        /// </summary>
        public int QueryToSpan<T1, T2, T3, T4, T5, T6, T7, T8>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct where T8 : struct
        {
            int n = 0;
            foreach (var (e, _, _, _, _, _, _, _, _) in Query<T1, T2, T3, T4, T5, T6, T7, T8>(f))
            {
                if (n >= dst.Length) break;
                dst[n++] = e;
            }

            return n;
        }

        /// <summary>
        /// Iterate a span of entities and apply a ref action to each existing component.
        /// Dead entities and entities without the component are skipped.
        /// </summary>
        /// <typeparam name="T">Component type to process.</typeparam>
        /// <param name="ents">
        /// Input entity span (commonly the slice returned from <see cref="QueryToSpan{T1}(Span{Entity}, Filter)"/>).
        /// </param>
        /// <param name="action">
        /// Delegate invoked with a <c>ref T</c> for each matching entity.
        /// </param>
        public void Process<T>(ReadOnlySpan<Entity> ents, RefAction<T> action) where T : struct
        {
            for (int i = 0; i < ents.Length; i++)
            {
                var e = ents[i];
                if (!IsAlive(e)) continue;
                if (!HasComponent<T>(e)) continue;
                action(ref RefComponentExisting<T>(e));
            }
        }
    }
}
