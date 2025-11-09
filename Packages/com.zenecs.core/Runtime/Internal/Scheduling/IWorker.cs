// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: IWorker.cs
// Purpose: Minimal job scheduler for world-scoped background work.
// Key concepts:
//   • Queue IJob instances; run them at the simulation barrier
//   • Deterministic FIFO execution per frame
//   • ClearAllScheduledJobs() for teardown / hard resets
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Internal.Scheduling
{
    /// <summary>
    /// Unit of executable work scheduled against a specific <see cref="IWorld"/>.
    /// </summary>
    internal interface IJob
    {
        /// <summary>Executes the job against the provided world.</summary>
        void Execute(IWorld w);
    }

    /// <summary>
    /// Simple FIFO worker used by the world to run scheduled jobs (e.g., command buffers)
    /// at a well-defined point in the frame.
    /// </summary>
    internal interface IWorker
    {
        /// <summary>Enqueue a job to be run later; ignores <see langword="null"/>.</summary>
        void Schedule(IJob? job);

        /// <summary>
        /// Dequeue and execute all scheduled jobs against <paramref name="w"/>.
        /// </summary>
        /// <returns>The number of executed jobs.</returns>
        int RunScheduledJobs(IWorld w);

        /// <summary>Clears all pending jobs without executing them.</summary>
        void ClearAllScheduledJobs();
    }
}