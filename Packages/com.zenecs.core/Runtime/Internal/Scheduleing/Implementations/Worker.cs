// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: MessageBus.cs
// Purpose: Thread-safe publish/subscribe message dispatcher for ECS systems.
// Key concepts:
//   • Struct-based messages, no boxing or allocations on Publish.
//   • Each message type maintains its own queue and subscriber list.
//   • PumpAll() flushes all message queues per frame (deterministic order).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.Scheduling
{
    internal sealed class Worker : IWorker
    {
        private readonly ConcurrentQueue<IJob> jobQueue = new();
        
        public void Schedule(IJob? job)
        {
            if (job != null)
            {
                jobQueue.Enqueue(job);
            }
        }

        public int RunScheduledJobs(IWorld w)
        {
            int n = 0;
            while (jobQueue.TryDequeue(out var j))
            {
                j.Execute(w);
                n++;
            }

            return n;
        }
        
        /// <summary>
        /// Clears all pending jobs without executing them.
        /// </summary>
        /// <remarks>
        /// Useful during hard resets or when discarding work between scene loads.
        /// </remarks>
        public void ClearAllScheduledJobs()
        {
            jobQueue.Clear();
        }
    }
}
