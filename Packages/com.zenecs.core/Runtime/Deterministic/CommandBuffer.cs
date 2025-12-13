// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Scheduling
// File: CommandBuffer.cs
// Purpose: Deferred structural command queue with barrier-only apply semantics.
// Key concepts:
//   • Systems record structural/value ops into a buffer, never mutate world directly.
//   • Buffers are scheduled and applied only at deterministic tick barriers.
//   • Safe no-op when target entity is already dead at apply time.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using ZenECS.Core.Scheduling.Internal;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Buffered structural command queue bound to a single world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances are created via <c>IWorldCommandBufferApi.BeginWrite()</c> and
    /// scheduled for execution at a deterministic barrier when
    /// <see cref="Dispose"/> or <see cref="EndWrite"/> is called.
    /// </para>
    /// <para>
    /// Commands recorded into this buffer never apply immediately; they are
    /// applied only when the worker executes this job at a tick boundary.
    /// This ensures deterministic ordering of structural and value changes.
    /// </para>
    /// </remarks>
    internal sealed class CommandBuffer : IJob, ICommandBuffer
    {
        private readonly IWorld _world;
        private readonly IWorker _worker;
        private readonly ConcurrentQueue<IOp> _q = new();
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="CommandBuffer"/> bound to the given world and worker.
        /// </summary>
        /// <param name="world">The world that will receive the recorded commands.</param>
        /// <param name="worker">
        /// Worker responsible for executing this buffer as a job at a barrier.
        /// </param>
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
        // Write lifecycle
        // ──────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void EndWrite()
        {
            // Ending the write phase simply defers to Dispose, which schedules
            // this buffer as a job to be executed at the next barrier.
            Dispose();
        }

        // ──────────────────────────────────────────────────────────────────
        // Entity lifecycle
        // ──────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public Entity CreateEntity()
        {
            // World is an internal sealed partial and lives in the same assembly,
            // so we can downcast to access the internal reserve/create APIs.
            if (_world is not World world)
                throw new InvalidOperationException("CommandBuffer expects a World instance.");

            // The entity is only reserved at this point; it becomes alive when
            // the CreateOp is applied at the barrier.
            var e = world.ReserveEntity();
            _q.Enqueue(new CreateOp(e));
            return e;
        }

        /// <inheritdoc/>
        public void DestroyEntity(Entity e)
            => _q.Enqueue(new DestroyOp(e));

        // ──────────────────────────────────────────────────────────────────
        // Component operations
        // ──────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void AddComponent<T>(Entity e, in T v) where T : struct
            => _q.Enqueue(new AddOp<T>(e, v));

        /// <inheritdoc/>
        public void AddComponentBoxed(Entity e, object? boxed)
            => _q.Enqueue(new AddBoxedOp(e, boxed));

        /// <inheritdoc/>
        public void ReplaceComponent<T>(Entity e, in T v) where T : struct
            => _q.Enqueue(new ReplaceOp<T>(e, v));

        /// <inheritdoc/>
        public void ReplaceComponentBoxed(Entity e, object? boxed)
            => _q.Enqueue(new ReplaceBoxedOp(e, boxed));

        /// <inheritdoc/>
        public void RemoveComponent<T>(Entity e) where T : struct
            => _q.Enqueue(new RemoveOp<T>(e));

        /// <inheritdoc/>
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
        public void SetSingletonTyped(Type type, object? boxed)
        {
            _q.Enqueue(new SetSingletonTypedOp(type, boxed));
        }

        /// <inheritdoc/>
        public void RemoveSingleton<T>()
            where T : struct, IWorldSingletonComponent
        {
            _q.Enqueue(new RemoveSingletonOp<T>());
        }

        /// <inheritdoc/>
        public void RemoveSingletonTyped(Type type)
        {
            _q.Enqueue(new RemoveSingletonTypedOp(type));
        }

        /// <inheritdoc/>
        public void DestroyAllEntities()
        {
            _q.Enqueue(new DestroyAllEntitiesOp());
        }

        // ──────────────────────────────────────────────────────────────────
        // IJob
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes all recorded operations against the target world.
        /// </summary>
        /// <param name="w">The world for which this job is being executed.</param>
        void IJob.Execute(IWorld w)
        {
            while (_q.TryDequeue(out var op))
                op.Apply(w);
        }

        // ──────────────────────────────────────────────────────────────────
        // Concrete ops
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Internal command interface for queued operations.
        /// </summary>
        /// <remarks>
        /// All structural operations (create/destroy entities, add/remove/replace
        /// components, singleton operations) are represented as command objects
        /// implementing this interface. Commands are queued during recording and
        /// applied at a deterministic barrier when the buffer is executed.
        /// </remarks>
        private interface IOp
        {
            /// <summary>
            /// Applies the command to the given world.
            /// </summary>
            /// <param name="w">Target world.</param>
            /// <remarks>
            /// Implementations should check if the target entity is still alive
            /// before applying the operation, as entities may be destroyed between
            /// command recording and execution.
            /// </remarks>
            void Apply(IWorld w);
        }

        /// <summary>
        /// Command that adds a component of type <typeparamref name="T"/> to an entity.
        /// </summary>
        private sealed class AddOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;

            /// <summary>
            /// Creates a new add-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            /// <param name="v">Component value to add.</param>
            public AddOp(Entity e, in T v)
            {
                _e = e;
                _v = v;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.AddComponent(_e, in _v);
            }
        }

        /// <summary>
        /// Command that adds a boxed component instance to an entity.
        /// </summary>
        private sealed class AddBoxedOp : IOp
        {
            private readonly Entity _e;
            private readonly object? _boxed;

            /// <summary>
            /// Creates a new add-boxed-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            /// <param name="boxed">Boxed component value.</param>
            public AddBoxedOp(Entity e, object? boxed)
            {
                _boxed = boxed;
                _e = e;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.AddComponentBoxed(_e, _boxed);
            }
        }

        /// <summary>
        /// Command that replaces a component value on an entity.
        /// </summary>
        private sealed class ReplaceOp<T> : IOp where T : struct
        {
            private readonly Entity _e;
            private readonly T _v;

            /// <summary>
            /// Creates a new replace-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            /// <param name="v">New component value.</param>
            public ReplaceOp(Entity e, in T v)
            {
                _e = e;
                _v = v;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.ReplaceComponent(_e, in _v);
            }
        }

        /// <summary>
        /// Command that replaces a component value on an entity using a boxed value.
        /// </summary>
        private sealed class ReplaceBoxedOp : IOp
        {
            private readonly Entity _e;
            private readonly object? _boxed;

            /// <summary>
            /// Creates a new replace-boxed-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            /// <param name="boxed">Boxed component value.</param>
            public ReplaceBoxedOp(Entity e, object? boxed)
            {
                _e = e;
                _boxed = boxed;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.ReplaceComponentBoxed(_e, _boxed);
            }
        }

        /// <summary>
        /// Command that removes a component of type <typeparamref name="T"/> from an entity.
        /// </summary>
        private sealed class RemoveOp<T> : IOp where T : struct
        {
            private readonly Entity _e;

            /// <summary>
            /// Creates a new remove-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            public RemoveOp(Entity e)
            {
                _e = e;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.RemoveComponent<T>(_e);
            }
        }

        /// <summary>
        /// Command that removes a component by runtime type from an entity.
        /// </summary>
        private sealed class RemoveTypedOp : IOp
        {
            private readonly Entity _e;
            private readonly Type _type;

            /// <summary>
            /// Creates a new typed remove-component command.
            /// </summary>
            /// <param name="e">Target entity.</param>
            /// <param name="type">Component type to remove.</param>
            public RemoveTypedOp(Entity e, Type type)
            {
                _e = e;
                _type = type;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.RemoveComponentTyped(_e, _type);
            }
        }

        /// <summary>
        /// Command that finalizes creation of an entity previously reserved by the world.
        /// </summary>
        private sealed class CreateOp : IOp
        {
            private readonly Entity _e;

            /// <summary>
            /// Creates a new entity-create command.
            /// </summary>
            /// <param name="e">Reserved entity handle.</param>
            public CreateOp(Entity e)
            {
                _e = e;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                world.CreateReserved(_e);
            }
        }

        /// <summary>
        /// Command that destroys an entity if it is still alive.
        /// </summary>
        private sealed class DestroyOp : IOp
        {
            private readonly Entity _e;

            /// <summary>
            /// Creates a new entity-destroy command.
            /// </summary>
            /// <param name="e">Entity to destroy.</param>
            public DestroyOp(Entity e)
            {
                _e = e;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (!w.IsAlive(_e)) return;
                if (w is not World world) return;

                world.DestroyEntity(_e);
            }
        }

        /// <summary>
        /// Command that sets or replaces a strongly-typed singleton component.
        /// </summary>
        private sealed class SetSingletonOp<T> : IOp
            where T : struct, IWorldSingletonComponent
        {
            private readonly T _value;

            /// <summary>
            /// Creates a new singleton-set command.
            /// </summary>
            /// <param name="value">Singleton value to assign.</param>
            public SetSingletonOp(in T value)
            {
                _value = value;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                // Internal SetSingleton implementation is expected to:
                //  • Replace value if singleton exists.
                //  • Otherwise create a dedicated entity and attach the singleton.
                world.SetSingleton(_value);
            }
        }

        /// <summary>
        /// Command that sets or replaces a singleton component based on runtime type.
        /// </summary>
        private sealed class SetSingletonTypedOp : IOp
        {
            private readonly Type _type;
            private readonly object? _boxed;

            /// <summary>
            /// Creates a new typed singleton-set command.
            /// </summary>
            /// <param name="type">Singleton component type.</param>
            /// <param name="boxed">Boxed singleton value.</param>
            public SetSingletonTypedOp(Type type, object? boxed)
            {
                _type = type;
                _boxed = boxed;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                world.SetSingletonTyped(_type, _boxed);
            }
        }

        /// <summary>
        /// Command that removes a strongly-typed singleton component.
        /// </summary>
        private sealed class RemoveSingletonOp<T> : IOp
            where T : struct, IWorldSingletonComponent
        {
            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                // Internal RemoveSingleton implementation is expected to:
                //  • Despawn the dedicated singleton entity if it exists.
                //  • Otherwise act as a no-op.
                world.RemoveSingleton<T>();
            }
        }

        /// <summary>
        /// Command that removes a singleton component based on runtime type.
        /// </summary>
        private sealed class RemoveSingletonTypedOp : IOp
        {
            private readonly Type _type;

            /// <summary>
            /// Creates a new typed singleton-remove command.
            /// </summary>
            /// <param name="type">Singleton component type.</param>
            public RemoveSingletonTypedOp(Type type)
            {
                _type = type;
            }

            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                world.RemoveSingletonTyped(_type);
            }
        }

        /// <summary>
        /// Command that despawns all alive entities in the world.
        /// </summary>
        private sealed class DestroyAllEntitiesOp : IOp
        {
            /// <inheritdoc/>
            public void Apply(IWorld w)
            {
                if (w is not World world) return;

                // Internal DestroyAllEntities implementation is expected to:
                //  • Despawn every alive entity (including singletons).
                //  • Fire all associated events and binder notifications.
                world.DestroyAllEntities();
            }
        }
    }
}
