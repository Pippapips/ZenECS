// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: CommandBuffer.cs
// Purpose: Buffered structural command queue with auto-apply semantics.
// Key concepts:
//   • Enqueue structural ops; apply immediately or schedule at frame barrier
//   • Implements IJob for worker integration
//   • Safe no-op when target entity is already dead at apply time
// License: MIT
// © 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core.Internal.Scheduling
{
    /// <summary>
    /// Buffered structural command queue. Instances are typically created via
    /// <c>world.BeginWrite(mode)</c> and auto-applied on <see cref="Dispose"/>.
    /// </summary>
    internal sealed class CommandBuffer : IJob, ICommandBuffer, ICommandBufferInternal
    {
        /// <summary>Internal operation contract for queued structural changes.</summary>
        internal interface IOp { void Apply(IWorld w); }

        internal readonly ConcurrentQueue<IOp> Q = new();

        // Bound in Bind(...)
        private IWorld? _boundWorld;
        private CommandBufferApplyMode _mode;
        private bool _disposed;

        /// <inheritdoc/>
        public IWorld? WorldRef => _boundWorld;

        /// <inheritdoc/>
        public void Bind(IWorld w, CommandBufferApplyMode mode)
        {
            _boundWorld = w;
            _mode = mode;
            _disposed = false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var w = _boundWorld;
            _boundWorld = null;
            if (w == null) return;

            if (_mode == CommandBufferApplyMode.Immediate)
                w.EndWrite(this);     // apply immediately
            else
                w.Schedule(this);     // schedule for barrier
        }

        // ---- Enqueue ops -----------------------------------------------------

        /// <inheritdoc/>
        public void AddComponent<T>(Entity e, in T v) where T : struct
            => Q.Enqueue(new AddOp<T>(e, v));

        /// <inheritdoc/>
        public void ReplaceComponent<T>(Entity e, in T v) where T : struct
            => Q.Enqueue(new ReplaceOp<T>(e, v));

        /// <inheritdoc/>
        public void RemoveComponent<T>(Entity e) where T : struct
            => Q.Enqueue(new RemoveOp<T>(e));

        /// <inheritdoc/>
        public void DespawnEntity(Entity e)
            => Q.Enqueue(new DestroyOp(e));

        // ---- IJob ------------------------------------------------------------

        void IJob.Execute(IWorld w)
        {
            while (Q.TryDequeue(out var op))
                op.Apply(w);
        }

        // ---- Concrete ops ----------------------------------------------------

        private sealed class AddOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;
            public AddOp(Entity e, in T v) { _e = e; _v = v; }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                w.AddComponent(_e, in _v);
            }
        }

        private sealed class ReplaceOp<T> : IOp where T : struct
        {
            private readonly Entity _e; private readonly T _v;
            public ReplaceOp(Entity e, in T v) { _e = e; _v = v; }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                w.ReplaceComponent(_e, in _v);
            }
        }

        private sealed class RemoveOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            public RemoveOp(Entity e) { _e = e; }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                w.RemoveComponent<T>(_e);
            }
        }

        private sealed class DestroyOp : IOp
        {
            private readonly Entity _e;
            public DestroyOp(Entity e) { _e = e; }
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                w.DespawnEntity(_e);
            }
        }
    }
}
