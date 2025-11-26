// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Editor Integration
// File: ExternalEditorCommandQueue.cs
// Purpose: Per-world queue for editor-driven structural changes, flushed via
//          ICommandBuffer at a deterministic barrier (e.g. FixedUpdate).
// Notes:
//   • EcsExplorer and other tools enqueue EditorCommand instances here
//   • World driver / kernel calls FlushTo(...) at a safe time slice
//   • Uses boxed/typed ICommandBuffer APIs to avoid reflection in tools layer
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core;

namespace ZenECS.EditorCommands
{
    /// <summary>
    /// Holds editor-originated mutation requests for a single world and applies
    /// them via <see cref="ICommandBuffer"/> when instructed.
    /// </summary>
    public sealed class ExternalEditorCommandQueue
    {
        private readonly List<EditorCommand> _pending = new();

        /// <summary>
        /// Gets the number of pending commands.
        /// </summary>
        public int Count => _pending.Count;

        /// <summary>
        /// Returns <c>true</c> if there is at least one pending command.
        /// </summary>
        public bool HasPending => _pending.Count > 0;

        /// <summary>
        /// Enqueue a new editor command to be applied later.
        /// </summary>
        public void Enqueue(EditorCommand command)
        {
            _pending.Add(command);
        }

        /// <summary>
        /// Clear all pending commands without applying them.
        /// </summary>
        public void Clear()
        {
            _pending.Clear();
        }

        /// <summary>
        /// Flush all pending commands into the specified world using its
        /// <see cref="IWorldCommandBufferApi"/> implementation.
        /// <para>
        /// This should only be called from a safe structural mutation window
        /// (e.g. world tick barrier / FixedUpdate), never from within systems.
        /// </para>
        /// </summary>
        /// <param name="world">Target world.</param>
        public void FlushTo(IWorld world)
        {
            if (world is null)
                throw new ArgumentNullException(nameof(world));

            if (_pending.Count == 0)
                return;

            // World must expose a command buffer API; in ZenECS, IWorld also
            // implements IWorldCommandBufferApi, typically via a Commands facade.
            using var buffer = world.BeginWrite();

            try
            {
                for (int i = 0; i < _pending.Count; i++)
                {
                    ApplySingle(_pending[i], buffer);
                }
            }
            finally
            {
                buffer.EndWrite();
                _pending.Clear();
            }
        }

        private static void ApplySingle(in EditorCommand cmd, ICommandBuffer buffer)
        {
            switch (cmd.Kind)
            {
                case EditorCommandKind.SpawnEntity:
                {
                    var e = buffer.SpawnEntity();
                    cmd.SpawnCallback?.Invoke(e);
                    break;
                }

                case EditorCommandKind.DespawnEntity:
                {
                    buffer.DespawnEntity(cmd.Entity);
                    break;
                }

                case EditorCommandKind.AddComponent:
                {
                    // Uses ICommandBuffer.AddComponentBoxed(Entity, object?)
                    buffer.AddComponentBoxed(cmd.Entity, cmd.ComponentBoxed);
                    break;
                }

                case EditorCommandKind.ReplaceComponent:
                {
                    // Uses ICommandBuffer.ReplaceComponentBoxed(Entity, object?)
                    buffer.ReplaceComponentBoxed(cmd.Entity, cmd.ComponentBoxed);
                    break;
                }

                case EditorCommandKind.RemoveComponent:
                {
                    // Uses ICommandBuffer.RemoveComponentTyped(Entity, Type)
                    if (cmd.ComponentType is not null)
                    {
                        buffer.RemoveComponentTyped(cmd.Entity, cmd.ComponentType);
                    }
                    break;
                }

                case EditorCommandKind.SetSingleton:
                {
                    if (cmd.ComponentType is not null)
                    {
                        buffer.SetSingletonTyped(cmd.ComponentType, cmd.ComponentBoxed);
                    }
                    break;
                }

                case EditorCommandKind.RemoveSingleton:
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
                        "Unknown editor command kind.");
            }
        }
    }
}
