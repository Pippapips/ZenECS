// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: IComponentPool.cs
// Purpose: Minimal pool surface required by snapshots, queries, and tools.
// Key concepts:
//   • EnsureCapacity/Has/Remove for structural ops.
//   • Boxed Get/Set for reflection and persistence layers.
//   • Allocation-free enumeration via PoolEnumerator.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;

namespace ZenECS.Core.ComponentPooling.Internal
{
    /// <summary>
    /// Allocation-free enumerator over active entity ids within a component pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct is designed for <c>foreach</c>-style iteration without
    /// allocations. It iterates over all indices in the underlying pool and
    /// yields only those that currently have a component present.
    /// </para>
    /// <para>
    /// The enumerated values are entity ids, not indices; in the current sparse
    /// layout these happen to be the same, but callers should always treat
    /// <see cref="CurrentId"/> as an entity id.
    /// </para>
    /// </remarks>
    internal struct PoolEnumerator
    {
        private readonly IComponentPool? _pool;
        private readonly int _end;
        private int _idx;
        private int _currentId;

        /// <summary>
        /// Gets an empty enumerator that will never yield any ids.
        /// </summary>
        public static PoolEnumerator Empty => default;

        /// <summary>
        /// Initializes a new enumerator over the specified <paramref name="pool"/>.
        /// </summary>
        /// <param name="pool">Component pool to iterate over.</param>
        internal PoolEnumerator(IComponentPool pool)
        {
            _pool = pool;
            _end = pool.Capacity;
            _idx = -1;
            _currentId = -1;
        }

        /// <summary>
        /// Gets the current entity id for this enumerator.
        /// </summary>
        public int CurrentId => _currentId;

        /// <summary>
        /// Advances the enumerator to the next present entity id.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator successfully advanced to the
        /// next active entity id; otherwise <see langword="false"/>.
        /// </returns>
        public bool MoveNext()
        {
            var p = _pool;
            if (p == null) return false;

            while (++_idx < _end)
            {
                int id = p.EntityIdAt(_idx); // in current impl: id == index
                if (p.Has(id))
                {
                    _currentId = id;
                    return true;
                }
            }

            _currentId = -1;
            return false;
        }
    }

    /// <summary>
    /// Common component-pool surface used by the world, snapshot system,
    /// and editors/tools.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pool manages components of a single struct type, indexed by entity id.
    /// It supports cheap presence tests, boxing-based get/set for reflection,
    /// and allocation-free iteration over active entities.
    /// </para>
    /// <para>
    /// Implementations are expected to grow on demand and to keep presence
    /// information consistent with the underlying world.
    /// </para>
    /// </remarks>
    internal interface IComponentPool
    {
        /// <summary>
        /// Ensures that the pool can address the given <paramref name="entityId"/>.
        /// </summary>
        /// <param name="entityId">
        /// Entity id that must be valid as an index in the pool's internal storage.
        /// </param>
        void EnsureCapacity(int entityId);

        /// <summary>
        /// Determines whether the specified entity currently has this component type.
        /// </summary>
        /// <param name="entityId">Entity id to test.</param>
        /// <returns>
        /// <see langword="true"/> if the component is present for the entity;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool Has(int entityId);

        /// <summary>
        /// Removes the component from the given entity.
        /// </summary>
        /// <param name="entityId">Entity id to remove the component from.</param>
        /// <param name="dataClear">
        /// When <see langword="true"/>, the underlying data slot is cleared
        /// in addition to the presence flag.
        /// </param>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>
        /// Gets the component for the specified entity as a boxed value.
        /// </summary>
        /// <param name="entityId">Entity id to read from.</param>
        /// <returns>
        /// Boxed component value if present; otherwise <see langword="null"/>.
        /// </returns>
        object? GetBoxed(int entityId);

        /// <summary>
        /// Sets the component value for the specified entity from a boxed value.
        /// </summary>
        /// <param name="entityId">Entity id to write to.</param>
        /// <param name="value">
        /// Boxed component value. Implementations are responsible for validating
        /// the runtime type and unboxing to the concrete component type.
        /// </param>
        /// <remarks>
        /// This will add the component if it is not already present, or overwrite
        /// the existing value otherwise.
        /// </remarks>
        void SetBoxed(int entityId, object value);

        /// <summary>
        /// Gets the current capacity of the underlying storage in entity slots.
        /// </summary>
        /// <remarks>
        /// The number of addressable entity ids is equal to <c>Capacity</c>.
        /// Presence is tracked separately via internal flags.
        /// </remarks>
        int Capacity { get; }

        /// <summary>
        /// Returns an allocation-free enumerator over active entity ids.
        /// </summary>
        /// <returns>
        /// A <see cref="PoolEnumerator"/> that yields ids for which
        /// <see cref="Has"/> is <see langword="true"/>.
        /// </returns>
        PoolEnumerator EnumerateIds();

        /// <summary>
        /// Enumerates all active components as <c>(entityId, boxed)</c> pairs.
        /// </summary>
        /// <returns>
        /// An enumerable sequence of pairs where <c>id</c> is the entity id and
        /// <c>boxed</c> is the boxed component value.
        /// </returns>
        /// <remarks>
        /// Intended primarily for tooling and snapshotting; callers should not
        /// assume any particular ordering of ids.
        /// </remarks>
        IEnumerable<(int id, object boxed)> EnumerateAll();

        /// <summary>
        /// Maps an internal index to an entity id.
        /// </summary>
        /// <param name="index">Internal index into the underlying arrays.</param>
        /// <returns>
        /// Entity id corresponding to <paramref name="index"/>. In the current
        /// sparse layout, this returns <paramref name="index"/> unchanged.
        /// </returns>
        int EntityIdAt(int index);

        /// <summary>
        /// Gets the number of active component instances stored in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Clears all presence flags and optionally component data.
        /// </summary>
        /// <remarks>
        /// After calling this, <see cref="Count"/> should be zero and
        /// <see cref="Has"/> should return <see langword="false"/> for all ids.
        /// </remarks>
        void ClearAll();
    }
}
