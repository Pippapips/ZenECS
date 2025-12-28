// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: IWorker.cs
// Purpose: Minimal job scheduler for world-scoped background work.
// Key concepts:
//   • Queue IJob instances; run them at the simulation barrier
//   • Deterministic FIFO execution per frame
//   • ClearAllScheduledJobs() for teardown / hard resets
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Scheduling.Internal
{
    /// <summary>
    /// Unit of executable work scheduled against a specific <see cref="IWorld"/>.
    /// </summary>
    internal interface IJob
    {
        /// <summary>
        /// Executes the job against the provided world.
        /// </summary>
        /// <param name="w">World instance the job should operate on.</param>
        void Execute(IWorld w);
    }

    /// <summary>
    /// Simple FIFO worker used by the world to run scheduled jobs
    /// (for example, command buffers) at a well-defined point in the frame.
    /// </summary>
    internal interface IWorker
    {
        /// <summary>
        /// Enqueues a job to be run later.
        /// </summary>
        /// <param name="job">
        /// Job instance to schedule. If <see langword="null"/>, the call is ignored.
        /// </param>
        void Schedule(IJob? job);

        /// <summary>
        /// Dequeues and executes all scheduled jobs against the specified world.
        /// </summary>
        /// <param name="w">World instance passed into each job on execution.</param>
        /// <returns>
        /// The number of jobs that were executed during this call.
        /// </returns>
        int RunScheduledJobs(IWorld w);

        /// <summary>
        /// Clears all pending jobs without executing any of them.
        /// </summary>
        void ClearAllScheduledJobs();
    }
}
