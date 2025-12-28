// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: ComponentPoolRepository.cs
// Purpose: Map component Type → IComponentPool and create pools on demand.
// Key concepts:
//   • Lazy factories: closed generic ComponentPool<T> via cached delegates.
//   • Per-world repository: fast lookup, RemoveEntity fan-out.
//   • Snapshot-friendly: EnsureCapacity(0) on creation for immediate validity.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.ComponentPooling.Internal
{
    /// <summary>
    /// Per-world repository mapping component <see cref="Type"/> to its pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each world owns a single <see cref="ComponentPoolRepository"/> instance that
    /// manages all component pools. Pools are created lazily on first access and
    /// cached for the lifetime of the world.
    /// </para>
    /// <para>
    /// The repository also provides fan-out operations such as
    /// <see cref="RemoveEntity(Entity)"/> which clears a given entity id from
    /// every known pool.
    /// </para>
    /// </remarks>
    internal sealed class ComponentPoolRepository : IComponentPoolRepository
    {
        /// <summary>
        /// Backing map that stores the pool for each component type.
        /// </summary>
        private Dictionary<Type, IComponentPool> _pools;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentPoolRepository"/> class.
        /// </summary>
        /// <param name="poolSize">
        /// Initial bucket size for the internal dictionary. This is only a hint
        /// and can be tuned to the expected number of component types in the world.
        /// </param>
        public ComponentPoolRepository(int poolSize = 256)
        {
            _pools = new Dictionary<Type, IComponentPool>(poolSize);
        }

        /// <summary>
        /// Cache of factories that create <see cref="IComponentPool"/> instances
        /// for a given component <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// The factories are built via closed generics of <see cref="ComponentPool{T}"/>
        /// and cached to avoid repeated reflection.
        /// </remarks>
        private static readonly ConcurrentDictionary<Type, Func<IComponentPool>> _poolFactories = new();

        /// <inheritdoc/>
        public IReadOnlyDictionary<Type, IComponentPool> ReadOnlyPools => _pools;

        /// <inheritdoc/>
        public IComponentPool GetPool<T>() where T : struct
        {
            var t = typeof(T);
            if (!_pools.TryGetValue(t, out var pool))
            {
                pool = new ComponentPool<T>();
                _pools.Add(t, pool);
            }
            return pool;
        }

        /// <summary>
        /// Attempts to get a strongly typed pool for component <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component struct type.</typeparam>
        /// <returns>
        /// The existing <see cref="ComponentPool{T}"/> instance if one has been
        /// created, or <see langword="null"/> otherwise.
        /// </returns>
        public ComponentPool<T>? TryGetPool<T>() where T : struct =>
            _pools.TryGetValue(typeof(T), out var p) ? (ComponentPool<T>)p : null;

        /// <summary>
        /// Gets a pool by component <see cref="Type"/> if it exists.
        /// </summary>
        /// <param name="t">Component type.</param>
        /// <returns>
        /// The existing <see cref="IComponentPool"/> instance, or
        /// <see langword="null"/> if the pool has not been created yet.
        /// </returns>
        public IComponentPool? GetPool(Type t) => _pools.GetValueOrDefault(t);

        /// <inheritdoc/>
        public IComponentPool GetOrCreatePoolByType(Type t)
        {
            if (!_pools.TryGetValue(t, out var pool))
            {
                var factory = GetOrBuildPoolFactory(t);
                pool = factory();

                // Ensure minimal capacity for immediate reads/writes.
                pool.EnsureCapacity(0);

                _pools.Add(t, pool);
            }
            return pool;
        }

        /// <inheritdoc/>
        public void RemoveEntity(Entity e)
        {
            foreach (var kv in _pools)
                kv.Value.Remove(e.Id);
        }

        /// <inheritdoc/>
        public Func<IComponentPool> GetOrBuildPoolFactory(Type compType)
        {
            if (_poolFactories.TryGetValue(compType, out var existing))
                return existing;

            var closed = typeof(ComponentPool<>).MakeGenericType(compType);
            Func<IComponentPool> factory = () => (IComponentPool)Activator.CreateInstance(closed)!;

            return _poolFactories.GetOrAdd(compType, factory);
        }

        /// <inheritdoc/>
        public void SetPool(Type componentType, IComponentPool pool)
        {
            _pools[componentType] = pool;
        }

        /// <inheritdoc/>
        public void ClearAllPools()
        {
            _pools.Clear();
        }
    }
}
