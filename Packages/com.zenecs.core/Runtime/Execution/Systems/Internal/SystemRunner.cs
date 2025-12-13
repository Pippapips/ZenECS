// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemRunner.cs
// Purpose: Execute ECS systems per phase with lifecycle hooks and barrier
//          coordination against a world's worker/router/permission hooks.
// Key concepts:
//   • Three-phase flow: BeginFrame (variable), FixedStep (fixed), LateFrame (presentation).
//   • FixedStep is split into: FixedInput → FixedDecision → FixedSimulation → FixedPost.
//   • Frame flow: FrameInput → FrameSync; LateFrame: FrameView → FrameUI + binder ApplyAll.
//   • Barrier points: worker flush between phase buckets; router apply before Late exit.
//   • Read-only presentation: temporary write-deny guard during LateFrame.
//   • Deterministic: respects order planned by SystemPlanner.Plan.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Binding.Internal;
using ZenECS.Core.Hooking.Internal;
using ZenECS.Core.Internal;
using ZenECS.Core.Messaging.Internal;
using ZenECS.Core.Scheduling.Internal;

namespace ZenECS.Core.Systems.Internal
{
    /// <summary>
    /// Coordinates system execution for a single world across
    /// FrameInput/FrameSync, FixedInput/FixedDecision/FixedSimulation/FixedPost, and
    /// FrameView/FrameUI phases. Handles lifecycle, barrier flushing, and
    /// frame-safe runtime mutations (add/remove/enable).
    /// </summary>
    internal sealed class SystemRunner : ISystemRunner, IDisposable
    {
        /// <summary>
        /// Deterministic execution plan built by <see cref="SystemPlanner"/>.
        /// </summary>
        private SystemPlanner.Plan? _plan;

        /// <summary>
        /// State machine managing system lifecycle states.
        /// </summary>
        private readonly SystemStateMachine _stateMachine = new();

        private readonly IMessageBus _bus;
        private readonly IWorker _worker;
        private readonly IBindingRouter _router;
        private readonly IPermissionHook _permissionHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemRunner"/> class
        /// bound to world-scoped services.
        /// </summary>
        /// <param name="bus">Message bus used to pump world messages each frame.</param>
        /// <param name="worker">Worker used to execute scheduled jobs between phases.</param>
        /// <param name="router">
        /// Binding router that flushes presentation deltas at the end of LateFrame.
        /// </param>
        /// <param name="permissionHook">
        /// Permission hook used to temporarily deny structural writes during presentation.
        /// </param>
        public SystemRunner(IMessageBus bus, IWorker worker, IBindingRouter router, IPermissionHook permissionHook)
        {
            _permissionHook = permissionHook;
            _router = router;
            _worker = worker;
            _bus = bus;
        }

