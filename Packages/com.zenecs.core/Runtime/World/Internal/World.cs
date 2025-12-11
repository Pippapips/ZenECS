// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World
// File: World.cs
// Purpose: Concrete per-world runtime host (entities, components, systems, IO).
// Key concepts:
//   • DI-composed per-world services: runner, pools, router, contexts, bus.
//   • Lifetime wiring: subscribe to Kernel ticks; cleanly detach on dispose.
//   • Entity id/generation: stable handles; 0 is reserved as “null”.
//   • Pause per world: Kernel may step selected worlds only.
//   • Reset paths: fast capacity-preserving reset for bulk despawn.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Internal;
using ZenECS.Core.ComponentPooling.Internal;
using ZenECS.Core.Hooking.Internal;
using ZenECS.Core.Infrastructure.Internal;
using ZenECS.Core.Messaging.Internal;
using ZenECS.Core.Scheduling.Internal;
using ZenECS.Core.Systems.Internal;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Concrete world: owns entity storage, component pools, messaging, systems, and binding host.
    /// All services are resolved from the per-world DI <see cref="ServiceContainer"/>.
    /// </summary>
    internal sealed partial class World : IWorld
    {
        private readonly IKernel _kernel;
        private readonly ServiceContainer _scope;

        // Per-world services (resolved from _scope)
        private readonly ISystemRunner _runner;
        private readonly IPermissionHook _permissionHook;
        private readonly IBindingRouter _bindingRouter;
        private readonly IContextRegistry _contextRegistry;
        private readonly IComponentPoolRepository _componentPoolRepository;
        private readonly IMessageBus _bus;
        private readonly IWorker _worker;

        /// <inheritdoc/>
        public IKernel Kernel => _kernel;

        /// <inheritdoc/>
        public WorldId Id { get; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Tags { get; }

        /// <inheritdoc/>
        public long FrameCount { get; private set; }

        /// <inheritdoc/>
        public long Tick { get; private set; }

        /// <inheritdoc/>
        public bool IsPaused => _pause;

        /// <inheritdoc/>
        public bool IsDisposing => _disposed;

        private bool _pause;
        private bool _disposed;

        private readonly WorldConfig _cfg;

        /// <summary>
        /// Bitset of occupied entity slots (alive flags).
        /// </summary>
        private BitSet _alive;

        /// <summary>
        /// Next id to issue for newly created entities (1-based; 0 reserved).
        /// </summary>
        private int _nextId = 1;

        /// <summary>
        /// Recycled ids of destroyed entities (LIFO).
        /// </summary>
        private Stack<int> _freeIds;

        /// <summary>
        /// Per-slot generation counter preventing stale handles.
        /// </summary>
        private int[] _generation;

        /// <summary>
        /// Gets the current generation value for the given internal id.
        /// </summary>
        /// <param name="id">Internal entity id.</param>
        /// <returns>Generation value for the slot.</returns>
        public int GenerationOf(int id) => _generation[id];

        /// <summary>
        /// Construct a world with a configuration, identity metadata, and a DI scope.
        /// Subscribes to kernel tick events and composes per-world services.
        /// </summary>
        /// <param name="cfg">World storage and growth configuration.</param>
        /// <param name="id">Stable identifier for this world.</param>
        /// <param name="name">Human-readable world name.</param>
        /// <param name="tags">Tags attached to this world.</param>
        /// <param name="kernel">Owning kernel instance.</param>
        /// <param name="scope">Per-world service container.</param>
        public World(
            WorldConfig cfg,
            WorldId id,
            string name,
            IReadOnlyCollection<string> tags,
            IKernel kernel,
            ServiceContainer scope)
        {
            _cfg = cfg;

            _alive = new BitSet(_cfg.InitialEntityCapacity);
            _generation = new int[_cfg.InitialEntityCapacity];
            _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity);
            _nextId = 1;
            _pause = false;

            Id = id;
            Name = name;
            Tags = tags.Count == 0 ? Array.Empty<string>() : tags;

            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));

            _bus = _scope.GetRequired<IMessageBus>();
            _worker = _scope.GetRequired<IWorker>();
            _contextRegistry = _scope.GetRequired<IContextRegistry>();
            _bindingRouter = _scope.GetRequired<IBindingRouter>();
            _permissionHook = _scope.GetRequired<IPermissionHook>();
            _runner = _scope.GetRequired<ISystemRunner>();
            _componentPoolRepository = _scope.GetRequired<IComponentPoolRepository>();
        }

        /// <summary>
        /// Dispose the world: shutdown systems, reset storage, and dispose DI scope.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Reset(false);
            Resume();
            _scope.Dispose();
        }

        /// <summary>
        /// Pause stepping for this world.
        /// </summary>
        public void Pause() => _pause = true;

        /// <summary>
        /// Resume stepping for this world.
        /// </summary>
        public void Resume() => _pause = false;

        /// <summary>
        /// Kernel callback: begin-frame hook for this world.
        /// </summary>
        /// <param name="w">World instance to step.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        internal void BeginFrame(IWorld w, float dt)
        {
            if (w != this) return;
            if (IsPaused) return;

            FrameCount++;

            _runner.BeginFrame(w, dt);
        }

        /// <summary>
        /// Kernel callback: fixed-step hook for this world.
        /// </summary>
        /// <param name="w">World instance to step.</param>
        /// <param name="fixedDelta">Fixed timestep in seconds.</param>
        internal void FixedStep(IWorld w, float fixedDelta)
        {
            if (w != this) return;
            if (IsPaused) return;

            Tick++;

            _runner.FixedStep(w, fixedDelta);
        }

        /// <summary>
        /// Kernel callback: late-frame (presentation) hook for this world.
        /// </summary>
        /// <param name="w">World instance to step.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        /// <param name="alpha">
        /// Interpolation factor in [0,1] used by view/presentation systems.
        /// </param>
        internal void LateFrame(IWorld w, float dt, float alpha = 1)
        {
            if (w != this) return;
            if (IsPaused) return;

            _runner.LateFrame(w, dt, alpha);
        }
    }
}
