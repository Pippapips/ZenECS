// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemRunner.cs
// Purpose: Execute ECS systems per phase with lifecycle hooks and barrier
//          coordination against a world’s worker/router/permission hooks.
// Key concepts:
//   • Three-phase flow: BeginFrame (variable), FixedStep (fixed), LateFrame (presentation).
//   • Barrier points: scheduler flush between setup/simulation; router apply before Late.
//   • Read-only presentation: temporary write-deny guard during LateFrame.
//   • Deterministic: respects order planned by SystemPlanner.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Messaging;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal.Systems
{
    /// <summary>
    /// Coordinates system execution for a single world across FrameSetup, Simulation,
    /// and Presentation phases. Handles system lifecycle and barrier flushing.
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

        /// <summary>
        /// Construct a runner bound to world-scoped services.
        /// </summary>
        public SystemRunner(IMessageBus bus, IWorker worker, IBindingRouter router, IPermissionHook permissionHook)
        {
            _permissionHook = permissionHook;
            _router = router;
            _worker = worker;
            _bus = bus;
        }

        /// <inheritdoc/>
        public void Build(IEnumerable<ISystem>? systems, Action<string>? warn)
        {
            _plan = SystemPlanner.Build(systems, warn);
        }

        /// <summary>
        /// Initialize systems once before the first frame.
        /// </summary>
        /// <param name="w">Target world.</param>
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
        /// Shutdown systems in reverse execution order.
        /// </summary>
        /// <param name="w">Target world.</param>
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
        /// Variable-timestep tick covering FrameSetup and Simulation. Pumps the
        /// message bus and flushes scheduled jobs at barrier points.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Delta time in seconds.</param>
        public void BeginFrame(IWorld w, float dt)
        {
            // Drain per-world message queue at frame start.
            _bus.PumpAll();

            // FrameSetup (variable)
            RunGroup(SystemGroup.FrameSetup, w, dt);
            _worker.RunScheduledJobs(w);

            // Simulation (variable)
            RunGroup(SystemGroup.Simulation, w, dt);
            _worker.RunScheduledJobs(w);
        }

        /// <summary>
        /// Fixed-timestep tick. Structural changes are queued for later application.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="fixedDelta">Fixed step duration in seconds.</param>
        public void FixedStep(IWorld w, float fixedDelta)
        {
            RunFixedGroup(SystemGroup.FrameSetup, w, fixedDelta);
            RunFixedGroup(SystemGroup.Simulation, w, fixedDelta);
            // NOTE: Don't flush here; main-frame barriers handle it.
        }

        /// <summary>
        /// Presentation (read-only) tick. Applies router deltas and temporarily denies writes.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Delta time in seconds of the originating frame.</param>
        /// <param name="interpolationAlpha">Interpolation factor in [0,1].</param>
        public void LateFrame(IWorld w, float dt, float interpolationAlpha = 1f)
        {
            // Apply world→view deltas before presentation systems run.
            _router.ApplyAll();

            using IDisposable? guard = DenyWrites(_permissionHook);
            RunLateGroup(SystemGroup.Presentation, w, dt, interpolationAlpha);
        }

        /// <summary>
        /// Create a temporary guard that denies Add/Replace/Remove during presentation.
        /// </summary>
        private static IDisposable DenyWrites(IPermissionHook hook)
        {
            Func<Entity, Type, bool> token = static (_, __) => false;
            hook.AddWritePermission(token);
            return new DisposableAction(() => hook.RemoveWritePermission(token));
        }

        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _onDispose;
            public DisposableAction(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }

        /// <summary>
        /// Run fixed-timestep systems for a specific group.
        /// </summary>
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

        /// <summary>
        /// Run variable-timestep systems for a specific group.
        /// </summary>
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

        /// <summary>
        /// Run presentation systems (read-only).
        /// </summary>
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
