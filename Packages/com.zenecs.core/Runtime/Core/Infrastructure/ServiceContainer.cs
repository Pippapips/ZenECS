// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Lightweight DI
// File: ServiceContainer.cs
// Purpose: Minimal hierarchical service container for Core internals (no deps).
// Key concepts:
//   • Parent/child scopes with deterministic reverse dispose.
//   • Singletons & factories (transient or cached singleton) with ownership.
//   • Freeze (Seal) after composition for safety and predictability.
//   • Introspection: Verify / Dump / Contains / GetAll helpers for tooling/tests.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace ZenECS.Core.Infrastructure.Internal
{
    /// <summary>
    /// Lightweight hierarchical DI/Service registry for Core internals.
    /// </summary>
    /// <remarks>
    /// Designed for Core's internal needs; not a general-purpose DI replacement.
    /// Thread-safe for registration and resolution via a single internal lock.
    /// </remarks>
    internal sealed class ServiceContainer : IDisposable
    {
        private readonly object _gate = new();
        private bool _disposed;
        private bool _sealed;

        private readonly ServiceContainer? _parent;

        // Multi-registration friendly: lists per service type
        private readonly Dictionary<Type, List<object>> _singletons = new();
        private readonly Dictionary<Type, List<FactoryEntry>> _factories = new();

        // Ownership tracking in registration order (reverse-dispose)
        private readonly List<IDisposable> _owned = new();

        private readonly List<ServiceContainer> _children = new();

        /// <summary>
        /// Internal record for registered factories.
        /// </summary>
        private sealed class FactoryEntry
        {
            /// <summary>
            /// Factory function that creates the service instance.
            /// </summary>
            public Func<ServiceContainer, object> Factory = default!;

            /// <summary>
            /// When <see langword="true"/>, the first created instance is cached and reused (singleton-like).
            /// When <see langword="false"/>, the factory is invoked each time (transient).
            /// </summary>
            public bool AsSingleton;

            /// <summary>
            /// Whether the container should track and dispose the created instance.
            /// </summary>
            public bool TakeOwnership;

            /// <summary>
            /// Indicates that a cached instance was already created.
            /// </summary>
            public bool Cached;

            /// <summary>
            /// The cached instance for singleton factories.
            /// </summary>
            public object? Cache;
        }

        /// <summary>
        /// Create a new container (optionally with a parent scope).
        /// </summary>
        /// <param name="parent">Parent scope; disposing this container will dispose children first.</param>
        public ServiceContainer(ServiceContainer? parent = null)
        {
            _parent = parent;
            _parent?._children.Add(this);
        }

        /// <summary>
        /// Create a child scope whose lifetime is tied to this container.
        /// </summary>
        /// <returns>A new <see cref="ServiceContainer"/> that uses this instance as its parent.</returns>
        public ServiceContainer CreateChildScope() => new(this);

        // --------------------------- Registration ---------------------------

        /// <summary>
        /// Ensure the container is writable (not sealed and not disposed).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container is sealed.</exception>
        private void EnsureWritable()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceContainer));
            if (_sealed) throw new InvalidOperationException("ServiceHost is frozen (no further registrations).");
        }

        /// <summary>
        /// Prevent further registrations (freezes the container). Call after composition.
        /// </summary>
        /// <returns>This container (for chaining).</returns>
        /// <exception cref="ObjectDisposedException">Container already disposed.</exception>
        public ServiceContainer Seal()
        {
            lock (_gate)
            {
                _sealed = true;
            }
            return this;
        }

        /// <summary>
        /// Register a singleton instance for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <param name="instance">Instance to register.</param>
        /// <param name="takeOwnership">
        /// If <see langword="true"/>, the container will dispose it when disposed.
        /// </param>
        /// <returns>This container (for chaining).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
        public ServiceContainer RegisterSingleton<T>(T instance, bool takeOwnership = true) where T : class
            => AppendSingleton(typeof(T), instance!, takeOwnership);

        /// <summary>
        /// Append an additional singleton instance for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <param name="instance">Instance to append.</param>
        /// <param name="takeOwnership">Whether to dispose the instance with the container.</param>
        /// <returns>This container (for chaining).</returns>
        public ServiceContainer AppendSingleton<T>(T instance, bool takeOwnership = true) where T : class
            => AppendSingleton(typeof(T), instance!, takeOwnership);

        /// <summary>
        /// Register a factory for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <param name="factory">Factory function (this container is passed in).</param>
        /// <param name="asSingleton">If true, cache first result and reuse.</param>
        /// <param name="takeOwnership">Whether to dispose created instances.</param>
        /// <returns>This container (for chaining).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public ServiceContainer RegisterFactory<T>(
            Func<ServiceContainer, T> factory,
            bool asSingleton = false,
            bool takeOwnership = true) where T : class
            => AppendFactory(typeof(T), h => factory(h)!, asSingleton, takeOwnership);

        /// <summary>
        /// Non-generic singleton registration.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <param name="instance">Instance to register.</param>
        /// <param name="takeOwnership">Whether to dispose the instance with the container.</param>
        /// <returns>This container (for chaining).</returns>
        public ServiceContainer RegisterSingleton(Type serviceType, object instance, bool takeOwnership = true)
            => AppendSingleton(serviceType, instance, takeOwnership);

        /// <summary>
        /// Non-generic factory registration.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <param name="factory">Factory function (this container is passed in).</param>
        /// <param name="asSingleton">If true, cache first result and reuse.</param>
        /// <param name="takeOwnership">Whether to dispose created instances.</param>
        /// <returns>This container (for chaining).</returns>
        public ServiceContainer RegisterFactory(
            Type serviceType,
            Func<ServiceContainer, object> factory,
            bool asSingleton = false,
            bool takeOwnership = true)
            => AppendFactory(serviceType, factory, asSingleton, takeOwnership);

        /// <summary>
        /// Implementation for singleton registration.
        /// </summary>
        /// <param name="t">Service contract type.</param>
        /// <param name="instance">Instance to register.</param>
        /// <param name="takeOwnership">Whether to dispose the instance with the container.</param>
        /// <returns>This container (for chaining).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
        private ServiceContainer AppendSingleton(Type t, object instance, bool takeOwnership)
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

        /// <summary>
        /// Implementation for factory registration.
        /// </summary>
        /// <param name="t">Service contract type.</param>
        /// <param name="factory">Factory function that creates the service.</param>
        /// <param name="asSingleton">If true, cache first result and reuse.</param>
        /// <param name="takeOwnership">Whether to dispose created instances.</param>
        /// <returns>This container (for chaining).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        private ServiceContainer AppendFactory(Type t, Func<ServiceContainer, object> factory, bool asSingleton, bool takeOwnership)
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

        // --------------------------- Resolve (single) ---------------------------

        /// <summary>
        /// Resolve type <typeparamref name="T"/> or throw if missing.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <returns>Resolved service instance.</returns>
        /// <exception cref="KeyNotFoundException">No registration was found for the type.</exception>
        public T GetRequired<T>() where T : class => (T)GetRequired(typeof(T));

        /// <summary>
        /// Resolve a service by <see cref="Type"/> or throw if missing.
        /// </summary>
        /// <param name="t">Service contract type.</param>
        /// <returns>Resolved service instance.</returns>
        /// <exception cref="KeyNotFoundException">No registration was found for the type.</exception>
        public object GetRequired(Type t)
        {
            if (!TryGet(t, out var svc))
                throw new KeyNotFoundException($"Service not found: {t.FullName}");
            return svc!;
        }

        /// <summary>
        /// Try resolve a service by type parameter.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <param name="value">Resolved instance or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if resolved successfully; otherwise <see langword="false"/>.</returns>
        public bool TryGet<T>(out T? value) where T : class
        {
            var ok = TryGet(typeof(T), out var obj);
            value = ok ? (T)obj! : null;
            return ok;
        }

        /// <summary>
        /// Try resolve a service by <see cref="Type"/> (parent chain considered).
        /// </summary>
        /// <param name="t">Service contract type.</param>
        /// <param name="value">Resolved instance or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if resolved successfully; otherwise <see langword="false"/>.</returns>
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

        // --------------------------- Resolve (multiple) ---------------------------

        /// <summary>
        /// Resolve all registrations for type <typeparamref name="T"/> in this scope and parents.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <returns>A read-only snapshot of all resolved instances.</returns>
        /// <remarks>
        /// Singleton factories are cached; transient factories create new instances per call.
        /// </remarks>
        public IReadOnlyList<T> GetAll<T>() where T : class
        {
            var t = typeof(T);
            var result = new List<T>();
            CollectAll(t, result, includeParent: true);
            return result;
        }

        /// <summary>
        /// Collect all instances for <paramref name="t"/> into <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        /// <param name="t">Service type to collect.</param>
        /// <param name="result">Output collection.</param>
        /// <param name="includeParent">Whether to include parent scopes.</param>
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
                            // transient: intentionally create a new instance per call
                            result.Add((T)CreateViaFactory(fe, t));
                        }
                    }
                }
            }
            if (includeParent && _parent is not null)
                _parent.CollectAll(t, result, includeParent: true);
        }

        /// <summary>
        /// Create an instance via factory and record ownership when configured.
        /// </summary>
        /// <param name="fe">Factory entry.</param>
        /// <param name="t">Service type (for diagnostics).</param>
        /// <returns>Created instance.</returns>
        /// <exception cref="InvalidOperationException">Factory returned null.</exception>
        private object CreateViaFactory(FactoryEntry fe, Type t)
        {
            var obj = fe.Factory(this) ?? throw new InvalidOperationException($"Factory returned null for {t.FullName}");
            if (fe.TakeOwnership && obj is IDisposable d)
                _owned.Add(d);
            return obj;
        }

        // --------------------------- Introspection / Verify / Dump ---------------------------

        /// <summary>
        /// Returns <see langword="true"/> if type <typeparamref name="T"/> is registered locally (in this scope only).
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        public bool ContainsLocal<T>() where T : class
        {
            var t = typeof(T);
            lock (_gate) return (_singletons.ContainsKey(t) || _factories.ContainsKey(t));
        }

        /// <summary>
        /// Returns <see langword="true"/> if type <typeparamref name="T"/> can be resolved from this container or any parent.
        /// </summary>
        /// <typeparam name="T">Service contract type.</typeparam>
        public bool Contains<T>() where T : class => TryGet(typeof(T), out _);

        /// <summary>
        /// Assert that all <paramref name="required"/> service types are resolvable; throws if not.
        /// </summary>
        /// <param name="required">Service types that must be resolvable.</param>
        /// <exception cref="KeyNotFoundException">Thrown if any required type cannot be resolved.</exception>
        public void Verify(params Type[] required)
        {
            foreach (var t in required)
                _ = GetRequired(t);
        }

        /// <summary>
        /// Produce a human-readable dump of registrations for diagnostics.
        /// </summary>
        /// <param name="includeParent">Include parent container content.</param>
        /// <returns>A formatted multi-line string summarizing registrations.</returns>
        public string Dump(bool includeParent = true)
        {
            var sb = new StringBuilder();
            lock (_gate)
            {
                sb.AppendLine($"ServiceHost (frozen={_sealed}, disposed={_disposed})");
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

        // --------------------------- Dispose (deterministic reverse) ---------------------------

        /// <summary>
        /// Dispose child scopes first, then owned instances in reverse registration order.
        /// Individual dispose exceptions are swallowed to maximize robustness.
        /// </summary>
        public void Dispose()
        {
            List<IDisposable> ownedSnapshot;
            List<ServiceContainer> childrenSnapshot;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                ownedSnapshot = new List<IDisposable>(_owned);
                childrenSnapshot = new List<ServiceContainer>(_children);
                _owned.Clear();
                _children.Clear();
                _singletons.Clear();
                _factories.Clear();
            }

            // Dispose child scopes first (deep resources first)
            for (int i = childrenSnapshot.Count - 1; i >= 0; i--)
            {
                try { childrenSnapshot[i].Dispose(); } catch { /* swallow */ }
            }

            // Then dispose owned resources in reverse registration order
            for (int i = ownedSnapshot.Count - 1; i >= 0; i--)
            {
                try { ownedSnapshot[i].Dispose(); } catch { /* swallow */ }
            }
        }
    }
}
