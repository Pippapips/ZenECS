// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: Internal/ComponentPool.cs
// Purpose: Generic component pool (T[]) + presence bitset implementing IComponentPool.
// Key concepts:
//   • O(1) id-indexed access; auto-growth policy.
//   • Boxed accessors for external tooling and snapshots.
//   • Designed for maximum performance in runtime systems and safe use in AOT/IL2CPP.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// A strongly-typed pool for value-type components.
    /// Backed by an array for O(1) access and a BitSet for tracking which entity IDs are occupied.
    /// Designed for maximum performance with minimal memory overhead.
    /// </summary> 
    internal sealed class ComponentPoolRepository : IComponentPoolRepository
    {
        /// <summary>
        /// Mapping of component <see cref="Type"/> to its corresponding <see cref="IComponentPool"/>.
        /// </summary>
        private Dictionary<Type, IComponentPool> _pools;

        /// <summary>
        /// Cache of factory delegates that create new component pools by type.
        /// Used to avoid repeated reflection calls.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<IComponentPool>> _poolFactories = new();

        public void Initialize(int poolSize)
        {
            _pools = new Dictionary<Type, IComponentPool>(poolSize);
        }
        
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

        public ComponentPool<T>? TryGetPool<T>() where T : struct =>
            _pools.TryGetValue(typeof(T), out var p) ? (ComponentPool<T>)p : null;

        public IComponentPool? GetPool(Type t)
        {
            return _pools.GetValueOrDefault(t);
        } 

        public IComponentPool GetOrCreatePoolByType(Type t)
        {
            if (!_pools.TryGetValue(t, out var pool))
            {
                // ✅ Safe factory creation through GetOrBuildPoolFactory
                var factory = GetOrBuildPoolFactory(t);
                pool = factory();

                // Ensure minimal capacity so the pool is valid for immediate operations.
                pool.EnsureCapacity(0);

                _pools.Add(t, pool);
            }

            return pool;
        }
        public void RemoveEntity(Entity e)
        {
            foreach (var kv in _pools)
                kv.Value.Remove(e.Id);
        }

        /// <summary>
        /// Retrieves an existing factory for a given component type, or builds a new one if missing.
        /// </summary>
        /// <param name="compType">The component type to create a pool for.</param>
        /// <returns>A delegate that constructs an <see cref="IComponentPool"/> for the given type.</returns>
        /// <remarks>
        /// - Uses <c>ComponentPool&lt;T&gt;</c> with a parameterless constructor for AOT/IL2CPP safety.<br/>
        /// - The factory is cached in a concurrent dictionary and reused across all worlds.<br/>
        /// - When multiple threads race to add a factory, the first inserted instance wins.
        /// </remarks>
        private Func<IComponentPool> GetOrBuildPoolFactory(Type compType)
        {
            if (_poolFactories.TryGetValue(compType, out var existing))
                return existing;

            // Build a closed generic type for ComponentPool<T>.
            var closed = typeof(ComponentPool<>).MakeGenericType(compType);

            // ComponentPool<T> exposes a parameterless constructor,
            // which is safe for use under AOT/IL2CPP environments.
            Func<IComponentPool> factory = () => (IComponentPool)Activator.CreateInstance(closed)!;

            // If multiple threads attempt insertion, the first registered factory is used.
            return _poolFactories.GetOrAdd(compType, factory);
        }
    }
}
