// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Worker/Scheduler API)
// File: WorldWorkerApi.cs
// Purpose: Expose world-scoped worker to run scheduled jobs at safe barriers.
// Key concepts:
//   • Per-world isolation: jobs run against the owning world instance.
//   • Determinism: callers explicitly pull/flush scheduled jobs when desired.
//   • Integration: command buffers, deferred ops, and async work units can queue here.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldWorkerApi"/>: run scheduled jobs for this world.
    /// </summary>
    internal sealed partial class World : IWorldWorkerApi
    {
        /// <summary>
        /// Execute all jobs currently scheduled on the per-world worker.
        /// </summary>
        /// <returns>The number of jobs executed.</returns>
        public int RunScheduledJobs() => _worker.RunScheduledJobs(this);
    }
}