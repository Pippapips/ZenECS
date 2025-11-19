// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: CommandBuffer.cs
// Purpose: Deferred structural command queue with barrier-only apply semantics.
// Key concepts:
//   • Systems record structural/value ops into a buffer, never mutate world directly
//   • Buffers are scheduled and applied only at deterministic tick barriers
//   • Safe no-op when target entity is already dead at apply time
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using ZenECS.Core.Internal.Scheduling;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Buffered structural command queue. Instances are created via
    /// <c>IWorldCommandBufferApi.BeginWrite()</c> and scheduled for execution
    /// at a deterministic barrier when disposed.
    /// <para>
    /// Commands recorded into this buffer never apply immediately; they are
    /// applied only when the worker executes this job at a tick boundary.
    /// </para>
    /// </summary>
    internal sealed class CommandBuffer : IJob, ICommandBuffer
    {
        private readonly IWorld _world;
        private readonly IWorker _worker;
        private readonly ConcurrentQueue<IOp> _q = new();
        private bool _disposed;

        public CommandBuffer(IWorld world, IWorker worker)
        {
            _world = world;
            _worker = worker;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Defer execution to the worker; it will run this job at a barrier.
            _worker.Schedule(this);
        }

        // ──────────────────────────────────────────────────────────────────
        // Entity lifecycle
        // ──────────────────────────────────────────────────────────────────

        public void EndWrite()
        {
            Dispose();
        }
        
        /// <inheritdoc/>
        public Entity SpawnEntity()
        {
            // World는 internal sealed partial 이고, 같은 어셈블리라 캐스트 가능
            if (_world is not World world)
                throw new InvalidOperationException("CommandBuffer expects a World instance.");

            // 아직 alive 아님. SpawnOp가 배리어에서 SpawnReserved를 호출.
            var e = world.ReserveEntity();
            _q.Enqueue(new SpawnOp(e));
            return e;
        }

        /// <inheritdoc/>
        public void DespawnEntity(Entity e)
            => _q.Enqueue(new DestroyOp(e));

        // ──────────────────────────────────────────────────────────────────
        // Component operations
        // ──────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void AddComponent<T>(Entity e, in T v) where T : struct
            => _q.Enqueue(new AddOp<T>(e, v));

        public void AddComponentBoxed(Entity e, object? boxed)
            => _q.Enqueue(new AddBoxedOp(e, boxed));

        /// <inheritdoc/>
        public void ReplaceComponent<T>(Entity e, in T v) where T : struct
            => _q.Enqueue(new ReplaceOp<T>(e, v));

        public void ReplaceComponentBoxed(Entity e, object? boxed)
            => _q.Enqueue(new ReplaceBoxedOp(e, boxed));

        /// <inheritdoc/>
        public void RemoveComponent<T>(Entity e) where T : struct
            => _q.Enqueue(new RemoveOp<T>(e));

        public void RemoveComponentTyped(Entity e, Type type)
            => _q.Enqueue(new RemoveTypedOp(e, type));

        // ──────────────────────────────────────────────────────────────────
        // Singleton operations
        // ──────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void SetSingleton<T>(in T value)
            where T : struct, IWorldSingletonComponent
        {
            _q.Enqueue(new SetSingletonOp<T>(value));
        }

        /// <inheritdoc/>
        public void RemoveSingleton<T>()
            where T : struct, IWorldSingletonComponent
        {
            _q.Enqueue(new RemoveSingletonOp<T>());
        }

        // ──────────────────────────────────────────────────────────────────
        // IJob
        // ──────────────────────────────────────────────────────────────────

        void IJob.Execute(IWorld w)
        {
            while (_q.TryDequeue(out var op))
                op.Apply(w);
        }

        // ──────────────────────────────────────────────────────────────────
        // Concrete ops
        // ──────────────────────────────────────────────────────────────────

        private interface IOp
        {
            void Apply(IWorld w);
        }

        private sealed class AddOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;

            public AddOp(Entity e, in T v)
            {
                _e = e;
                _v = v;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;
                world.AddComponent(_e, in _v);
            }
        }

        private sealed class AddBoxedOp : IOp
        {
            private readonly Entity _e;
            private readonly object? _boxed;

            public AddBoxedOp(Entity e, object? boxed)
            {
                _boxed = boxed;
                _e = e;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;
                world.AddComponentBoxed(_e, _boxed);
            }
        }

        private sealed class ReplaceOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;

            public ReplaceOp(Entity e, in T v)
            {
                _e = e;
                _v = v;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;
                world.ReplaceComponent(_e, in _v);
            }
        }

        private sealed class ReplaceBoxedOp : IOp
        {
            private readonly Entity _e;
            private readonly object? _boxed;

            public ReplaceBoxedOp(Entity e, object? boxed)
            {
                _e = e;
                _boxed = boxed;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;
                world.ReplaceComponentBoxed(_e, _boxed);
            }
        }

        private sealed class RemoveOp<T> : IOp where T : struct
        {
            private readonly Entity _e;

            public RemoveOp(Entity e)
            {
                _e = e;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.RemoveComponent<T>(_e);
            }
        }

        private sealed class RemoveTypedOp : IOp
        {
            private readonly Entity _e;
            private readonly Type _type;

            public RemoveTypedOp(Entity e, Type type)
            {
                _e = e;
                _type = type;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;
                world.RemoveComponentTyped(_e, _type);
            }
        }

        private sealed class SpawnOp : IOp
        {
            private readonly Entity _e;

            public SpawnOp(Entity e)
            {
                _e = e;
            }

            public void Apply(IWorld w)
            {
                // World로 캐스트해서 SpawnReserved 호출
                if (w is not World world) return;
                world.SpawnReserved(_e);
            }
        }

        private sealed class DestroyOp : IOp
        {
            private readonly Entity _e;

            public DestroyOp(Entity e)
            {
                _e = e;
            }

            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.DespawnEntity(_e);
            }
        }

        private sealed class SetSingletonOp<T> : IOp
            where T : struct, IWorldSingletonComponent
        {
            private readonly T _value;

            public SetSingletonOp(in T value)
            {
                _value = value;
            }

            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                // "싱글톤은 항상 전용 엔티티에만 붙는다" 규칙 하에서,
                // 내부 SetSingleton 구현은:
                //  • 존재하면 값 교체
                //  • 없으면 전용 엔티티 생성 후 부착
                world.SetSingleton(_value);
            }
        }

        private sealed class RemoveSingletonOp<T> : IOp
            where T : struct, IWorldSingletonComponent
        {
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                // 내부 RemoveSingleton 구현은:
                //  • 싱글톤 엔티티가 있다면 전용 엔티티 통째로 Despawn
                //  • 없다면 no-op
                world.RemoveSingleton<T>();
            }
        }
    }
}
