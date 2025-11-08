﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.CommandBuffer.cs
// Purpose: Thread-safe command buffer for deferred or immediate structural changes.
// Key concepts:
//   • ConcurrentQueue of operations; Scheduled vs Immediate apply modes.
//   • RunScheduledJobs to commit at frame boundaries.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System.Collections.Concurrent;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    public sealed partial class WorldOld
    {
        // Multithreaded command buffer + scheduling example:
        // var cb = world.BeginWrite();
        // cb.Add(e, new Damage { Amount = 10 });
        // cb.Remove<Stunned>(e);
        // world.Schedule(cb);     // Applied at the frame barrier when world.RunScheduledJobs() is called
        // Or call world.EndWrite(cb); to apply immediately.


        /// <summary>
        /// Thread-safe buffer of structural ECS operations (Add/Replace/Remove/Destroy) that can be
        /// applied later as a job (scheduled) or flushed immediately on the main thread.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use <see cref="BeginWrite(ApplyMode)"/> to obtain a buffer. Enqueue operations via
        /// <see cref="Add{T}(Entity, in T)"/>, <see cref="Replace{T}(Entity, in T)"/>,
        /// <see cref="Remove{T}(Entity)"/>, and <see cref="Destroy(Entity)"/>.
        /// </para>
        /// <para>
        /// When the buffer is disposed, it is either scheduled or immediately applied depending on the
        /// <see cref="ApplyMode"/> it was created with. You can also explicitly call
        /// <see cref="WorldOld.EndWrite(CommandBuffer)"/> or <see cref="WorldOld.Schedule(CommandBuffer?)"/>.
        /// </para>
        /// </remarks>


    }
}
