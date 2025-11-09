// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Command Buffer API
// File: IWorldCommandBufferApi.cs
// Purpose: Frame-barrier-friendly recording and application of world mutations.
// Key concepts:
//   • Apply modes: schedule at barrier vs immediate apply on dispose.
//   • Threading: record off-thread; schedule for main-thread safe application.
//   • Determinism: batch commits to precise points in the frame.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core
{
    /// <summary>Controls how a <see cref="CommandBuffer"/> is applied.</summary>
    public enum CommandBufferApplyMode
    {
        /// <summary>Queue to apply at the next safe frame barrier.</summary>
        Schedule = 0,

        /// <summary>Apply immediately (typically from the main thread).</summary>
        Immediate = 1,
    }

    /// <summary>
    /// Minimal command buffer contract used by world command APIs.
    /// </summary>
    public interface ICommandBuffer : System.IDisposable
    {
        /// <summary>The world this buffer is bound to (null after disposal).</summary>
        IWorld? WorldRef { get; }
        
        /// <summary>Add a component (no-op if present policies forbid).</summary>
        void AddComponent<T>(Entity e, in T v) where T : struct;

        /// <summary>Replace or set a component value.</summary>
        void ReplaceComponent<T>(Entity e, in T v) where T : struct;

        /// <summary>Remove a component.</summary>
        void RemoveComponent<T>(Entity e) where T : struct;

        /// <summary>Despawn an entity.</summary>
        void DespawnEntity(Entity e);
    }

    /// <summary>
    /// Begin/apply/schedule command buffers bound to a world.
    /// </summary>
    public interface IWorldCommandBufferApi
    {
        /// <summary>Begin a new buffer with the chosen apply mode.</summary>
        ICommandBuffer BeginWrite(CommandBufferApplyMode mode = CommandBufferApplyMode.Schedule);

        /// <summary>Flush/apply the buffer immediately and return applied op count.</summary>
        int EndWrite(ICommandBuffer cb);

        /// <summary>Schedule the buffer to run at the next frame barrier.</summary>
        void Schedule(ICommandBuffer? cb);

        /// <summary>Clear all scheduled buffers/jobs for this frame/world.</summary>
        void ClearAllCommandBuffers();
    }
}
