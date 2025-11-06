#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.DI;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.Bootstrap;
using ZenECS.Core.Internal.ComponentPooling;
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
        // private readonly MessageBus    _bus;
        // private readonly EventHub      _events;
        // private readonly QueryGateway  _query;
        private readonly ISystemRunner _runner;
        private readonly IComponentPoolRepository _componentPoolRepository;

        // // Read-only view + binding host are composed here (need this world context)
        // private readonly IReadWorld _readOnlyView;
        // private readonly AddonHost  _addons;

        public WorldId Id { get; }
        public string Name { get; set; }
        public IReadOnlyCollection<string> Tags { get; }
        public bool IsPaused => _pause;

        private bool _pause;

        public World(WorldId id, string name, IReadOnlyCollection<string> tags, IKernel kernel, ServiceContainer scope)
        {
            Id = id;
            Name = name;
            Tags = tags.Count == 0 ? Array.Empty<string>() : tags;

            _pause = false;

            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _kernel.OnBeginFrame += BeginFrame;
            _kernel.OnFixedStep += FixedStep;
            _kernel.OnLateFrame += LateFrame;

            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            // _store  = _scope.GetRequired<EntityStore>();
            // _bus    = _scope.GetRequired<MessageBus>();
            // _events = _scope.GetRequired<EventHub>();
            // _query  = _scope.GetRequired<QueryGateway>();
            _runner = _scope.GetRequired<ISystemRunner>();
            _componentPoolRepository = _scope.GetRequired<IComponentPoolRepository>();

            // // Compose read-only view & addon host (world-scoped)
            // _readOnlyView = new ReadOnlyView(_query, _events);
            // _addons       = new AddonHost(_readOnlyView);
        }

        public void Dispose()
        {
            Shutdown();
        }
        
        public void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null)
        {
            _runner.Build(systems, warn);
            _runner.Initialize(this);
        }

        public void Shutdown()
        {
            _runner.Shutdown(this);
            
            _kernel.OnBeginFrame -= BeginFrame;
            _kernel.OnFixedStep -= FixedStep;
            _kernel.OnLateFrame -= LateFrame;

            // Dispose DI scope (owned singletons/factories)
            _scope.Dispose();
        }

        public void AddComponent<T>(Entity e, in T value) where T : struct
        {
            if (!.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return;
            }

            bool valid = w.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return;
            }
            else if (!w.ValidateObject(value!))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return;
            }
        }
        
        public bool HasComponent<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            return pool != null && pool.Has(e.Id);
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