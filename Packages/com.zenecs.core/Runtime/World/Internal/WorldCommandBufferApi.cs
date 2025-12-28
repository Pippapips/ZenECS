// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Command Buffer API)
// File: WorldCommandBufferApi.cs
// Purpose: Frame-barrier-friendly command recording & application.
// Key concepts:
//   • using-scope buffers: record operations, applied at controlled barriers.
//   • Scheduler integration: deferred jobs flushed at safe simulation points.
//   • Safety on reset: pending jobs are drained to avoid dropping work.
//   • External commands: integrate out-of-band requests into deterministic buffers.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldCommandBufferApi"/> for recording and applying
    /// world mutations through command buffers and external command queues.
    /// </summary>
    internal sealed partial class World : IWorldCommandBufferApi
    {
        /// <summary>
        /// Queue of external commands awaiting translation into command-buffer
        /// operations at the next flush point.
        /// </summary>
        private readonly List<ExternalCommand> _pendingExternalCommands = new();

        /// <summary>
        /// Begins a command-buffer write scope bound to this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned <see cref="ICommandBuffer"/> does not apply mutations
        /// immediately; it only records operations that will later be applied at
        /// a deterministic barrier by the worker/scheduler.
        /// </para>
        /// </remarks>
        /// <returns>A new <see cref="ICommandBuffer"/> bound to this world.</returns>
        public ICommandBuffer BeginWrite()
        {
            return new CommandBuffer(this, _worker);
        }

        /// <summary>
        /// Clears all pending frame-local command buffers by flushing the
        /// underlying scheduler queue.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is typically used during reset to ensure that no stale jobs or
        /// delayed command-buffer applications remain queued.
        /// </para>
        /// </remarks>
        private void ClearAllCommandBuffers()
        {
            _worker.ClearAllScheduledJobs();
        }

        /// <inheritdoc/>
        public int ExternalCommandCount => _pendingExternalCommands.Count;

        /// <inheritdoc/>
        public bool HasExternalCommand => _pendingExternalCommands.Count > 0;

        /// <inheritdoc/>
        public void ExternalCommandEnqueue(ExternalCommand command)
        {
            _pendingExternalCommands.Add(command);
        }

        /// <inheritdoc/>
        public void ExternalCommandClear()
        {
            _pendingExternalCommands.Clear();
        }

        /// <summary>
        /// Flushes all pending external commands into a command buffer and applies them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method translates each <see cref="ExternalCommand"/> into concrete
        /// <see cref="ICommandBuffer"/> calls and then applies them, ensuring that
        /// out-of-band requests (e.g. from networking, UI, or tools) are integrated
        /// into the deterministic simulation pipeline at a safe barrier.
        /// </para>
        /// </remarks>
        internal void ExternalCommandFlushTo()
        {
            if (_pendingExternalCommands.Count == 0)
                return;

            var cmdBuffer = BeginWrite();
            try
            {
                foreach (var externalCommand in _pendingExternalCommands)
                {
                    ApplySingle(externalCommand, cmdBuffer);
                }
            }
            finally
            {
                cmdBuffer.EndWrite();
                RunScheduledJobs();
                ExternalCommandClear();
            }
        }

        /// <summary>
        /// Applies a single external command to the provided command buffer.
        /// </summary>
        /// <param name="cmd">External command to translate.</param>
        /// <param name="buffer">Command buffer that records the resulting operations.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cmd"/> has an unknown
        /// <see cref="ExternalCommand.Kind"/> value.
        /// </exception>
        private static void ApplySingle(in ExternalCommand cmd, ICommandBuffer buffer)
        {
            switch (cmd.Kind)
            {
                case ExternalCommandKind.CreateEntity:
                {
                    var e = buffer.CreateEntity();
                    cmd.CreatedCallback?.Invoke(e, buffer);
                    break;
                }

                case ExternalCommandKind.DestroyEntity:
                {
                    buffer.DestroyEntity(cmd.Entity);
                    break;
                }

                case ExternalCommandKind.AddComponent:
                {
                    // Uses ICommandBuffer.AddComponentBoxed(Entity, object?)
                    buffer.AddComponentBoxed(cmd.Entity, cmd.ComponentBoxed);
                    break;
                }

                case ExternalCommandKind.ReplaceComponent:
                {
                    // Uses ICommandBuffer.ReplaceComponentBoxed(Entity, object?)
                    buffer.ReplaceComponentBoxed(cmd.Entity, cmd.ComponentBoxed);
                    break;
                }

                case ExternalCommandKind.RemoveComponent:
                {
                    // Uses ICommandBuffer.RemoveComponentTyped(Entity, Type)
                    if (cmd.ComponentType is not null)
                    {
                        buffer.RemoveComponentTyped(cmd.Entity, cmd.ComponentType);
                    }
                    break;
                }

                case ExternalCommandKind.SetSingleton:
                {
                    if (cmd.ComponentType is not null)
                    {
                        buffer.SetSingletonTyped(cmd.ComponentType, cmd.ComponentBoxed);
                    }
                    break;
                }

                case ExternalCommandKind.RemoveSingleton:
                {
                    if (cmd.ComponentType is not null)
                    {
                        buffer.RemoveSingletonTyped(cmd.ComponentType);
                    }
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cmd.Kind),
                        cmd.Kind,
                        "Unknown external command kind.");
            }
        }
    }
}
