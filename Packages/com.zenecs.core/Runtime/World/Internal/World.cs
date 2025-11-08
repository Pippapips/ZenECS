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
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.ComponentPooling;
using ZenECS.Core.Internal.Contexts;
using ZenECS.Core.Internal.DI;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Messaging;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Internal.Systems;
using ZenECS.Core.Systems;

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
        public WorldId Id { get; }
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public IReadOnlyCollection<string> Tags { get; }
        /// <inheritdoc/>
        public bool IsPaused => _pause;

        private bool _pause;
        private bool _disposed;

        private readonly WorldConfig _cfg;

        /// <summary>Bitset of occupied entity slots (alive flags).</summary>
        private BitSet _alive;

        /// <summary>Next id to issue for newly created entities (1-based; 0 reserved).</summary>
        private int _nextId = 1;

        /// <summary>Recycled ids of destroyed entities (LIFO).</summary>
        private Stack<int> _freeIds;

        /// <summary>Per-slot generation counter preventing stale handles.</summary>
        private int[] _generation;

        /// <summary>Get current generation value for the given internal id.</summary>
        public int GenerationOf(int id) => _generation[id];

        /// <summary>
        /// Construct a world with a configuration, identity metadata, and a DI scope.
        /// Subscribes to kernel tick events and composes per-world services.
        /// </summary>
        public World(WorldConfig cfg, WorldId id, string name, IReadOnlyCollection<string> tags, IKernel kernel,
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

            _kernel.OnBeginFrame += BeginFrame;
            _kernel.OnFixedStep += FixedStep;
            _kernel.OnLateFrame += LateFrame;
        }

        /// <summary>
        /// Dispose the world: shutdown systems, unsubscribe from kernel ticks, reset storage, and dispose DI scope.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _runner.Shutdown(this);

            _kernel.OnBeginFrame -= BeginFrame;
            _kernel.OnFixedStep  -= FixedStep;
            _kernel.OnLateFrame  -= LateFrame;

            Reset(false);
            _scope.Dispose();
        }

        /// <summary>
        /// Build and initialize systems for this world using the configured runner.
        /// </summary>
        /// <param name="systems">Optional explicit system list; runner may provide defaults.</param>
        /// <param name="warn">Optional warning logger for runner diagnostics.</param>
        public void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null)
        {
            _runner.Build(systems, warn);
            _runner.Initialize(this);
        }

        /// <summary>Pause stepping for this world.</summary>
        public void Pause() => _pause = true;

        /// <summary>Resume stepping for this world.</summary>
        public void Resume() => _pause = false;

        private void BeginFrame(IWorld w, float dt)
        {
            if (w != this) return;
            _runner.BeginFrame(w, dt);
        }

        private void FixedStep(IWorld w, float fixedDelta)
        {
            if (w != this) return;
            _runner.FixedStep(w, fixedDelta);
        }

        private void LateFrame(IWorld w, float dt, float alpha = 1)
        {
            if (w != this) return;
            _runner.LateFrame(w, dt, alpha);
        }
    }
}
