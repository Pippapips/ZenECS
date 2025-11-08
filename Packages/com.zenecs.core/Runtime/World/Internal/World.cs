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
    /// World implementation: storage, messaging, events, runner, binding host.
    /// All per-world services are resolved from the provided ServiceHost scope.
    /// </summary>
    internal sealed partial class World : IWorld
    {
        private readonly IKernel _kernel;

        // DI scope for this world (disposed with the world)
        private readonly ServiceContainer _scope;

        // Per-world services (resolved from _scope)
        // private readonly EntityStore   _store;
        // private readonly EventHub      _events;
        // private readonly QueryGateway  _query;
        private readonly ISystemRunner _runner;
        private readonly IPermissionHook _permissionHook;
        private readonly IBindingRouter _bindingRouter;
        private readonly IContextRegistry _contextRegistry;
        private readonly IComponentPoolRepository _componentPoolRepository;
        private readonly IMessageBus _bus;
        private readonly IWorker _worker;

        // // Read-only view + binding host are composed here (need this world context)
        // private readonly IReadWorld _readOnlyView;
        // private readonly AddonHost  _addons;

        public WorldId Id { get; }
        public string Name { get; set; }
        public IReadOnlyCollection<string> Tags { get; }
        public bool IsPaused => _pause;

        private bool _pause;
        private bool _disposed;

        private readonly WorldConfig _cfg;

        /// <summary>
        /// Bitset of occupied entity slots (alive flags).
        /// </summary>
        private BitSet _alive;

        /// <summary>
        /// Next id to issue for newly created entities.
        /// Starts at 1 to reserve 0 for "null"/invalid semantics.
        /// </summary>
        private int _nextId = 1;

        /// <summary>
        /// Recycled IDs of destroyed entities (LIFO). When creating a new entity,
        /// the world prefers reusing a freed id from this stack before growing.
        /// </summary>
        private Stack<int> _freeIds;

        /// <summary>
        /// Generation array: per-slot generation counter used to prevent zombie handles.
        /// Increments when an id is destroyed and reused, so stale handles no longer match.
        /// </summary>
        private int[] _generation; // 세대(Generation) 배열: slot별 현재 세대 카운터 → Generation array: per-slot current generation

        public int GenerationOf(int id) => _generation[id];

        public World(WorldConfig cfg, WorldId id, string name, IReadOnlyCollection<string> tags, IKernel kernel,
            ServiceContainer scope)
        {
            _cfg = cfg;

            _alive = new BitSet(_cfg.InitialEntityCapacity);       // Bitmap of occupied entity slots
            _generation = new int[_cfg.InitialEntityCapacity];     // Per-slot generation counters (start at 0)
            _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity); // Recycled IDs storage for destroyed entities
            _nextId = 1;                                           // New entities start from 1
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _runner.Shutdown(this);

            _kernel.OnBeginFrame -= BeginFrame;
            _kernel.OnFixedStep -= FixedStep;
            _kernel.OnLateFrame -= LateFrame;

            Reset(false);
            
            // Dispose DI scope (owned singletons/factories)
            _scope.Dispose();
        }

        public void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null)
        {
            _runner.Build(systems, warn);
            _runner.Initialize(this);
        }

        public void Pause()
        {
            _pause = true;
        }

        public void Resume()
        {
            _pause = false;
        }
        
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