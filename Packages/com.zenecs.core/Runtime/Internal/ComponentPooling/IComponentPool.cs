// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: IComponentPool.cs
// Purpose: Minimal pool surface required by snapshots, queries, and tools.
// Key concepts:
//   • EnsureCapacity/Has/Remove for structural ops.
//   • Boxed Get/Set for reflection and persistence layers.
//   • Allocation-free enumeration via PoolEnumerator.
// License: MIT
// © 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Allocation-free enumerator over active entity ids for a pool.
    /// </summary>
    internal struct PoolEnumerator
    {
        private readonly IComponentPool? _pool;
        private readonly int _end;
        private int _idx;
        private int _currentId;

        /// <summary>An empty enumerator.</summary>
        public static PoolEnumerator Empty => default;

        /// <summary>Create a new enumerator over <paramref name="pool"/>.</summary>
        internal PoolEnumerator(IComponentPool pool)
        {
            _pool = pool;
            _end = pool.Capacity;
            _idx = -1;
            _currentId = -1;
        }

        /// <summary>The current entity id.</summary>
        public int CurrentId => _currentId;

        /// <summary>Advance to the next present entity id.</summary>
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
    /// Common pool surface used by the world, snapshots, and editors/tools.
    /// </summary>
    internal interface IComponentPool
    {
        /// <summary>Ensure the pool can address <paramref name="entityId"/>.</summary>
        void EnsureCapacity(int entityId);

        /// <summary>Return whether the entity currently has this component type.</summary>
        bool Has(int entityId);

        /// <summary>Remove the component from the entity (optionally clear slot).</summary>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>Get the component as a boxed value (null if not present).</summary>
        object? GetBoxed(int entityId);

        /// <summary>Set the component from a boxed value (add or overwrite).</summary>
        void SetBoxed(int entityId, object value);

        /// <summary>Underlying array length (scan upper bound).</summary>
        int Capacity { get; }

        /// <summary>Allocation-free enumeration of active ids.</summary>
        PoolEnumerator EnumerateIds();

        /// <summary>Enumerate (entityId, boxed) pairs for tooling/snapshots.</summary>
        IEnumerable<(int id, object boxed)> EnumerateAll();

        /// <summary>In current sparse layout, returns <paramref name="index"/> unchanged.</summary>
        int EntityIdAt(int index);

        /// <summary>Number of active components stored in the pool.</summary>
        int Count { get; }

        /// <summary>Clear presence flags and (optionally) data slots.</summary>
        void ClearAll();
    }
}
