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
// ─────────────────────────────────────────────────────────────────────────────-
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
    /// and Presentation phases. Handles lifecycle, barrier flushing, and frame-safe
    /// runtime mutations (add/remove/enable).
    /// </summary>
    internal sealed class SystemRunner : ISystemRunner
    {
        private SystemPlanner.Plan? _plan;

        // Authoritative list of active systems
        private readonly List<ISystem> _active = new();

        // Track which systems have received Initialize
        private readonly HashSet<ISystem> _initialized = new();

        // Pending mutations (applied only at frame boundary)
        private readonly List<ISystem> _pendingAdd = new();
        private readonly List<Type> _pendingRemove = new();
        private bool _dirty;

        private readonly IMessageBus _bus;
        private readonly IWorker _worker;
        private readonly IBindingRouter _router;
        private readonly IPermissionHook _permissionHook;

        /// <summary>
        /// Creates a system runner bound to world-scoped services.
        /// </summary>
        public SystemRunner(IMessageBus bus, IWorker worker, IBindingRouter router, IPermissionHook permissionHook)
        {
            _permissionHook = permissionHook;
            _router = router;
            _worker = worker;
            _bus = bus;
        }

        /// <summary>
        /// Disposes the runner, issuing Shutdown to systems in reverse order.
        /// </summary>
        /// <remarks>
        /// This does not dispose the injected world services.
        /// </remarks>
        public void Dispose()
        {
            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleShutdownOrder)
                    s.Shutdown();
            }

            _initialized.Clear();
            _active.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
            _dirty = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Runtime mutation API (frame-safe). Exposed via ISystemRunner.
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void RequestAdd(ISystem system)
        {
            if (system == null) return;
            _pendingAdd.Add(system);
            _dirty = true;
        }

        /// <inheritdoc/>
        public void RequestAddRange(IEnumerable<ISystem> systems)
        {
            if (systems == null) return;
            foreach (var s in systems) if (s != null) _pendingAdd.Add(s);
            _dirty = true;
        }

        /// <inheritdoc/>
        public void RequestRemove<T>() where T : ISystem
        {
            _pendingRemove.Add(typeof(T));
            _dirty = true;
        }

        /// <inheritdoc/>
        public void RequestRemove(Type t)
        {
            if (t == null) return;
            _pendingRemove.Add(t);
            _dirty = true;
        }

        /// <inheritdoc/>
        public bool TryGet<T>(out T? system) where T : class, ISystem
        {
            system = _active.OfType<T>().FirstOrDefault();
            return system != null;
        }

        /// <inheritdoc/>
        public bool SetEnabled<T>(bool enabled) where T : ISystem
        {
            var s = _active.OfType<T>().FirstOrDefault();
            if (s is ISystemEnabledFlag f) { f.Enabled = enabled; return true; }
            return false;
        }

        /// <summary>
        /// Applies queued mutations, rebuilds the deterministic plan, and performs
        /// delta Initialize/Shutdown as needed. Call at the frame boundary.
        /// </summary>
        private void ApplyPending(IWorld w)
        {
            if (!_dirty) return;

            // Remove by type (Shutdown if previously initialized)
            if (_pendingRemove.Count > 0)
            {
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    var s = _active[i];
                    var st = s.GetType();
                    if (_pendingRemove.Any(t => t == st))
                    {
                        if (_initialized.Contains(s) && s is ISystemLifecycle life)
                        {
                            life.Shutdown();
                            _initialized.Remove(s);
                        }
                        _active.RemoveAt(i);
                    }
                }
                _pendingRemove.Clear();
            }

            // Add new systems (avoid duplicate instances)
            if (_pendingAdd.Count > 0)
            {
                foreach (var s in _pendingAdd)
                    if (!_active.Contains(s)) _active.Add(s);
                _pendingAdd.Clear();
            }

            // Rebuild plan and Initialize only newly joined systems
            _plan = SystemPlanner.Build(_active);
            if (_plan != null)
            {
                foreach (var s in _plan.LifecycleInitializeOrder)
                {
                    var sys = (ISystem)s;
                    if (!_initialized.Contains(sys))
                    {
                        s.Initialize(w);
                        _initialized.Add(sys);
                    }
                }
            }

            _dirty = false;
        }

        /// <inheritdoc/>
        public void BeginFrame(IWorld w, float dt)
        {
            // Apply queued mutations at the start of the frame
            ApplyPending(w);

            // Drain per-world message queue at frame start
            _bus.PumpAll();

            // FrameSetup (variable)
            RunGroup(SystemGroup.FrameSetup, w, dt);
            _worker.RunScheduledJobs(w);

            // Simulation (variable)
            RunGroup(SystemGroup.Simulation, w, dt);
            _worker.RunScheduledJobs(w);
        }

        /// <inheritdoc/>
        public void FixedStep(IWorld w, float fixedDelta)
        {
            RunFixedGroup(SystemGroup.FrameSetup, w, fixedDelta);
            RunFixedGroup(SystemGroup.Simulation, w, fixedDelta);
            // Note: structural changes are deferred to the main-frame boundary
        }

        /// <inheritdoc/>
        public void LateFrame(IWorld w, float dt, float interpolationAlpha = 1f)
        {
            // Apply world → view deltas before presentation systems run
            _router.ApplyAll();

            using IDisposable? guard = DenyWrites(_permissionHook);
            RunLateGroup(SystemGroup.Presentation, w, dt, interpolationAlpha);
        }

        /// <summary>
        /// Creates a temporary guard that denies Add/Replace/Remove during presentation.
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

        /// <summary>Runs fixed-timestep systems for a specific execution group.</summary>
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

        /// <summary>Runs variable-timestep systems for a specific execution group.</summary>
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

        /// <summary>Runs read-only presentation systems.</summary>
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
