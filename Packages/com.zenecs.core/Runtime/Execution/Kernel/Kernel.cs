// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: Kernel.cs
// Purpose: Central coordinator that owns the lifetime, lookup, and stepping of
//          multiple independent Worlds. It composes the root DI container,
//          creates per-world scopes, and drives simulation (Begin/Fixed/Late).
// Key concepts:
//   • Multi-world manager: create/destroy, find by id/name/tag, select current.
//   • Deterministic stepping: BeginFrame(dt) → FixedStep(fixedDelta)×N → LateFrame(alpha).
//   • Accumulator-based fixed update: PumpAndLateFrame() consumes dt into fixed substeps.
//   • Events as hooks: OnBeginFrame/OnFixedStep/OnLateFrame per world.
//   • DI/bootstrap: builds a root ServiceContainer and per-world scopes.
//   • Indexes: concurrent maps for id/name/tag with thread-safe snapshots.
//   • Pause & selection semantics: optionally step only the currently selected world.
//   • Time tracking: frame counters and total simulated seconds (fixed-step only).
//   • Adapter-friendly: Unity/Godot/etc. call the three ticks from their loops.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Config;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.Bootstrap;
using ZenECS.Core.Infrastructure.Internal;

namespace ZenECS.Core
{
    /// <summary>
    /// Manages the lifecycle, lookup and ticking of multiple worlds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// External code must interact with world state via the world's API surfaces,
    /// not through kernel internals. The kernel is responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Creating and destroying worlds and their DI scopes.</description></item>
    /// <item><description>Indexing worlds by id, name, and tags.</description></item>
    /// <item><description>Stepping all or a single selected world.</description></item>
    /// <item><description>Tracking global time counters for frames and fixed steps.</description></item>
    /// </list>
    /// </remarks>
    public sealed class Kernel : IKernel
    {
        // --- Indexes ---------------------------------------------------------

        // id → world
        private readonly ConcurrentDictionary<WorldId, IWorld> _byId = new();

        // name → set of world ids (protected by _nameLock)
        private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byName =
            new(StringComparer.Ordinal);

        // tag → set of world ids (protected by _tagLock)
        private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byTag =
            new(StringComparer.Ordinal);

        private readonly object _nameLock = new();
        private readonly object _tagLock = new();

        // --- Time state ------------------------------------------------------

        private float _delta;
        private float _simulationAccumulatorSeconds;
        private float _totalTime;
        private long _frameCount;
        private long _fixedFrameCount;
        private int _fixedFrameIndexInFrame;
        private bool _firstFixedStepThisFrame;
        private double _totalSimulatedSeconds; // Accumulated time processed by FixedStep (seconds)
        private KernelOptions? _options;

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public bool IsPaused { get; private set; }

        /// <inheritdoc/>
        public float SimulationAccumulatorSeconds => _simulationAccumulatorSeconds;

        /// <inheritdoc/>
        public float TotalTimeSeconds => _totalTime;

        /// <inheritdoc/>
        public long FrameCount => _frameCount;

        /// <inheritdoc/>
        public long FixedFrameCount => _fixedFrameCount;

        /// <inheritdoc/>
        public int FixedFrameIndexInFrame => _fixedFrameIndexInFrame;

        /// <inheritdoc/>
        public double TotalSimulatedSeconds => _totalSimulatedSeconds;

        private IWorld? _current;

        /// <inheritdoc/>
        public IWorld? CurrentWorld => _current;

        /// <inheritdoc/>
        public KernelOptions? Options => _options;

        /// <inheritdoc/>
        public event Action<IWorld>? WorldCreated;

        /// <inheritdoc/>
        public event Action<IWorld>? WorldDestroyed;

        /// <inheritdoc/>
        public event Action<IWorld?, IWorld?>? CurrentWorldChanged;

        /// <inheritdoc/>
        public event Action? Disposed;

        // --- Internal root services (DI/bootstrap), not exposed -------------

        private readonly ServiceContainer? _root;

        // --- Construction ----------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Kernel"/> class.
        /// </summary>
        /// <param name="options">Optional kernel options; a default is used when omitted.</param>
        /// <param name="logger">Optional logger to plug into <see cref="EcsRuntimeOptions.Log"/>.</param>
        public Kernel(KernelOptions? options = null, IEcsLogger? logger = null)
        {
            IsRunning = true;
            IsPaused = false;
            _simulationAccumulatorSeconds = 0;

            _options = options ?? new KernelOptions();
            if (logger != null) EcsRuntimeOptions.Log = logger;
            _root = CoreBootstrap.BuildRoot(_options);
        }

        /// <summary>
        /// Disposes the kernel and all worlds it owns.
        /// </summary>
        public void Dispose()
        {
            if (!IsRunning) return;

            // Destroy all worlds (snapshot to avoid modification during iteration).
            foreach (var w in _byId.Values.ToArray())
                DestroyWorld(w);

            _root?.Dispose();
            _options = null;
            _current = null;
            IsRunning = false;
            IsPaused = false;
            _totalSimulatedSeconds = 0;
            Disposed?.Invoke();
        }

