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
    internal sealed class CommandBuffer : IJob, ICommandBuffer, ICommandBufferInternal
    {
        internal readonly ConcurrentQueue<IOp> q = new();

        // Bound in BeginWrite
        private IWorld? _boundWorld;
        private CommandBufferApplyMode _mode;
        private bool _disposed;

        /// <summary>
        /// The world this buffer is currently bound to (set by <see cref="BeginWrite(WorldOld.ApplyMode)"/>).
        /// May be <see langword="null"/> after <see cref="Dispose"/> is called.
        /// </summary>
        public IWorld? WorldRef => _boundWorld;

        /// <summary>
        /// Binds this buffer to a specific <see cref="WorldOld"/> and applies the given <see cref="WorldOld.ApplyMode"/>.
        /// Intended for internal use by <see cref="WorldOld.BeginWrite(WorldOld.ApplyMode)"/>.
        /// </summary>
        public void Bind(IWorld w, CommandBufferApplyMode mode)
        {
            _boundWorld = w;
            _mode = mode;
            _disposed = false;
        }

        /// <summary>
        /// Disposes the buffer and either schedules or immediately applies it according to the
        /// <see cref="WorldOld.ApplyMode"/> that was used at creation time.
        /// </summary>
        /// <remarks>
        /// Buffers created via <see cref="WorldOld.BeginWrite(WorldOld.ApplyMode)"/> are auto-applied on dispose.
        /// Buffers created manually should be applied via <see cref="WorldOld.EndWrite(CommandBuffer)"/> or
        /// <see cref="WorldOld.Schedule(CommandBuffer?)"/>.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Auto-apply only for buffers created by using (BeginWrite)
            var w = _boundWorld;
            _boundWorld = null;

            if (w == null) return;

            if (_mode == CommandBufferApplyMode.Immediate)
                w.EndWrite(this); // Apply immediately
            else
                w.Schedule(this); // Apply at the barrier
        }

        /// <summary>
        /// Internal operation contract implemented by each queued structural change.
        /// </summary>
        internal interface IOp
        {
            /// <summary>
            /// Applies the operation against the provided <see cref="WorldOld"/>.
            /// Implementations should guard against dead entities.
            /// </summary>
            void Apply(IWorld w);
        }

        /// <summary>
        /// Enqueues an Add component operation for the given entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="v">Component value to add.</param>
        public void AddComponent<T>(Entity e, in T v) where T : struct => q.Enqueue(new AddOp<T>(e, v));

        /// <summary>
        /// Enqueues a Replace component operation for the given entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="v">New component value.</param>
        public void ReplaceComponent<T>(Entity e, in T v) where T : struct => q.Enqueue(new ReplaceOp<T>(e, v));

        /// <summary>
        /// Enqueues a Remove component operation for the given entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        public void RemoveComponent<T>(Entity e) where T : struct => q.Enqueue(new RemoveOp<T>(e));

        /// <summary>
        /// Enqueues a Destroy entity operation.
        /// </summary>
        /// <param name="e">The entity to destroy.</param>
        public void DespawnEntity(Entity e) => q.Enqueue(new DestroyOp(e));

        // IJob: integration with the world's scheduler
        void IJob.Execute(IWorld w)
        {
            while (q.TryDequeue(out var op)) op.Apply(w);
        }

        // ----- Concrete ops ---------------------------------------------------------

        sealed class AddOp<T> : IOp where T : struct
        {
            readonly Entity e;
            readonly T v;
            public AddOp(Entity e, in T v)
            {
                this.e = e;
                this.v = v;
            }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(e))
                {
                    /* w.Trace($"Skip Add<{typeof(T).Name}>: {e} dead"); */
                    return;
                }
                w.AddComponent(e, in v);
            }
        }

        private sealed class ReplaceOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;
            public ReplaceOp(Entity e, in T v)
            {
                this._e = e;
                this._v = v;
            }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e))
                {
                    /* w.Trace($"Skip Replace<{typeof(T).Name}>: {e} dead"); */
                    return;
                }
                // Route through World.Replace to align with hooks/validation/events.
                w.ReplaceComponent(_e, in _v);
            }
        }

        private sealed class RemoveOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            public RemoveOp(Entity e) { this._e = e; }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e))
                {
                    /* w.Trace($"Skip Remove<{typeof(T).Name}>: {e} dead"); */
                    return;
                }
                w.RemoveComponent<T>(_e);
            }
        }

        private sealed class DestroyOp : IOp
        {
            private readonly Entity _e;
            public DestroyOp(Entity e) { this._e = e; }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e))
                {
                    /* w.Trace($"Skip Destroy: {e} already dead"); */
                    return;
                }
                w.DespawnEntity(_e);
            }
        }
    }
}