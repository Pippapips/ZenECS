// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Reset API
// File: IWorldResetApi.cs
// Purpose: Reset world storage and subsystems with optional capacity reuse.
// Key concepts:
//   • Fast reset: keep capacities; clear data; rebuild empty pools.
//   • Hard reset: discard storage; recreate from initial configuration.
//   • Hooks: subsystems can prepare/rehydrate around the reset boundary.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core
{
    /// <summary>
    /// Reset operations for a world. Implementations should guarantee that
    /// post-reset state is consistent and caches/queues are cleared.
    /// </summary>
    public interface IWorldResetApi
    {
        /// <summary>
        /// Reset the world.
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to reuse current array/pool capacities (fast path);
        /// <see langword="false"/> to rebuild storage from initial configuration.
        /// </param>
        void Reset(bool keepCapacity);
    }
}