#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.DI;

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
        private float _simulationTime;
        private float _totalTime;
        private long _frameCount;
        private long _fixedFrameCount;
        private int _fixedFrameIndexInFrame;
        private bool _frameHasBegun;
        private bool _firstFixedStepThisFrame;
        private IWorld? _current;
        private KernelOptions? _options;
        
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float SimulationTimeSeconds => _simulationTime;
        public float TotalTimeSeconds => _totalTime;
        public long FrameCount => _frameCount;
        public long FixedFrameCount => _fixedFrameCount;
        public int FixedFrameIndexInFrame => _fixedFrameIndexInFrame;
        
        public IWorld? CurrentWorld => _current;
        public KernelOptions? Options => _options;

        public event Action<IWorld>? WorldCreated;
        public event Action<IWorld>? WorldDestroyed;
        public event Action<IWorld?>? CurrentWorldChanged;

        public event Action<IWorld, float>? OnBeginFrame;
        public event Action<IWorld, float>? OnFixedStep;
        public event Action<IWorld, float, float>? OnLateFrame;
        
        // --- Internal root services (DI/bootstrap), not exposed -------------
        private readonly ServiceContainer? _root;

        // --- Ctor ------------------------------------------------------------

        public Kernel(KernelOptions? options = null)
        {
            if (IsRunning) return;
            IsRunning = true;
            IsPaused = false;
            _simulationTime = 0;
            
            _options = options ?? new KernelOptions();
            _root = Internal.Bootstrap.CoreBootstrap.BuildRoot(_options);
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
        }

        // --- World Management ------------------------------------------------

        public IWorld CreateWorld(WorldConfig? cfg = null, string? name = null, IEnumerable<string>? tags = null, WorldId? presetId = null,
            bool setAsCurrent = false)
        {
            var id = presetId ?? Options.NewWorldId();
            var finalName = name ?? $"{Options.AutoNamePrefix}{id.Value.ToString("N")[..6]}";
            var finalTags = (tags ?? new []{""}).ToArray();

            var scope = Internal.Bootstrap.CoreBootstrap.BuildWorldScope(_root);
            var world = new Internal.World(cfg, id, finalName, finalTags, this, scope);

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

                world.Dispose();
                WorldDestroyed?.Invoke(world);
            }
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

        public void SetCurrentWorld(IWorld world)
        {
            SetCurrentWorld(new WorldHandle(this, world.Id));
        }
        
        public void SetCurrentWorld(WorldHandle handle)
        {
            var w = handle.ResolveOrThrow();
            if (!ReferenceEquals(_current, w))
            {
                _current = w;
                CurrentWorldChanged?.Invoke(_current);
            }
        }

        public void ClearCurrentWorld()
        {
            if (_current is null) return;
            _current = null;
            CurrentWorldChanged?.Invoke(null);
        }

        // --- Update  ---------------------------------------------------------

        public void BeginFrame(float dt)
        {
            if (dt < 0) dt = 0;
            _totalTime += dt;
            
            if (!IsRunning || IsPaused) return;

            _delta = dt;
            _frameHasBegun = true;
            _firstFixedStepThisFrame = false;
            _simulationTime += dt;
            _frameCount++;

            if (Options.StepOnlyCurrentWhenSelected && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    OnBeginFrame?.Invoke(_current, dt);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    OnBeginFrame?.Invoke(w, dt);
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
            
            if (Options.StepOnlyCurrentWhenSelected && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    OnFixedStep?.Invoke(_current, fixedDelta);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    OnFixedStep?.Invoke(w, fixedDelta);
                }
            }
        }

        public void LateFrame(float alpha = 1)
        {
            if (!IsRunning || IsPaused) return;

            _frameHasBegun = false;

            if (Options.StepOnlyCurrentWhenSelected && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    OnLateFrame?.Invoke(_current, _delta, alpha);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    OnLateFrame?.Invoke(w, _delta, alpha);
                }
            }
        }

        public int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha)
        {
            if (!IsRunning || IsPaused)
            {
                alpha = 1;
                return 0;
            }

            BeginFrame(dt);
            int sub = 0;
            while (_simulationTime >= fixedDelta && sub < maxSubSteps)
            {
                FixedStep(fixedDelta);
                _simulationTime -= fixedDelta;
                sub++;
            }
            alpha = fixedDelta > 0f ? Math.Clamp(_simulationTime / fixedDelta, 0f, 1f) : 1f;
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