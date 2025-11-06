// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core
// File: SystemRunner.cs
// Purpose: Executes ECS systems in grouped order with configurable flush policy.
// Key concepts:
//   • Integrates with Unity’s Update / FixedUpdate / LateUpdate style flow.
//   • Supports deferred structural changes via policy (EndOfSimulation, NextFrame, Manual).
//   • Guards write access during presentation to enforce read-only phase.
//   • Coordinates world, systems, and message bus lifecycle.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Coordinates system execution per phase (FrameSetup, Simulation, Presentation)
    /// and manages lifecycle (Initialize / Shutdown).
    /// </summary>
    internal sealed class SystemRunner : ISystemRunner
    {
        private bool _started;
        private bool _stopped;
        private SystemPlanner.Plan? _plan;
        
        private readonly IMessageBus _bus;
        private readonly IWorker _worker;
        private readonly IBindingRouter _router;
        private IPermissionHook _permissionHook;

        public SystemRunner(IMessageBus bus, IWorker worker, IBindingRouter router, IPermissionHook permissionHook)
        {
            _permissionHook = permissionHook;
            _router = router;
            _worker = worker;
            _bus = bus;
        }

        public void Build(IEnumerable<ISystem>? systems, Action<string>? warn)
        {
            _plan = SystemPlanner.Build(systems, warn);
        }

        /// <summary>
        /// Initializes all systems once before the first frame.
        /// </summary>
        public void Initialize(IWorld w)
        {
            if (_started) return;
            _started = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleInitializeOrder)
                    s.Initialize(w);
            }

            _worker.RunScheduledJobs(w);
        }

        /// <summary>
        /// Shuts down all systems in reverse order (Presentation → Simulation → Setup).
        /// </summary>
        public void Shutdown(IWorld w)
        {
            if (!_started || _stopped) return;
            _stopped = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleShutdownOrder)
                    s.Shutdown(w);
            }
        }

        /// <summary>
        /// Corresponds to Unity's Update phase (variable timestep + barrier management).
        /// </summary>
        public void BeginFrame(IWorld w, float dt)
        {
            // Consume all queued messages
            _bus.PumpAll();

            // Frame setup phase (no DeltaTime use)
            RunGroup(SystemGroup.FrameSetup, w, dt);
            _worker.RunScheduledJobs(w);

            RunGroup(SystemGroup.Simulation, w, dt);
            // Barrier handling based on flush policy
            _worker.RunScheduledJobs(w);
        }
        
        /// <summary>
        /// Corresponds to Unity's FixedUpdate phase (fixed timestep).
        /// Structural changes are queued, not applied immediately.
        /// </summary>
        public void FixedStep(IWorld w, float fixedDelta)
        {
            // Optional pre-step setup (no DeltaTime use)
            RunFixedGroup(SystemGroup.FrameSetup, w, fixedDelta);
            RunFixedGroup(SystemGroup.Simulation, w, fixedDelta);
            // NOTE: Do not flush jobs here; handled at frame barrier (BeginFrame).
        }

        /// <summary>
        /// Corresponds to Unity's LateUpdate phase (Presentation stage, read-only).
        /// </summary>
        public void LateFrame(IWorld w, float dt, float interpolationAlpha = 1f)
        {
            _router.ApplyAll();
            
            using IDisposable? guard = DenyWrites(_permissionHook);
            RunLateGroup(SystemGroup.Presentation, w, dt, interpolationAlpha);
        }

        /// <summary>
        /// Temporarily disables write operations during presentation (Add/Replace/Remove).
        /// </summary>
        private static IDisposable DenyWrites(IPermissionHook hook)
        {
            Func<IWorld, Entity, Type, bool> token = static (_, _, __) => false;
            hook.AddWritePermission(token);
            return new DisposableAction(() => hook.RemoveWritePermission(token));
        }

        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _onDispose;
            public DisposableAction(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }

        private void RunFixedGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameSetup:
                    foreach (IFixedSetupSystem s in _plan.FrameSetup.OfType<IFixedSetupSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.Simulation:
                    foreach (IFixedRunSystem s in _plan.Simulation.OfType<IFixedRunSystem>())
                        s.Run(w, dt);
                    break;
            }
        }

        private void RunGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameSetup:
                    foreach (IFrameSetupSystem s in _plan.FrameSetup.OfType<IFrameSetupSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.Simulation:
                    foreach (IVariableRunSystem s in _plan.Simulation.OfType<IVariableRunSystem>())
                        s.Run(w, dt);
                    break;
            }
        }

        private void RunLateGroup(SystemGroup g, IWorld w, float dt, float interpolationAlpha = 1.0f)
        {
            if (_plan == null) return;

            if (g == SystemGroup.Presentation)
            {
                foreach (IPresentationSystem s in _plan.Presentation.OfType<IPresentationSystem>())
                    s.Run(w, dt, interpolationAlpha);
            }
        }
    }
}
