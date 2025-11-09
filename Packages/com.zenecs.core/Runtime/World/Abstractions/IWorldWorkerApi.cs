// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Worker/Scheduler API
// File: IWorldWorkerApi.cs
// Purpose: Execute scheduled jobs queued against the owning world instance.
// Key concepts:
//   • Per-world isolation: jobs run with the world as execution context.
//   • Deterministic barriers: callers choose when to flush queued work.
//   • Integration: command buffers and deferred ops can target the worker.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core
{
    /// <summary>
    /// World-scoped worker interface for running scheduled jobs.
    /// </summary>
    public interface IWorldWorkerApi
    {
        /// <summary>
        /// Execute all queued jobs for this world.
        /// </summary>
        /// <returns>The number of jobs executed.</returns>
        int RunScheduledJobs();
    }
}