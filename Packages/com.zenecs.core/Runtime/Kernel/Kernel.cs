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

        private IWorld? _current;
        private float _uptime;

        public bool IsRunning { get; private set; }
        public float UptimeSeconds => _uptime;
        public IWorld? CurrentWorld => _current;
        public KernelOptions Options { get; }

        public event Action<IWorld>? WorldCreated;
        public event Action<IWorld>? WorldDestroyed;
        public event Action<IWorld?>? CurrentWorldChanged;

        public event Action<IWorld, float>? OnBeginFrame;
        public event Action<IWorld, float>? OnFixedStep;
        public event Action<IWorld, float>? OnLateFrame;
        
        // --- Internal root services (DI/bootstrap), not exposed -------------
        private readonly ServiceHost _root;

        // --- Ctor ------------------------------------------------------------

        public Kernel(KernelOptions? options = null)
        {
            Options = options ?? new KernelOptions();
            _root = Internal.Bootstrap.CoreBootstrap.BuildRoot();
        }

        // --- Lifecycle -------------------------------------------------------

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _uptime = 0;
        }

        public void Shutdown()
        {
            if (!IsRunning) return;

            // Destroy all worlds
            foreach (var w in _byId.Values.ToArray())
                DestroyWorld(w);

            _current = null;
            IsRunning = false;
        }

        // --- World Management ------------------------------------------------

        public IWorld CreateWorld(bool setAsCurrent = false)
        {
            return CreateWorld(null, null, null, setAsCurrent);
        }
        
        public IWorld CreateWorld(string? name = null, IEnumerable<string>? tags = null, bool setAsCurrent = false)
        {
            return CreateWorld(name, tags, null, setAsCurrent);
        }

        public IWorld CreateWorld(string? name = null, IEnumerable<string>? tags = null, WorldId? presetId = null,
            bool setAsCurrent = false)
        {
            var id = presetId ?? Options.NewWorldId();
            var finalName = name ?? $"{Options.AutoNamePrefix}{id.Value.ToString("N")[..6]}";
            var finalTags = (tags ?? Array.Empty<string>()).ToArray();

            var scope = Internal.Bootstrap.CoreBootstrap.BuildWorldScope(_root);
            var world = new Internal.WorldImpl(id, finalName, finalTags, this, scope);

            if (!_byId.TryAdd(id, world))
                throw new InvalidOperationException($"World with id {id} already exists.");

            IndexName(world);
            IndexTags(world);

            WorldCreated?.Invoke(world);

            // ★ 새 옵션: 생성 직후 현재 월드로 자동 지정
            if (setAsCurrent)
                SetCurrentWorld(new WorldHandle(this, world.Id));

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
                    _current = null;
                    CurrentWorldChanged?.Invoke(null);
                }

                //world.Dispose();
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
            if (!IsRunning) return;
            if (dt < 0) dt = 0;

            _uptime += dt;

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
            if (!IsRunning) return;

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
            if (!IsRunning) return;

            if (Options.StepOnlyCurrentWhenSelected && _current is not null)
            {
                if (!_current.IsPaused)
                {
                    OnLateFrame?.Invoke(_current, alpha);
                }
                return;
            }

            foreach (var w in _byId.Values)
            {
                if (!w.IsPaused)
                {
                    OnLateFrame?.Invoke(w, alpha);
                }
            }
        }

        public int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha)
        {
            if (!IsRunning)
            {
                alpha = 1;
                return 0;
            }

            BeginFrame(dt);
            int sub = 0;
            while (_uptime >= fixedDelta && sub < maxSubSteps)
            {
                FixedStep(fixedDelta);
                _uptime -= fixedDelta;
                sub++;
            }
            alpha = fixedDelta > 0f ? Math.Clamp(_uptime / fixedDelta, 0f, 1f) : 1f;
            return sub;
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

        // --- IDisposable -----------------------------------------------------

        public void Dispose()
        {
            Shutdown();
        }
    }
}

// ------------------------
// Internal namespace notes
// ------------------------
// - ZenECS.Core.Internal.Bootstrap.CoreBootstrap.BuildRoot(): builds process-wide service root.
// - ZenECS.Core.Internal.Bootstrap.CoreBootstrap.BuildWorldScope(ServiceHost root): builds per-world scope.
// - ZenECS.Core.Internal.WorldImpl : IWorld
//
// These are intentionally internal and are implemented under Runtime/Internal/*.