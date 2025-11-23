// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemRunner.cs
// Purpose: Execute ECS systems per phase with lifecycle hooks and barrier
//          coordination against a world’s worker/router/permission hooks.
// Key concepts:
//   • Three-phase flow: BeginFrame (variable), FixedStep (fixed), LateFrame (presentation).
//   • FixedStep is split into: FixedInput → FixedDecision → FixedSimulation → FixedPost.
//   • Frame flow: FrameInput → FrameView, then Presentation + FrameUI in LateFrame.
//   • Barrier points: scheduler flush between phase buckets; router apply before Late.
//   • Read-only presentation: temporary write-deny guard during LateFrame.
//   • Deterministic: respects order planned by SystemPlanner.
// ─────────────────────────────────────────────────────────────────────────────-

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Events;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Messaging;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal.Systems
{
    /// <summary>
    /// Coordinates system execution for a single world across FrameInput/FrameView,
    /// FixedInput/FixedDecision/FixedSimulation/FixedPost, and Presentation/FrameUI
    /// phases. Handles lifecycle, barrier flushing, and frame-safe runtime mutations
    /// (add/remove/enable).
    /// </summary>
    internal sealed class SystemRunner : ISystemRunner, IDisposable
    {
        private SystemPlanner.Plan? _plan;

        // Authoritative list of active systems
        private readonly List<ISystem> _active = new();

        // Track which systems have received Initialize
        private readonly HashSet<ISystem> _initialized = new();

        // Pending mutations (applied only at frame boundary)
        private readonly List<ISystem> _pendingAdd = new();
        private readonly List<Type> _pendingRemove = new();
        private readonly List<Entity> _newEntitiesThisFrame = new();

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

            EntityEvents.EntityCreated += EntityEventsOnEntityCreated;
        }

        private void EntityEventsOnEntityCreated(IWorld w, Entity e)
        {
            _newEntitiesThisFrame.Add(e);
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
            _newEntitiesThisFrame.Clear();
            _dirty = false;

            EntityEvents.EntityCreated -= EntityEventsOnEntityCreated;
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
            foreach (var s in systems)
            {
                if (s != null)
                    _pendingAdd.Add(s);
            }
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

        public bool TryGet(Type t, out ISystem? system)
        {
            system = _active.FirstOrDefault(s => s.GetType() == t);
            return system != null;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ISystem> GetAllSystems()
        {
            return _active;
        }

        /// <inheritdoc/>
        public bool SetEnabled<T>(bool enabled) where T : ISystem
        {
            var s = _active.OfType<T>().FirstOrDefault();
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
            var s = _active.OfType<T>().FirstOrDefault();
            if (s is ISystemEnabledFlag f) return f.Enabled;
            return false;
        }

        /// <inheritdoc/>
        public bool IsEnabled(Type t)
        {
            var s = _active.FirstOrDefault(s => s.GetType() == t);
            if (s is ISystemEnabledFlag f) return f.Enabled;
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
                {
                    if (!_active.Contains(s))
                        _active.Add(s);
                }

                _pendingAdd.Clear();
            }

            // Rebuild plan and Initialize only newly joined systems
            _plan = SystemPlanner.Build(w, _active);
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
            ApplyPending(w);
            _bus.PumpAll();

            // 1) FrameInput: Unity Input, 디바이스, 창 크기 등
            RunGroup(SystemGroup.FrameInput, w, dt);
            _worker.RunScheduledJobs(w);

            // 2) FrameView: 카메라, 예측, 뷰용 로직
            RunGroup(SystemGroup.FrameView, w, dt);
            _worker.RunScheduledJobs(w);
        }

        /// <inheritdoc/>
        public void FixedStep(IWorld w, float fixedDelta)
        {
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
            if (_newEntitiesThisFrame.Count > 0)
            {
                EntityEvents.RaiseNewEntitiesFrame(w, _newEntitiesThisFrame.ToArray());
                _newEntitiesThisFrame.Clear();
            }

            // Apply world → view deltas before presentation systems run
            _router.ApplyAll(w);

            using var guard = DenyWrites(_permissionHook);

            // Presentation: 보간/뷰 바인딩 (IPresentationSystem)
            RunLateGroup(SystemGroup.Presentation, w, dt, interpolationAlpha);

            // FrameUI: HUD/디버그 (IFrameRunSystem, read-only 영역)
            RunLateGroup(SystemGroup.FrameUI, w, dt, interpolationAlpha);
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

        /// <summary>
        /// Runs variable-timestep systems for a specific execution group.
        /// FrameInput / FrameView.
        /// </summary>
        private void RunGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameInput:
                    foreach (IFrameSetupSystem s in _plan.FrameInput.OfType<IFrameSetupSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.FrameView:
                    foreach (IFrameRunSystem s in _plan.FrameView.OfType<IFrameRunSystem>())
                        s.Run(w, dt);
                    break;
            }
        }

        /// <summary>
        /// Runs fixed-timestep systems for a specific execution group.
        /// FixedInput / FixedDecision / FixedSimulation / FixedPost.
        /// </summary>
        private void RunFixedGroup(SystemGroup g, IWorld w, float dt)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FixedInput:
                    foreach (IFixedSetupSystem s in _plan.FixedInput.OfType<IFixedSetupSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.FixedDecision:
                    foreach (IFixedSetupSystem s in _plan.FixedDecision.OfType<IFixedSetupSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.FixedSimulation:
                    foreach (IFixedRunSystem s in _plan.FixedSimulation.OfType<IFixedRunSystem>())
                        s.Run(w, dt);
                    break;

                case SystemGroup.FixedPost:
                    foreach (IFixedRunSystem s in _plan.FixedPost.OfType<IFixedRunSystem>())
                        s.Run(w, dt);
                    break;
            }
        }

        /// <summary>
        /// Runs read-only presentation and frame UI systems.
        /// </summary>
        private void RunLateGroup(SystemGroup g, IWorld w, float dt, float interpolationAlpha = 1.0f)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.Presentation:
                    foreach (IPresentationSystem s in _plan.Presentation.OfType<IPresentationSystem>())
                        s.Run(w, dt, interpolationAlpha);
                    break;

                case SystemGroup.FrameUI:
                    foreach (IFrameRunSystem s in _plan.FrameUI.OfType<IFrameRunSystem>())
                        s.Run(w, dt);
                    break;
            }
        }
    }
}
