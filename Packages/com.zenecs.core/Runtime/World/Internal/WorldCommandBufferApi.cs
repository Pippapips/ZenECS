// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Command Buffer API)
// File: WorldCommandBufferApi.cs
// Purpose: Frame-barrier-friendly command recording & application.
// Key concepts:
//   • using-scope buffers: record operations, apply on Dispose (schedule/immediate).
//   • Scheduler integration: deferred jobs flushed at safe barriers.
//   • Safety on reset: pending jobs are drained to avoid dropping work.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using ZenECS.Core.Internal.Scheduling;

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
        public ICommandBuffer BeginWrite(CommandBufferApplyMode mode = CommandBufferApplyMode.Schedule)
        {
            var cb = new CommandBuffer();
            cb.Bind(this, mode);
            return cb;
        }

        /// <summary>
        /// Apply all enqueued operations in the given buffer immediately.
        /// </summary>
        /// <param name="icb">Command buffer to flush; ignored if <see langword="null"/>.</param>
        /// <returns>Number of applied operations.</returns>
        public int EndWrite(ICommandBuffer icb)
        {
            var cb = (CommandBuffer)icb;
            if (cb == null) return 0;
            int n = 0;
            while (cb.Q.TryDequeue(out var op))
            {
                op.Apply(this);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Schedule a command buffer to run at the next safe frame barrier.
        /// </summary>
        /// <param name="cb">Command buffer to schedule; ignored if <see langword="null"/>.</param>
        public void Schedule(ICommandBuffer? cb)
        {
            if (cb != null)
                _worker.Schedule((IJob)cb);
        }

        /// <summary>
        /// Clear pending frame-local command buffers by flushing the scheduler queue.
        /// </summary>
        public void ClearAllCommandBuffers()
        {
            _worker.ClearAllScheduledJobs();
        }

        /// <summary>
        /// Hook executed before world reset. When capacity will be rebuilt, flush jobs first.
        /// </summary>
        /// <param name="keepCapacity">Keep capacity if <see langword="true"/>; rebuild if <see langword="false"/>.</param>
        partial void OnBeforeWorldReset(bool keepCapacity)
        {
            if (!keepCapacity) _worker.RunScheduledJobs(this);
        }
    }
}
