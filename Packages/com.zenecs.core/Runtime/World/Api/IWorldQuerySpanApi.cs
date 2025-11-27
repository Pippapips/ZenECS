// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Query (Span)
// File: IWorldQuerySpanApi.cs
// Purpose: Span-based, zero-allocation helpers for collecting entities and
//          batch-processing component refs.
// Key concepts:
//   • Span fill: write matching entities directly into Span<Entity> (no GC).
//   • Tight loops: process ref components over ReadOnlySpan<Entity>.
//   • Safety: skips dead/missing components during processing.
//   • Interop: complements IWorldQueryApi (T1..T8) typed queries.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Delegate used to process a component by reference (no boxing/copy).
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="value">Reference to the component value.</param>
    public delegate void RefAction<T>(ref T value) where T : struct;

    /// <summary>
    /// Span-oriented query surface for zero-allocation collection and batch
    /// processing. Use alongside <see cref="IWorldQueryApi"/> to first collect
    /// entities, then process their components in tight loops.
    /// </summary>
    public interface IWorldQuerySpanApi
    {
        /// <summary>
        /// Collect entities having <typeparamref name="T1"/> into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">Required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1>(Span<Entity> dst, Filter f = default) where T1 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/> and <typeparamref name="T2"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>,
        /// <typeparamref name="T2"/> and <typeparamref name="T3"/> into
        /// <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>…<typeparamref name="T4"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3, T4>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>…<typeparamref name="T5"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3, T4, T5>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>…<typeparamref name="T6"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3, T4, T5, T6>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
            where T4 : struct where T5 : struct where T6 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>…<typeparamref name="T7"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <typeparam name="T7">Seventh required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3, T4, T5, T6, T7>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
            where T4 : struct where T5 : struct where T6 : struct where T7 : struct;

        /// <summary>
        /// Collect entities having <typeparamref name="T1"/>…<typeparamref name="T8"/>
        /// into <paramref name="dst"/>.
        /// </summary>
        /// <typeparam name="T1">First required component type.</typeparam>
        /// <typeparam name="T2">Second required component type.</typeparam>
        /// <typeparam name="T3">Third required component type.</typeparam>
        /// <typeparam name="T4">Fourth required component type.</typeparam>
        /// <typeparam name="T5">Fifth required component type.</typeparam>
        /// <typeparam name="T6">Sixth required component type.</typeparam>
        /// <typeparam name="T7">Seventh required component type.</typeparam>
        /// <typeparam name="T8">Eighth required component type.</typeparam>
        /// <param name="dst">Destination span to write entity handles to.</param>
        /// <param name="f">Optional filter (with/without any/all buckets).</param>
        /// <returns>
        /// Number of entities written (≤ <paramref name="dst"/> length).
        /// </returns>
        int QueryToSpan<T1, T2, T3, T4, T5, T6, T7, T8>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
            where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct;

        /// <summary>
        /// Iterate <paramref name="ents"/> and apply <paramref name="action"/> to each
        /// existing <typeparamref name="T"/> component by reference.
        /// Dead entities and entities lacking the component are skipped.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="ents">
        /// Input entity span (commonly the slice returned from <c>QueryToSpan</c>).
        /// </param>
        /// <param name="action">
        /// Delegate invoked with a <c>ref T</c> for each matching entity.
        /// </param>
        void Process<T>(ReadOnlySpan<Entity> ents, RefAction<T> action) where T : struct;
    }
}
