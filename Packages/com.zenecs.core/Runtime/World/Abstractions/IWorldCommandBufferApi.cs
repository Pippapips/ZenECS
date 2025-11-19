// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Command Buffer API
// File: IWorldCommandBufferApi.cs
// Purpose: Deterministic, barrier-applied recording of world mutations.
// Key concepts:
//   • Systems never mutate the world directly.
//   • All structural & value changes are recorded into command buffers.
//   • Buffers are applied only at well-defined tick barriers.
//   • This enables deterministic, network- and replay-friendly simulation.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core
{
    /// <summary>
    /// World-side API for beginning command buffers.
    /// <para>
    /// The runner/worker is responsible for applying buffers at well-defined
    /// tick barriers (e.g., end of fixed step, end of variable step).
    /// </para>
    /// </summary>
    public interface IWorldCommandBufferApi
    {
        /// <summary>
        /// Begin recording a new command buffer bound to this world.
        /// <para>
        /// The returned buffer never applies mutations immediately; it only
        /// records operations that will later be applied at a deterministic
        /// barrier by the runner/worker.
        /// </para>
        /// </summary>
        /// <returns>A new command buffer bound to the world.</returns>
        ICommandBuffer BeginWrite();
    }
}
