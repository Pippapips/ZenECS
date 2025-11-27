// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Worker/Scheduler API
// File: IWorldWorkerApi.cs
// Purpose: Execute scheduled jobs queued against the owning world instance.
// Key concepts:
//   • Per-world isolation: jobs run with the world as execution context.
//   • Deterministic barriers: callers choose when to flush queued work.
//   • Integration: command buffers and deferred ops can target the worker.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core
{
    /// <summary>
    /// World-scoped worker interface for running scheduled jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The worker API provides a single deterministic barrier where queued jobs
    /// (such as deferred structural changes, async completions, or adapter tasks)
    /// can be executed against the owning world instance.
    /// </para>
    /// <para>
    /// Each world has its own worker; jobs from different worlds are isolated and
    /// must be flushed independently.
    /// </para>
    /// </remarks>
    public interface IWorldWorkerApi
    {
        /// <summary>
        /// Executes all jobs currently scheduled for this world.
        /// </summary>
        /// <returns>
        /// The number of jobs executed during this call.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Callers are free to choose when to run scheduled jobs. A common pattern
        /// is to invoke this at explicit simulation barriers (for example, after
        /// all systems in a phase have run).
        /// </para>
        /// </remarks>
        int RunScheduledJobs();
    }
}