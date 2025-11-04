using System;
using System.Linq;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Testing
{
    /// <summary>
    /// Thin, in‑memory host for tests. Creates a World + Bus and exposes a minimal runner facade.
    /// </summary>
    public sealed class TestWorldHost : IDisposable
    {
        public WorldOld WorldOld { get; }
        public IMessageBus Bus { get; }

        private ISystem[]? _orderedSystems = Array.Empty<ISystem>();

        public TestWorldHost(WorldOld? world = null, IMessageBus? bus = null)
        {
            WorldOld = world ?? new WorldOld();
            Bus = bus ?? new ZenECS.Core.Messaging.MessageBus();
        }

        /// <summary>Registers systems (in any order). Order is resolved by <see cref="SystemPlanner"/>.</summary>
        public void RegisterSystems(params ISystem[] systems)
        {
            var plan = SystemPlanner.Build(systems); // deterministic order
            _orderedSystems = plan?.AllInExecutionOrder.ToArray();
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as ISystemLifecycle)?.Initialize(WorldOld);
        }

        /// <summary>Simulate one frame: FrameSetup → Fixed(n) → Update → LateFrame.</summary>
        public void TickFrame(int fixedSteps = 0)
        {
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as IFrameSetupSystem)?.Run(WorldOld);

            for (int i = 0; i < fixedSteps; i++)
                foreach (var s in _orderedSystems)
                    (s as IFixedRunSystem)?.Run(WorldOld);

            foreach (var s in _orderedSystems)
                (s as IVariableRunSystem)?.Run(WorldOld);

            foreach (var s in _orderedSystems)
                (s as IPresentationSystem)?.Run(WorldOld);

            // Deliver queued bus messages once per frame
            Bus.PumpAll();
        }

        public void Dispose()
        {
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as ISystemLifecycle)?.Shutdown(WorldOld);
        }
    }
}