        // --- World Management ------------------------------------------------

        /// <inheritdoc/>
        public IWorld CreateWorld(
            WorldConfig? cfg = null,
            string? name = null,
            IEnumerable<string>? tags = null,
            WorldId? presetId = null,
            bool setAsCurrent = false)
        {
            if (!IsRunning)
                throw new ObjectDisposedException(nameof(Kernel), "Kernel has been disposed.");

            if (Options == null)
                throw new InvalidOperationException("Kernel has no options configured.");

            var id = presetId ?? Options.NewWorldId();
            var finalName = name ?? $"{Options.AutoNamePrefix}{id.Value.ToString("N")[..6]}";
            var finalTags = (tags ?? Array.Empty<string>()).ToArray();

            if (_root == null)
                throw new InvalidOperationException("Kernel root scope was not initialized.");

            var worldConfig = cfg ?? new WorldConfig();
            var scope = CoreBootstrap.BuildWorldScope(worldConfig, _root);
            var world = new World(worldConfig, id, finalName, finalTags, this, scope);

            if (!_byId.TryAdd(id, world))
                throw new InvalidOperationException($"World with id {id} already exists.");

            IndexName(world);
            IndexTags(world);

            WorldCreated?.Invoke(world);

            if (Options.AutoSelectNewWorld || setAsCurrent)
                SetCurrentWorld(world);

            return world;
        }

