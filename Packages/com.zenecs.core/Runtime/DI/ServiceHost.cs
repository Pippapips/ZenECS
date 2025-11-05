using System;
using System.Collections.Generic;
using System.Text;

namespace ZenECS.Core.DI
{
    /// <summary>
    /// Lightweight hierarchical DI/Service registry for Core internals.
    /// - Parent/child scopes (CreateChildScope)
    /// - Singleton instance registration (ownership optional)
    /// - Factory registration (transient / singleton with lazy cache, ownership optional)
    /// - Deterministic dispose in reverse registration order
    /// - Freeze() to lock registrations after composition
    /// - TryGet(Type) / GetRequired(Type) non-generic overloads
    /// - Optional multi-registration support + GetAll<T>()
    /// </summary>
    public sealed class ServiceHost : IDisposable
    {
        private readonly object _gate = new();
        private bool _disposed;
        private bool _frozen;

        private readonly ServiceHost? _parent;

        // Multi-registration friendly: lists per service type
        private readonly Dictionary<Type, List<object>> _singletons = new();
        private readonly Dictionary<Type, List<FactoryEntry>> _factories = new();

        // Ownership tracking in registration order (reverse-dispose)
        private readonly List<IDisposable> _owned = new();

        private readonly List<ServiceHost> _children = new();

        private sealed class FactoryEntry
        {
            public Func<ServiceHost, object> Factory = default!;
            public bool AsSingleton;
            public bool TakeOwnership;
            // Lazy cache only when AsSingleton == true
            public bool Cached;
            public object? Cache;
        }

        public ServiceHost(ServiceHost? parent = null)
        {
            _parent = parent;
            _parent?._children.Add(this);
        }

        public ServiceHost CreateChildScope() => new(this);

