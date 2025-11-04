#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.Bootstrap
{
    /// <summary>
    /// Minimal, allocation-light DI container for Core internal wiring.
    /// - Hierarchical scopes (root → per-world)
    /// - Singleton or factory (transient) registrations
    /// - Thread-safe resolution
    /// - Deterministic disposal of owned instances
    /// </summary>
    internal sealed class ServiceHost : IDisposable
    {
        private readonly ServiceHost? _parent;

        // Prebuilt singletons (or singleton instances produced by factories)
        private readonly ConcurrentDictionary<Type, object> _singletons = new();

        // Factories with lifetime flag
        private readonly ConcurrentDictionary<Type, FactoryEntry> _factories = new();

        // Objects this scope owns and will dispose
        private readonly ConcurrentBag<IDisposable> _owned = new();

        private volatile bool _disposed;

        private readonly struct FactoryEntry
        {
            public readonly Func<ServiceHost, object> Factory;
            public readonly bool AsSingleton;
            public FactoryEntry(Func<ServiceHost, object> factory, bool asSingleton)
            {
                Factory = factory; AsSingleton = asSingleton;
            }
        }

        public ServiceHost(ServiceHost? parent = null)
        {
            _parent = parent;
        }

        /// <summary>Create a child scope inheriting parent's lookups.</summary>
        public ServiceHost CreateChildScope() => new ServiceHost(this);

        // ---------- Registration ----------

        /// <summary>Register a prebuilt singleton instance. Optionally take ownership for disposal.</summary>
        public ServiceHost RegisterSingleton<T>(T instance, bool takeOwnership = false)
            where T : class
        {
            EnsureNotDisposed();
            var t = typeof(T);
            _singletons[t] = instance ?? throw new ArgumentNullException(nameof(instance));
            if (takeOwnership && instance is IDisposable d) _owned.Add(d);
            return this;
        }

        /// <summary>
        /// Register a factory. If <paramref name="asSingleton"/> is true, the first resolved instance is cached in this scope.
        /// Otherwise the factory is invoked on each resolve (transient).
        /// </summary>
        public ServiceHost RegisterFactory<T>(Func<ServiceHost, T> factory, bool asSingleton = false, bool takeOwnership = true)
            where T : class
        {
            EnsureNotDisposed();
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            var t = typeof(T);
            _factories[t] = new FactoryEntry(
                host => factory(host) ?? throw new InvalidOperationException($"Factory for {t} returned null."),
                asSingleton);

            // For singleton factories, we record ownership at creation time (when instance is materialized).
            // For transient, we won't track ownership (caller should own).
            return this;
        }

        /// <summary>Replace or upsert an existing singleton instance (same as RegisterSingleton, but explicit name for intent).</summary>
        public ServiceHost ReplaceSingleton<T>(T instance, bool takeOwnership = false) where T : class
            => RegisterSingleton(instance, takeOwnership);

        // ---------- Resolution ----------

        /// <summary>Get a required service or throw.</summary>
        public T GetRequired<T>() where T : class
        {
            if (TryGet<T>(out var v)) return v;
            throw new InvalidOperationException($"Service of type {typeof(T)} was not found in this scope (or its parents).");
        }

        /// <summary>Try to get a service from this scope or its parents.</summary>
        public bool TryGet<T>(out T value) where T : class
        {
            EnsureNotDisposed();
            var t = typeof(T);

            // 1) Singleton table
            if (_singletons.TryGetValue(t, out var s))
            {
                value = (T)s;
                return true;
            }

            // 2) Factory in this scope
            if (_factories.TryGetValue(t, out var entry))
            {
                var obj = entry.AsSingleton
                    ? _singletons.GetOrAdd(t, _ =>
                    {
                        var created = entry.Factory(this);
                        if (created is IDisposable d) _owned.Add(d);
                        return created;
                    })
                    : entry.Factory(this);

                value = (T)obj;
                return true;
            }

            // 3) Parent scope
            if (_parent is not null && _parent.TryGet(out value))
                return true;

            value = default!;
            return false;
        }

        /// <summary>Internal generic TryGet with Type param.</summary>
        private bool TryGet(out object value, Type t)
        {
            var method = typeof(ServiceHost).GetMethod(nameof(TryGetGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var generic = method.MakeGenericMethod(t);
            var args = new object?[] { null };
            var ok = (bool)generic.Invoke(this, args)!;
            value = args[0]!;
            return ok;
        }

        private bool TryGetGeneric<T>(out T value) where T : class => TryGet<T>(out value);

        /// <summary>Whether this scope contains a registration or instance for T (without searching parents).</summary>
        public bool ContainsLocal<T>() where T : class
            => _singletons.ContainsKey(typeof(T)) || _factories.ContainsKey(typeof(T));

        // ---------- Disposal ----------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Dispose owned instances for this scope only
            while (_owned.TryTake(out var d))
            {
                try { d.Dispose(); }
                catch { /* swallow to ensure deterministic cleanup */ }
            }

            _singletons.Clear();
            _factories.Clear();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceHost));
        }
    }
}
