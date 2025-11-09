// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: Worker.cs
// Purpose: FIFO job runner used by the world to execute scheduled work.
// Key concepts:
//   • Thread-safe queue; Execute jobs on caller thread
//   • Barrier-friendly: invoke RunScheduledJobs() at a deterministic point
//   • ClearAllScheduledJobs() for teardown or scene changes
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core.Internal.Scheduling
{
    /// <summary>
    /// Default <see cref="IWorker"/> implementation backed by a thread-safe queue.
    /// </summary>
    internal sealed class Worker : IWorker
    {
        private readonly ConcurrentQueue<IJob> _jobQueue = new();

        /// <inheritdoc/>
        public void Schedule(IJob? job)
        {
            if (job != null) _jobQueue.Enqueue(job);
        }

        /// <inheritdoc/>
        public int RunScheduledJobs(IWorld w)
        {
            int n = 0;
            while (_jobQueue.TryDequeue(out var j))
            {
                j.Execute(w);
                n++;
            }
            return n;
        }

        /// <inheritdoc/>
        public void ClearAllScheduledJobs()
        {
            _jobQueue.Clear();
        }
    }
}