        // ---------------------------
        // Registration
        // ---------------------------
        private void EnsureWritable()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceHost));
            if (_frozen) throw new InvalidOperationException("ServiceHost is frozen (no further registrations).");
        }

        public ServiceHost Freeze()
        {
            lock (_gate)
            {
                _frozen = true;
            }
            return this;
        }

        public ServiceHost RegisterSingleton<T>(T instance, bool takeOwnership = true) where T : class
            => AppendSingleton(typeof(T), instance!, takeOwnership);

        public ServiceHost AppendSingleton<T>(T instance, bool takeOwnership = true) where T : class
            => AppendSingleton(typeof(T), instance!, takeOwnership);

        public ServiceHost RegisterFactory<T>(
            Func<ServiceHost, T> factory,
            bool asSingleton = false,
            bool takeOwnership = true) where T : class
            => AppendFactory(typeof(T), h => factory(h)!, asSingleton, takeOwnership);

        // Non generic registrations (optional)
        public ServiceHost RegisterSingleton(Type serviceType, object instance, bool takeOwnership = true)
            => AppendSingleton(serviceType, instance, takeOwnership);

        public ServiceHost RegisterFactory(Type serviceType,
            Func<ServiceHost, object> factory,
            bool asSingleton = false,
            bool takeOwnership = true)
            => AppendFactory(serviceType, factory, asSingleton, takeOwnership);

        private ServiceHost AppendSingleton(Type t, object instance, bool takeOwnership)
        {
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            lock (_gate)
            {
                EnsureWritable();
                if (!_singletons.TryGetValue(t, out var list))
                {
                    list = new List<object>(1);
                    _singletons[t] = list;
                }
                list.Add(instance);
                if (takeOwnership && instance is IDisposable d)
                    _owned.Add(d);
            }
            return this;
        }

        private ServiceHost AppendFactory(Type t, Func<ServiceHost, object> factory, bool asSingleton, bool takeOwnership)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            lock (_gate)
            {
                EnsureWritable();
                if (!_factories.TryGetValue(t, out var list))
                {
                    list = new List<FactoryEntry>(1);
                    _factories[t] = list;
                }
                list.Add(new FactoryEntry
                {
                    Factory = factory,
                    AsSingleton = asSingleton,
                    TakeOwnership = takeOwnership,
                    Cached = false,
                    Cache = null
                });
            }
            return this;
        }

        // ---------------------------
        // Resolve (single)
        // ---------------------------
        public T GetRequired<T>() where T : class
            => (T)GetRequired(typeof(T));

        public object GetRequired(Type t)
        {
            if (!TryGet(t, out var svc))
                throw new KeyNotFoundException($"Service not found: {t.FullName}");
            return svc!;
        }

        public bool TryGet<T>(out T? value) where T : class
        {
            var ok = TryGet(typeof(T), out var obj);
            value = ok ? (T)obj! : null;
            return ok;
        }

        public bool TryGet(Type t, out object? value)
        {
            if (_disposed) { value = null; return false; }
            lock (_gate)
            {
                // Local singletons
                if (_singletons.TryGetValue(t, out var list) && list.Count > 0)
                {
                    value = list[0];
                    return true;
                }

                // Local factories
                if (_factories.TryGetValue(t, out var flist) && flist.Count > 0)
                {
                    var fe = flist[0];
                    if (fe.AsSingleton)
                    {
                        if (!fe.Cached)
                        {
                            fe.Cache = CreateViaFactory(fe, t);
                            fe.Cached = true;
                        }
                        value = fe.Cache!;
                        return true;
                    }
                    value = CreateViaFactory(fe, t);
                    return true;
                }
            }

            // Parent fallback
            if (_parent is not null)
                return _parent.TryGet(t, out value);

            value = null;
            return false;
        }

        // ---------------------------
        // Resolve (multiple)
        // ---------------------------
        public IReadOnlyList<T> GetAll<T>() where T : class
        {
            var t = typeof(T);
            var result = new List<T>();
            CollectAll(t, result, includeParent: true);
            return result;
        }

        private void CollectAll<T>(Type t, List<T> result, bool includeParent) where T : class
        {
            lock (_gate)
            {
                if (_singletons.TryGetValue(t, out var sl))
                {
                    foreach (var o in sl)
                        result.Add((T)o);
                }
                if (_factories.TryGetValue(t, out var fl))
                {
                    foreach (var fe in fl)
                    {
                        if (fe.AsSingleton)
                        {
                            if (!fe.Cached) { fe.Cache = CreateViaFactory(fe, t); fe.Cached = true; }
                            result.Add((T)fe.Cache!);
                        }
                        else
                        {
                            // transient: 매 호출마다 생성 (의도적)
                            result.Add((T)CreateViaFactory(fe, t));
                        }
                    }
                }
            }
            if (includeParent && _parent is not null)
                _parent.CollectAll(t, result, includeParent: true);
        }

        private object CreateViaFactory(FactoryEntry fe, Type t)
        {
            var obj = fe.Factory(this) ?? throw new InvalidOperationException($"Factory returned null for {t.FullName}");
            if (fe.TakeOwnership && obj is IDisposable d)
                _owned.Add(d);
            return obj;
        }

        // ---------------------------
        // Introspection / Verify / Dump
        // ---------------------------
        public bool ContainsLocal<T>() where T : class
        {
            var t = typeof(T);
            lock (_gate) return (_singletons.ContainsKey(t) || _factories.ContainsKey(t));
        }

        public bool Contains<T>() where T : class
            => TryGet(typeof(T), out _);

        public void Verify(params Type[] required)
        {
            foreach (var t in required)
                _ = GetRequired(t);
        }

        public string Dump(bool includeParent = true)
        {
            var sb = new StringBuilder();
            lock (_gate)
            {
                sb.AppendLine($"ServiceHost (frozen={_frozen}, disposed={_disposed})");
                foreach (var kv in _singletons)
                    sb.AppendLine($"  [S] {kv.Key.FullName} x{kv.Value.Count}");
                foreach (var kv in _factories)
                    sb.AppendLine($"  [F] {kv.Key.FullName} x{kv.Value.Count}");
            }
            if (includeParent && _parent is not null)
            {
                sb.AppendLine("  └─ parent:");
                sb.Append(_parent.Dump(includeParent: false));
            }
            return sb.ToString();
        }

        // ---------------------------
        // Dispose (deterministic reverse)
        // ---------------------------
        public void Dispose()
        {
            List<IDisposable> ownedSnapshot;
            List<ServiceHost> childrenSnapshot;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                ownedSnapshot = new List<IDisposable>(_owned);
                childrenSnapshot = new List<ServiceHost>(_children);
                _owned.Clear();
                _children.Clear();
                _singletons.Clear();
                _factories.Clear();
            }

            // 먼저 자식 스코프를 종료 (깊은 자원부터)
            for (int i = childrenSnapshot.Count - 1; i >= 0; i--)
            {
                try { childrenSnapshot[i].Dispose(); } catch { /* swallow */ }
            }

            // 자신이 소유한 자원 역순 폐기
            for (int i = ownedSnapshot.Count - 1; i >= 0; i--)
            {
                try { ownedSnapshot[i].Dispose(); } catch { /* swallow */ }
            }
        }
    }
}
