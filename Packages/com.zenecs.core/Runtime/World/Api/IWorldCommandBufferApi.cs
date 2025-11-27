// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Command Buffer API
// File: IWorldCommandBufferApi.cs
// Purpose: Deterministic, barrier-applied recording of world mutations.
// Key concepts:
//   • Systems never mutate the world directly.
//   • All structural & value changes are recorded into command buffers.
//   • Buffers are applied only at well-defined tick barriers.
//   • This enables deterministic, network- and replay-friendly simulation.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable

namespace ZenECS.Core
{
    /// <summary>
    /// World-side API for beginning command buffers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runner/worker is responsible for applying buffers at well-defined
    /// tick barriers (for example, at the end of the fixed-step or frame-step
    /// pipelines). This decouples mutation recording from the moment mutations
    /// are actually applied.
    /// </para>
    /// </remarks>
    public interface IWorldCommandBufferApi
    {
        /// <summary>
        /// Begins recording a new command buffer bound to this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned buffer never applies mutations immediately; it only
        /// records operations that will later be applied at a deterministic
        /// barrier by the runner/worker.
        /// </para>
        /// </remarks>
        /// <returns>A new command buffer bound to the world.</returns>
        ICommandBuffer BeginWrite();
    }
}