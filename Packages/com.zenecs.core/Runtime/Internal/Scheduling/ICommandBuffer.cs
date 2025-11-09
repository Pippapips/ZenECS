// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: ICommandBuffer.cs
// Purpose: Buffered structural commands (Add/Replace/Remove/Despawn) with
//          world-bound apply semantics (Immediate or Scheduled).
// Key concepts:
//   • Using-scope creation via World.BeginWrite(...)
//   • Auto-apply on Dispose according to apply mode
//   • Also exposed as IJob to the world's worker for barrier execution
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Internal.Scheduling
{
    /// <summary>
    /// Internal binder used by the world to attach a buffer to a specific world
    /// and choose the apply mode.
    /// </summary>
    internal interface ICommandBufferInternal
    {
        /// <summary>Bind this buffer to a world and apply mode.</summary>
        void Bind(IWorld w, CommandBufferApplyMode mode);
    }
}