// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: ComponentPoolRepository.cs
// Purpose: Map component Type → IComponentPool and create pools on demand.
// Key concepts:
//   • Lazy factories: closed generic ComponentPool<T> via cached delegates.
//   • Per-world repository: fast lookup, RemoveEntity fan-out.
//   • Snapshot-friendly: EnsureCapacity(0) on creation for immediate validity.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Per-world repository mapping component <see cref="Type"/> to its pool.
    /// </summary>
    internal sealed class ComponentPoolRepository : IComponentPoolRepository
    {
        /// <summary>Backing map: component Type → pool.</summary>
        private Dictionary<Type, IComponentPool> _pools;

        /// <summary>Create a repository with an initial bucket size.</summary>
        public ComponentPoolRepository(int poolSize = 256)
        {
            _pools = new Dictionary<Type, IComponentPool>(poolSize);
        }

        /// <summary>Cache of factories that create <see cref="IComponentPool"/> for a given Type.</summary>
        private static readonly ConcurrentDictionary<Type, Func<IComponentPool>> _poolFactories = new();

        /// <inheritdoc/>
        public Dictionary<Type, IComponentPool> Pools => _pools;

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

        /// <summary>Try get a typed pool (null if missing).</summary>
        public ComponentPool<T>? TryGetPool<T>() where T : struct =>
            _pools.TryGetValue(typeof(T), out var p) ? (ComponentPool<T>)p : null;

        /// <summary>Get a pool by component type (null if absent).</summary>
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
    }
}