        /// <summary>
        /// Disposes the runner, issuing <see cref="ISystemLifecycle.Shutdown"/> to
        /// systems in the planner-defined shutdown order.
        /// </summary>
        /// <remarks>
        /// The injected world services (<see cref="IMessageBus"/>, <see cref="IWorker"/>,
        /// <see cref="IBindingRouter"/>, <see cref="IPermissionHook"/>) are not disposed.
        /// </remarks>
        public void Dispose()
        {
            if (_plan != null)
            {
                // Shutdown all initialized systems in reverse order
                var systemsToShutdown = _stateMachine.GetSystemsNeedingShutdown(_plan.LifecycleShutdownOrder);
                foreach (var system in systemsToShutdown)
                {
                    if (system is ISystemLifecycle lifecycle)
                    {
                        lifecycle.Shutdown();
                    }
                }
            }

            _stateMachine.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Runtime mutation API (frame-safe). Exposed via ISystemRunner.
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void RequestAdd(ISystem system)
        {
            _stateMachine.QueueAdd(system);
        }

        /// <inheritdoc/>
        public void RequestAddRange(IEnumerable<ISystem> systems)
        {
            _stateMachine.QueueAddRange(systems);
        }

        /// <inheritdoc/>
        public void RequestRemove<T>() where T : ISystem
        {
            _stateMachine.QueueRemove(typeof(T));
        }

        /// <inheritdoc/>
        public void RequestRemove(Type t)
        {
            _stateMachine.QueueRemove(t);
        }

        /// <inheritdoc/>
        public bool TryGet<T>(out T? system) where T : class, ISystem
        {
            var activeSystems = _stateMachine.GetActiveSystems();
            system = activeSystems.OfType<T>().FirstOrDefault();
            return system != null;
        }

        /// <summary>
        /// Attempts to get a system instance by its exact runtime type.
        /// </summary>
        /// <param name="t">Concrete system type to look up.</param>
        /// <param name="system">
        /// When this method returns, contains the matching system instance or
        /// <see langword="null"/> if not found.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a matching system instance was found; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGet(Type t, out ISystem? system)
        {
            var activeSystems = _stateMachine.GetActiveSystems();
            system = activeSystems.FirstOrDefault(s => s.GetType() == t);
            return system != null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ISystem> GetAllSystems()
        {
            return _stateMachine.GetActiveSystems();
        }

        /// <inheritdoc/>
        public bool SetEnabled<T>(bool enabled) where T : ISystem
        {
            var activeSystems = _stateMachine.GetActiveSystems();
            var s = activeSystems.OfType<T>().FirstOrDefault();
            if (s is ISystemEnabledFlag f)
            {
                f.Enabled = enabled;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsEnabled<T>() where T : ISystem
        {
            var activeSystems = _stateMachine.GetActiveSystems();
            var s = activeSystems.OfType<T>().FirstOrDefault();
            if (s is ISystemEnabledFlag f) return f.Enabled;
            return false;
        }

        /// <inheritdoc/>
        public bool IsEnabled(Type t)
        {
            var activeSystems = _stateMachine.GetActiveSystems();
            var s = activeSystems.FirstOrDefault(s => s.GetType() == t);
            if (s is ISystemEnabledFlag f) return f.Enabled;
            return false;
        }

        /// <summary>
        /// Applies queued mutations, rebuilds the deterministic plan, and performs
        /// delta Initialize/Shutdown as needed. Call at the frame boundary.
        /// </summary>
        /// <param name="w">World for which the runner is executing.</param>
        private void ApplyPending(IWorld w)
        {
            if (!_stateMachine.HasPendingMutations) return;

            // Apply pending state transitions (additions and removals)
            var newlyAdded = _stateMachine.ApplyPending(
                onRemove: null, // Removal is handled by state machine
                onShutdown: system =>
                {
                    if (system is ISystemLifecycle lifecycle)
                    {
                        lifecycle.Shutdown();
                    }
                });

            // Rebuild plan with all active systems
            var activeSystems = _stateMachine.GetActiveSystems();
            _plan = SystemPlanner.Build(w, activeSystems);

            // Initialize newly added systems (transition Active → Initialized)
            if (_plan != null)
            {
                foreach (var lifecycleSystem in _plan.LifecycleInitializeOrder)
                {
                    var system = (ISystem)lifecycleSystem;
                    
                    // Only initialize systems that are in Active state (not yet initialized)
                    if (_stateMachine.GetState(system) == SystemState.Active)
                    {
                        lifecycleSystem.Initialize(w);
                        _stateMachine.TransitionToInitialized(system);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void BeginFrame(IWorld w, float dt)
        {
            // 1) Apply pending add/remove/enable operations and build plan.
            ApplyPending(w);

            // 2) Pump messages once at the start of the frame.
            _bus.PumpAll();

            w.SetWritePhaseInternal(
                WorldWritePhase.FrameInput,
                denyAllWrites: false,
                structuralChangesAllowed: true);

            // 3) FrameInput: engine input, device, window events, etc.
            RunGroup(SystemGroup.FrameInput, w, dt);
            _worker.RunScheduledJobs(w);

            w.SetWritePhaseInternal(
                WorldWritePhase.FrameSync,
                denyAllWrites: false,
                structuralChangesAllowed: false);

            // 4) FrameSync: camera, view-space, non-deterministic sync logic.
            RunGroup(SystemGroup.FrameSync, w, dt);
            _worker.RunScheduledJobs(w);
        }

        /// <inheritdoc/>
        public void FixedStep(IWorld w, float fixedDelta)
        {
            w.SetWritePhaseInternal(
                WorldWritePhase.Simulation,
                denyAllWrites: false,
                structuralChangesAllowed: true);
            w.ExternalCommandFlushToInternal();

            // Fixed-step deterministic pipeline:
            // FixedInput → FixedDecision → FixedSimulation → FixedPost
            RunFixedGroup(SystemGroup.FixedInput, w, fixedDelta);
            _worker.RunScheduledJobs(w);

            RunFixedGroup(SystemGroup.FixedDecision, w, fixedDelta);
            _worker.RunScheduledJobs(w);

            RunFixedGroup(SystemGroup.FixedSimulation, w, fixedDelta);
            _worker.RunScheduledJobs(w);

            RunFixedGroup(SystemGroup.FixedPost, w, fixedDelta);
            _worker.RunScheduledJobs(w);
        }

        /// <inheritdoc/>
        public void LateFrame(IWorld w, float dt, float interpolationAlpha = 1f)
        {
            w.SetWritePhaseInternal(
                WorldWritePhase.FrameView,
                denyAllWrites: false,
                structuralChangesAllowed: false);

            // 1) FrameView: interpolation, transforms, animation, view binding.
            RunLateGroup(SystemGroup.FrameView, w, dt, interpolationAlpha);
            _worker.RunScheduledJobs(w);

            // 2) Temporarily deny structural writes during UI and binder ApplyAll.
            using var guard = DenyWrites(_permissionHook);

            w.SetWritePhaseInternal(
                WorldWritePhase.FrameUI,
                denyAllWrites: true,      // UI should be read-only from the world POV
                structuralChangesAllowed: false);

            // 3) FrameUI: HUD, overlays, debug UI.
            RunLateGroup(SystemGroup.FrameUI, w, dt, interpolationAlpha);

            // 4) Apply world → view deltas (binders).
            _router.ApplyAll(w);

            // 5) Clear write phase back to neutral.
            w.ClearWritePhaseInternal();
        }

        /// <summary>
        /// Creates a temporary guard that denies Add/Replace/Remove during presentation.
        /// </summary>
        /// <param name="hook">Permission hook used to register the guard predicate.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> token that, when disposed, removes the guard.
        /// </returns>
        private static IDisposable DenyWrites(IPermissionHook hook)
        {
            Func<Entity, Type, bool> token = static (_, __) => false;
            hook.AddWritePermission(token);
            return new DisposableAction(() => hook.RemoveWritePermission(token));
        }

        /// <summary>
        /// Simple disposable action wrapper used by <see cref="DenyWrites"/>.
        /// </summary>
        /// <remarks>
        /// This class provides a lightweight way to register a cleanup action
        /// that will be executed when the disposable is disposed, typically
        /// via a <c>using</c> statement. It is used to manage the lifetime
        /// of write permission guards during presentation phases.
        /// </remarks>
        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _onDispose;

            /// <summary>
            /// Initializes a new instance of the <see cref="DisposableAction"/> class.
            /// </summary>
            /// <param name="onDispose">Callback to invoke on <see cref="Dispose"/>.</param>
            public DisposableAction(Action onDispose) => _onDispose = onDispose;

            /// <summary>
            /// Invokes the configured dispose callback.
            /// </summary>
            public void Dispose() => _onDispose();
        }

        /// <summary>
        /// Runs a system only when it is enabled (or has no enable flag).
        /// </summary>
        /// <param name="system">System instance to run.</param>
        /// <param name="w">World passed to the system.</param>
        /// <param name="dt">Delta time argument.</param>
        private static void RunIfEnabled(ISystem? system, IWorld w, float dt)
        {
            if (system == null) return;
            if (system is ISystemEnabledFlag flag && !flag.Enabled) return;
            system.Run(w, dt);
        }

        /// <summary>
        /// Runs variable-timestep systems for a specific execution group
        /// (<see cref="SystemGroup.FrameInput"/> or <see cref="SystemGroup.FrameSync"/>).
        /// </summary>
        /// <param name="g">System group to execute.</param>
        /// <param name="w">World instance.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        private void RunGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameInput:
                    foreach (var system in _plan.FrameInput)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;

                case SystemGroup.FrameSync:
                    foreach (var system in _plan.FrameSync)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;
            }
        }

        /// <summary>
        /// Runs read-only presentation and frame UI systems.
        /// </summary>
        /// <param name="g">System group to execute (FrameView or FrameUI).</param>
        /// <param name="w">World instance.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        /// <param name="interpolationAlpha">
        /// Interpolation factor in [0,1], typically derived from accumulator/fixedDelta.
        /// </param>
        private void RunLateGroup(SystemGroup g, IWorld w, float dt, float interpolationAlpha = 1.0f)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameView:
                    foreach (var system in _plan.FrameView)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;

                case SystemGroup.FrameUI:
                    foreach (var system in _plan.FrameUI)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;
            }
        }

        /// <summary>
        /// Runs fixed-timestep systems for a specific execution group:
        /// FixedInput, FixedDecision, FixedSimulation, or FixedPost.
        /// </summary>
        /// <param name="g">System group to execute.</param>
        /// <param name="w">World instance.</param>
        /// <param name="dt">Fixed timestep in seconds.</param>
        private void RunFixedGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FixedInput:
                    foreach (var system in _plan.FixedInput)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;

                case SystemGroup.FixedDecision:
                    foreach (var system in _plan.FixedDecision)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;

                case SystemGroup.FixedSimulation:
                    foreach (var system in _plan.FixedSimulation)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;

                case SystemGroup.FixedPost:
                    foreach (var system in _plan.FixedPost)
                    {
                        RunIfEnabled(system, w, dt);
                    }
                    break;
            }
        }
    }
}
