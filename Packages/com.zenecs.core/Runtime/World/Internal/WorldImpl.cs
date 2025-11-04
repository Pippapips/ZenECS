#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.DI;
using ZenECS.Core.Internal.Bootstrap;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// World implementation: storage, messaging, events, runner, binding host.
    /// All per-world services are resolved from the provided ServiceHost scope.
    /// </summary>
    internal sealed partial class WorldImpl : IWorld
    {
        private readonly IKernel _kernel;
        
        // DI scope for this world (disposed with the world)
        private readonly ServiceHost _scope;

        // Per-world services (resolved from _scope)
        // private readonly EntityStore   _store;
        // private readonly MessageBus    _bus;
        // private readonly EventHub      _events;
        // private readonly QueryGateway  _query;
        private readonly ISystemRunner _runner;

        // // Read-only view + binding host are composed here (need this world context)
        // private readonly IReadWorld _readOnlyView;
        // private readonly AddonHost  _addons;

        public WorldId Id { get; }
        public string  Name { get; set; }
        public IReadOnlyCollection<string> Tags { get; }
        public bool IsPaused { get; set; }

        public WorldImpl(WorldId id, string name, IReadOnlyCollection<string> tags, IKernel kernel, ServiceHost scope)
        {
            Id    = id;
            Name  = name;
            Tags  = tags;
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

            // // Compose read-only view & addon host (world-scoped)
            // _readOnlyView = new ReadOnlyView(_query, _events);
            // _addons       = new AddonHost(_readOnlyView);
        }

        private void FixedStep(IWorld w, float fixedDelta)
        {
            if (w != this) return;
            _runner.FixedStep(fixedDelta);
        }

        private void BeginFrame(IWorld w, float delta)
        {
            if (w != this) return;
            _runner.BeginFrame(delta);
        }

        private void LateFrame(IWorld w, float alpha = 1)
        {
            if (w != this) return;
            _runner.LateFrame(alpha);
        }

        public void Dispose()
        {
            _kernel.OnBeginFrame -= BeginFrame;
            _kernel.OnFixedStep -= FixedStep;
            _kernel.OnLateFrame -= LateFrame;
            
            // Dispose DI scope (owned singletons/factories)
            _scope.Dispose();
        }
    }
}
