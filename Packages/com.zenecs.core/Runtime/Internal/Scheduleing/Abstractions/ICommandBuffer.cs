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
using ZenECS.Core.Internal.Scheduling;

namespace ZenECS.Core
{
    /// <summary>
    /// Controls how a <see cref="CommandBuffer"/> is applied when disposed or explicitly flushed.
    /// </summary>
    public enum CommandBufferApplyMode
    {
        /// <summary>
        /// Queue this buffer to be applied at the next frame barrier.
        /// Use for background threads or when you want deterministic, batched commits.
        /// </summary>
        Schedule = 0,

        /// <summary>
        /// Apply this buffer immediately on dispose (or when explicitly ended).
        /// Recommended from the main thread only, to minimize contention.
        /// </summary>
        Immediate = 1,
    }
    
    public interface ICommandBuffer : System.IDisposable
    {
        void AddComponent<T>(Entity e, in T v) where T : struct;
        void ReplaceComponent<T>(Entity e, in T v) where T : struct;
        void RemoveComponent<T>(Entity e) where T : struct;
        void DespawnEntity(Entity e);
    }

    internal interface ICommandBufferInternal
    {
        void Bind(IWorld w, CommandBufferApplyMode mode);
    }
}