        /// <inheritdoc/>
        public void DestroyWorld(IWorld world)
        {
            if (_byId.TryRemove(world.Id, out _))
            {
                DeindexName(world);
                DeindexTags(world);

                if (ReferenceEquals(_current, world))
                    ClearCurrentWorld();

                WorldDestroyed?.Invoke(world);
                world.Dispose();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IWorld> GetAllWorld()
        {
            return _byId.Values;
        }

        /// <inheritdoc/>
        public bool TryGet(WorldId id, out IWorld? world)
            => _byId.TryGetValue(id, out world);

        /// <inheritdoc/>
        public IEnumerable<IWorld> FindByName(string name)
        {
            HashSet<WorldId>? snapshot;
            lock (_nameLock)
            {
                if (!_byName.TryGetValue(name, out var set)) yield break;
                // Create a snapshot to avoid modification during iteration
                snapshot = new HashSet<WorldId>(set);
            }

            foreach (var id in snapshot)
            {
                if (_byId.TryGetValue(id, out var w))
                    yield return w;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IWorld> FindByTag(string tag)
        {
            HashSet<WorldId>? snapshot;
            lock (_tagLock)
            {
                if (!_byTag.TryGetValue(tag, out var set)) yield break;
                // Create a snapshot to avoid modification during iteration
                snapshot = new HashSet<WorldId>(set);
            }

            foreach (var id in snapshot)
            {
                if (_byId.TryGetValue(id, out var w))
                    yield return w;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IWorld> FindByAnyTag(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                yield break;

            // Normalize tags (trim + case-insensitive Distinct)
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tags)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    allowed.Add(t.Trim());
            }

            if (allowed.Count == 0)
                yield break;

            // Prevent duplicates of the same WorldId in results
            var seen = new HashSet<WorldId>();

            // Use lock to create a single snapshot for thread-safety
            List<KeyValuePair<string, HashSet<WorldId>>> snapshot;
            lock (_tagLock)
            {
                snapshot = new List<KeyValuePair<string, HashSet<WorldId>>>(_byTag);
            }

            foreach (var kv in snapshot)
            {
                var tag = kv.Key;
                if (!allowed.Contains(tag))
                    continue;

                // Create a snapshot of the HashSet to avoid modification during iteration
                var idSnapshot = new HashSet<WorldId>(kv.Value);

                foreach (var id in idSnapshot)
                {
                    if (!seen.Add(id))
                        continue;

                    if (_byId.TryGetValue(id, out var world) && world != null)
                        yield return world;
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IWorld> FindByNamePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                yield break;

            var p = prefix.Trim();
            var seen = new HashSet<WorldId>(); // avoid duplicates if a world matches multiple keys

            // Use lock to create a single snapshot for thread-safety
            List<KeyValuePair<string, HashSet<WorldId>>> snapshot;
            lock (_nameLock)
            {
                snapshot = new List<KeyValuePair<string, HashSet<WorldId>>>(_byName);
            }

            foreach (var kv in snapshot)
            {
                var name = kv.Key;
                if (name == null || !name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Create a snapshot of the HashSet to avoid modification during iteration
                var idSnapshot = new HashSet<WorldId>(kv.Value);

                foreach (var wid in idSnapshot)
                {
                    if (!seen.Add(wid)) continue;
                    if (_byId.TryGetValue(wid, out var world) && world != null)
                        yield return world;
                }
            }
        }

        /// <inheritdoc/>
        public void SetCurrentWorld(IWorld world)
        {
            SetCurrentWorld(new WorldHandle(this, world.Id));
        }

        /// <inheritdoc/>
        public void SetCurrentWorld(WorldHandle handle)
        {
            var w = handle.ResolveOrThrow();
            if (!ReferenceEquals(_current, w))
            {
                CurrentWorldChanged?.Invoke(_current, w);
                _current = w;
            }
        }

        /// <inheritdoc/>
        public void ClearCurrentWorld()
        {
            if (_current is null) return;
            CurrentWorldChanged?.Invoke(_current, null);
            _current = null;
        }

        // --- Update loop -----------------------------------------------------

        /// <inheritdoc/>
        public void BeginFrame(float dt)
        {
            if (dt < 0) dt = 0;
            _totalTime += dt;

            if (!IsRunning || IsPaused) return;

            _delta = dt;
            _firstFixedStepThisFrame = false;
            _simulationAccumulatorSeconds += dt;
            _frameCount++;

            if (Options is { StepOnlyCurrentWhenSelected: true } && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    _current.BeginFrameInternal(_current, dt);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    w.BeginFrameInternal(w, dt);
                }
            }
        }

        /// <inheritdoc/>
        public void FixedStep(float fixedDelta)
        {
            if (!IsRunning || IsPaused) return;

            if (!_firstFixedStepThisFrame)
            {
                _fixedFrameIndexInFrame = 0;
                _firstFixedStepThisFrame = true;
            }

            _fixedFrameCount++;
            _fixedFrameIndexInFrame++;
            _totalSimulatedSeconds += fixedDelta;

            if (Options is { StepOnlyCurrentWhenSelected: true } && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    _current.FixedStepInternal(_current, fixedDelta);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    w.FixedStepInternal(w, fixedDelta);
                }
            }
        }

        /// <inheritdoc/>
        public void LateFrame(float alpha = 1)
        {
            if (!IsRunning || IsPaused) return;

            if (Options is { StepOnlyCurrentWhenSelected: true } && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    _current.LateFrameInternal(_current, _delta, alpha);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    w.LateFrameInternal(w, _delta, alpha);
                }
            }
        }

        /// <inheritdoc/>
        public int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps)
        {
            if (!IsRunning || IsPaused)
            {
                return 0;
            }

            BeginFrame(dt);
            int sub = 0;
            while (_simulationAccumulatorSeconds >= fixedDelta && sub < maxSubSteps)
            {
                FixedStep(fixedDelta);
                _simulationAccumulatorSeconds -= fixedDelta;
                sub++;
            }

            var alpha = fixedDelta > 0f
                ? Math.Clamp(_simulationAccumulatorSeconds / fixedDelta, 0f, 1f)
                : 1f;

            LateFrame(alpha);
            return sub;
        }

        /// <inheritdoc/>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <inheritdoc/>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <inheritdoc/>
        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        // --- Index maintenance ----------------------------------------------

        /// <summary>
        /// Adds the world's name to the name index.
        /// </summary>
        /// <param name="w">World to index.</param>
        private void IndexName(IWorld w)
        {
            lock (_nameLock)
            {
                if (!_byName.TryGetValue(w.Name, out var set))
                    _byName[w.Name] = set = new HashSet<WorldId>();
                set.Add(w.Id);
            }
        }

        /// <summary>
        /// Removes the world's name from the name index.
        /// </summary>
        /// <param name="w">World to deindex.</param>
        private void DeindexName(IWorld w)
        {
            lock (_nameLock)
            {
                if (_byName.TryGetValue(w.Name, out var set))
                {
                    set.Remove(w.Id);
                    if (set.Count == 0)
                        _byName.Remove(w.Name, out _);
                }
            }
        }

        /// <summary>
        /// Adds all of the world's tags to the tag index.
        /// </summary>
        /// <param name="w">World to index.</param>
        private void IndexTags(IWorld w)
        {
            lock (_tagLock)
            {
                foreach (var t in w.Tags)
                {
                    if (!_byTag.TryGetValue(t, out var set))
                        _byTag[t] = set = new HashSet<WorldId>();
                    set.Add(w.Id);
                }
            }
        }

        /// <summary>
        /// Removes all of the world's tags from the tag index.
        /// </summary>
        /// <param name="w">World to deindex.</param>
        private void DeindexTags(IWorld w)
        {
            lock (_tagLock)
            {
                foreach (var t in w.Tags)
                {
                    if (_byTag.TryGetValue(t, out var set))
                    {
                        set.Remove(w.Id);
                        if (set.Count == 0)
                            _byTag.Remove(t, out _);
                    }
                }
            }
        }
    }
}