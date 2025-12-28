// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: Worker.cs
// Purpose: FIFO job runner used by the world to execute scheduled work.
// Key concepts:
//   • Thread-safe queue; Execute jobs on caller thread
//   • Barrier-friendly: invoke RunScheduledJobs() at a deterministic point
//   • ClearAllScheduledJobs() for teardown or scene changes
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core.Scheduling.Internal
{
    /// <summary>
    /// Default <see cref="IWorker"/> implementation backed by a thread-safe FIFO queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Jobs are scheduled from any thread via <see cref="Schedule"/> and executed on
    /// the thread that calls <see cref="RunScheduledJobs"/>. This makes it suitable
    /// as a world-local barrier runner for deferred work such as command buffers.
    /// </para>
    /// </remarks>
    internal sealed class Worker : IWorker
    {
        /// <summary>
        /// Internal queue storing scheduled jobs in FIFO order.
        /// </summary>
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