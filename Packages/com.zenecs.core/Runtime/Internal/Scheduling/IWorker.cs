// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IMessageBus.cs
// Purpose: Defines a minimal message-passing interface for ECS systems.
// Key concepts:
//   • Lightweight publish/subscribe model for struct-based messages.
//   • PumpAll() delivers all queued messages to subscribers once per frame.
//   • Thread-safe by design for cross-system communication.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Internal.Scheduling
{
    internal interface IJob
    {
        void Execute(IWorld w);
    }
    
    internal interface IWorker
    {
        void Schedule(IJob? job);
        int RunScheduledJobs(IWorld w);
        void ClearAllScheduledJobs();
    }
}