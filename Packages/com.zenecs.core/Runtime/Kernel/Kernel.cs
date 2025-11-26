// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: Kernel.cs
// Purpose: Central coordinator that owns the lifetime, lookup, and stepping of
//          multiple independent Worlds. It composes the root DI container,
//          creates per‑world scopes, and drives simulation (Begin/Fixed/Late).
// Key concepts:
//   • Multi‑world manager: create/destroy, find by id/name/tag, select current.
//   • Deterministic stepping: BeginFrame(dt) → FixedStep(fixedDelta)×N → LateFrame(alpha).
//   • Accumulator-based fixed update: PumpAndLateFrame() consumes dt into fixed substeps.
//   • Events as hooks: OnBeginFrame/OnFixedStep/OnLateFrame per world.
//   • DI/bootstrap: builds a root ServiceContainer and per‑world scopes.
//   • Indexes: concurrent maps for id/name/tag with thread‑safe snapshots.
//   • Pause & selection semantics: optionally step only the currently selected world.
//   • Time tracking: frame counters and total simulated seconds (fixed‑step only).
//   • Adapter‑friendly: Unity/Godot/etc. call the three ticks from their loops.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Abstractions.Config;
using ZenECS.Core.Abstractions.Diagnostics;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.Bootstrap;
using ZenECS.Core.Internal.DI;

namespace ZenECS.Core
{
    /// <summary>
    /// Manages the lifecycle, lookup and ticking of multiple Worlds.
    /// External code must interact with World state via the world's API surfaces,
    /// not through Kernel internals.
    /// </summary>
    public sealed class Kernel : IKernel
    {
        // --- Indexes ---------------------------------------------------------

        // id -> world
        private readonly ConcurrentDictionary<WorldId, IWorld> _byId = new();

        // name -> set of world ids
        private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byName =
            new(StringComparer.Ordinal);

        // tag -> set of world ids
        private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byTag =
            new(StringComparer.Ordinal);

        private readonly object _nameLock = new();
        private readonly object _tagLock = new();

        // --- State -----------------------------------------------------------

        private float _delta;
        private float _simulationAccumulatorSeconds;
        private float _totalTime;
        private long _frameCount;
        private long _fixedFrameCount;
        private int _fixedFrameIndexInFrame;
        private bool _firstFixedStepThisFrame;
        private double _totalSimulatedSeconds; // Accumulated time processed by FixedStep (seconds)
        private KernelOptions? _options;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float SimulationAccumulatorSeconds => _simulationAccumulatorSeconds;
        public float TotalTimeSeconds => _totalTime;
        public long FrameCount => _frameCount;
        public long FixedFrameCount => _fixedFrameCount;
        public int FixedFrameIndexInFrame => _fixedFrameIndexInFrame;
        public double TotalSimulatedSeconds => _totalSimulatedSeconds;

        private IWorld? _current;

        public IWorld? CurrentWorld => _current;
        public KernelOptions? Options => _options;

        public event Action<IWorld>? WorldCreated;
        public event Action<IWorld>? WorldDestroyed;
        public event Action<IWorld?, IWorld?>? CurrentWorldChanged;
        public event Action? Disposed;

        // --- Internal root services (DI/bootstrap), not exposed -------------
        private readonly ServiceContainer? _root;

        // --- Ctor ------------------------------------------------------------

        public Kernel(KernelOptions? options = null, IEcsLogger? logger = null)
        {
            if (IsRunning) return;
            IsRunning = true;
            IsPaused = false;
            _simulationAccumulatorSeconds = 0;

            _options = options ?? new KernelOptions();
            if (logger != null) EcsRuntimeOptions.Log = logger;
            _root = CoreBootstrap.BuildRoot(_options);
        }

