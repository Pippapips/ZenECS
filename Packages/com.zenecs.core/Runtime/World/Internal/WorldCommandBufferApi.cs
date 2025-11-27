// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Command Buffer API)
// File: WorldCommandBufferApi.cs
// Purpose: Frame-barrier-friendly command recording & application.
// Key concepts:
//   • using-scope buffers: record operations, apply on Dispose (schedule/immediate).
//   • Scheduler integration: deferred jobs flushed at safe barriers.
//   • Safety on reset: pending jobs are drained to avoid dropping work.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldCommandBufferApi"/> for recording/applying world mutations.
    /// </summary>
    internal sealed partial class World : IWorldCommandBufferApi
    {
        /// <summary>
        /// Begin a command-buffer write scope bound to this world.
        /// </summary>
        /// <param name="mode">
        /// Apply-on-dispose mode:
        /// <see cref="CommandBufferApplyMode.Schedule"/> queues for later (frame barrier),
        /// <see cref="CommandBufferApplyMode.Immediate"/> applies instantly.
        /// </param>
        /// <returns>A new <see cref="CommandBuffer"/>.</returns>
        public ICommandBuffer BeginWrite()
        {
            return new CommandBuffer(this, _worker);
        }

        /// <summary>
        /// Clear pending frame-local command buffers by flushing the scheduler queue.
        /// </summary>
        private void ClearAllCommandBuffers()
        {
            _worker.ClearAllScheduledJobs();
        }
    }
}