        public void Dispose()
        {
            if (!IsRunning) return;

            // Destroy all worlds
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

        public IWorld CreateWorld(WorldConfig? cfg = null, string? name = null, IEnumerable<string>? tags = null,
            WorldId? presetId = null,
            bool setAsCurrent = false)
        {
            if (Options == null)
            {
                throw new InvalidOperationException($"World has been no options");
            }
            
            var id = presetId ?? Options.NewWorldId();
            var finalName = name ?? $"{Options.AutoNamePrefix}{id.Value.ToString("N")[..6]}";
            var finalTags = (tags ?? Array.Empty<string>()).ToArray();

            if (_root == null)
            {
                throw new InvalidOperationException($"World has been no _root scope");
            }

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

        public void DestroyWorld(IWorld world)
        {
            if (_byId.TryRemove(world.Id, out _))
            {
                DeindexName(world);
                DeindexTags(world);

                if (ReferenceEquals(_current, world))
                {
                    ClearCurrentWorld();
                }

                WorldDestroyed?.Invoke(world);
                world.Dispose();
            }
        }

        public IEnumerable<IWorld> GetAllWorld()
        {
            return _byId.Values;
        }
        
        public bool TryGet(WorldId id, out IWorld world)
            => _byId.TryGetValue(id, out world!);

        public IEnumerable<IWorld> FindByName(string name)
        {
            if (!_byName.TryGetValue(name, out var set)) yield break;
            foreach (var id in set)
                if (_byId.TryGetValue(id, out var w))
                    yield return w;
        }

        public IEnumerable<IWorld> FindByTag(string tag)
        {
            if (!_byTag.TryGetValue(tag, out var set)) yield break;
            foreach (var id in set)
                if (_byId.TryGetValue(id, out var w))
                    yield return w;
        }

        public IEnumerable<IWorld> FindByAnyTag(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                yield break;

            // Normalize tags (trim + case-insensitive Distinct)
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tags)
                if (!string.IsNullOrWhiteSpace(t))
                    allowed.Add(t.Trim());
            if (allowed.Count == 0)
                yield break;

            // Prevent duplicates of the same WorldId in results
            var seen = new HashSet<WorldId>();

            // ConcurrentDictionary snapshot with ToArray() is safe.
            foreach (var kv in _byTag.ToArray()) // KeyValuePair<string, HashSet<WorldId>>
            {
                var tag = kv.Key;
                if (!allowed.Contains(tag))
                    continue;

                // HashSet is not thread-safe; take a snapshot before iterating.
                WorldId[] ids = kv.Value.ToArray();

                foreach (var id in ids)
                {
                    if (!seen.Add(id)) // already yielded this world
                        continue;

                    if (_byId.TryGetValue(id, out var world) && world != null)
                        yield return world;
                }
            }
        }

        public IEnumerable<IWorld> FindByNamePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                yield break;

            var p = prefix.Trim();
            var seen = new HashSet<WorldId>(); // avoid duplicates if a world matches multiple keys

            // Snapshot iteration for thread-safety
            foreach (var kv in _byName.ToArray())
            {
                var name   = kv.Key;
                var idSet  = kv.Value; // HashSet<WorldId>
                if (name == null || !name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var wid in idSet.ToArray())
                {
                    if (!seen.Add(wid)) continue;
                    if (_byId.TryGetValue(wid, out var world) && world != null)
                        yield return world;
                }
            }
        }

        public void SetCurrentWorld(IWorld world)
        {
            SetCurrentWorld(new WorldHandle(this, world.Id));
        }

        public void SetCurrentWorld(WorldHandle handle)
        {
            var w = handle.ResolveOrThrow();
            if (!ReferenceEquals(_current, w))
            {
                CurrentWorldChanged?.Invoke(_current, w);
                _current = w;
            }
        }

        public void ClearCurrentWorld()
        {
            if (_current is null) return;
            CurrentWorldChanged?.Invoke(_current, null);
            _current = null;
        }

        // --- Update  ---------------------------------------------------------

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
                    var internalWorld = _current as World;
                    internalWorld?.BeginFrame(_current, dt);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    var internalWorld = w as World;
                    internalWorld?.BeginFrame(w, dt);
                }
            }
        }

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
                    var internalWorld = _current as World;
                    internalWorld?.FixedStep(_current, fixedDelta);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    var internalWorld = w as World;
                    internalWorld?.FixedStep(w, fixedDelta);
                }
            }
        }

        public void LateFrame(float alpha = 1)
        {
            if (!IsRunning || IsPaused) return;

            if (Options is { StepOnlyCurrentWhenSelected: true } && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    var internalWorld = _current as World;
                    internalWorld?.LateFrame(_current, _delta, alpha);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    var internalWorld = w as World;
                    internalWorld?.LateFrame(w, _delta, alpha);
                }
            }
        }

        public int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps)
        {
            if (!IsRunning || IsPaused)
            {
                LateFrame(1);
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

            var alpha = fixedDelta > 0f ? Math.Clamp(_simulationAccumulatorSeconds / fixedDelta, 0f, 1f) : 1f;
            LateFrame(alpha);
            return sub;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        // --- Index maintenance ----------------------------------------------

        private void IndexName(IWorld w)
        {
            lock (_nameLock)
            {
                if (!_byName.TryGetValue(w.Name, out var set))
                    _byName[w.Name] = set = new HashSet<WorldId>();
                set.Add(w.Id);
            }
        }

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
