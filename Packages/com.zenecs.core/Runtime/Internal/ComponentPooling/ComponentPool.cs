// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: ComponentPool.cs
// Purpose: Generic value-type component pool (T[]) with presence bitset.
// Key concepts:
//   • O(1) id-indexed access with auto growth (power-of-two).
//   • Ref returns: zero-copy read/write; TryGet/Get for safe copies.
//   • Boxed accessors for tooling/snapshots; ClearAll for resets.
// License: MIT
// © 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Strongly-typed pool for value-type components.
    /// Backed by an array for O(1) access and a <c>BitSet</c> to track presence.
    /// </summary>
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        private const int DefaultInitialCapacity = 256;

        // Core data storage: maps entityId → component value
        private T[] _data;

        // Presence flags per entityId
        private BitSet _present;

        // Active component count
        private int _count;

        /// <summary>Parameterless ctor for reflection/AOT-safe instantiation.</summary>
        public ComponentPool() : this(DefaultInitialCapacity) { }

        /// <summary>Create a new pool with the given initial capacity.</summary>
        public ComponentPool(int initialCapacity)
        {
            int cap = Math.Max(1, initialCapacity);
            _data = new T[cap];
            _present = new BitSet(cap);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_data == null || _data.Length == 0)
                _data = new T[DefaultInitialCapacity];

            if (_present == null)
                _present = new BitSet(Math.Max(1, _data.Length));
            else if (_present.Length < _data.Length)
                _present.EnsureCapacity(_data.Length);
        }

        /// <inheritdoc/>
        public int Count => _count;

        /// <inheritdoc/>
        public void EnsureCapacity(int entityId)
        {
            EnsureInitialized();
            if (entityId < _data.Length) return;

            int cap = _data.Length == 0 ? 1 : _data.Length;
            while (cap <= entityId) cap <<= 1;

            Array.Resize(ref _data, cap);
            _present.EnsureCapacity(cap);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            if (entityId < 0) return false;
            if (_data == null || entityId >= _data.Length) return false;
            if (_present == null) return false;
            return _present.Get(entityId);
        }

        /// <summary>
        /// Get a reference to the component for writing; creates it if missing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ref(int entityId)
        {
            EnsureCapacity(entityId);
            if (!_present.Get(entityId))
            {
                _present.Set(entityId, true);
                _count++;
            }
            return ref _data[entityId];
        }

        /// <summary>
        /// Get a reference to an existing component; throws if missing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T RefExisting(int entityId)
        {
            if (!Has(entityId))
                throw new InvalidOperationException($"Component '{typeof(T).Name}' not present on entity {entityId}.");
            return ref _data[entityId];
        }

        /// <summary>
        /// Get a copy of the component value (default if not found).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int entityId)
            => Has(entityId) ? _data[entityId] : default;

        /// <summary>
        /// Try get a value copy of the component.
        /// </summary>
        public bool TryGet(int entityId, out T value)
        {
            if (Has(entityId))
            {
                value = _data[entityId];
                return true;
            }
            value = default;
            return false;
        }

        /// <inheritdoc/>
        public void Remove(int entityId, bool dataClear = true)
        {
            if (!Has(entityId)) return;
            _present.Set(entityId, false);
            _count--;
            if (dataClear)
                _data[entityId] = default;
        }

        /// <inheritdoc/>
        public int Capacity => _data?.Length ?? 0;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntityIdAt(int index) => index;

        /// <summary>
        /// Allocation-free enumerator over active entity ids.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolEnumerator EnumerateIds()
            => new PoolEnumerator(this);

        /// <summary>
        /// Enumerate (entityId, boxed component) pairs for external tools/snapshots.
        /// </summary>
        public IEnumerable<(int id, object boxed)> EnumerateAll()
        {
            var it = new PoolEnumerator(this);
            while (it.MoveNext())
            {
                int id = it.CurrentId;
                var boxed = GetBoxed(id);
                if (boxed != null)
                    yield return (id, boxed);
            }
        }

        /// <inheritdoc/>
        public object? GetBoxed(int entityId)
            => Has(entityId) ? (object)_data[entityId] : null;

        /// <inheritdoc/>
        public void SetBoxed(int entityId, object value)
        {
            EnsureInitialized();
            if (value is not T v)
                throw new InvalidCastException(
                    $"SetBoxed type mismatch: value is '{value?.GetType().FullName ?? "null"}' " +
                    $"but pool expects '{typeof(T).FullName}'");

            ref var r = ref Ref(entityId);
            r = v;
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            EnsureInitialized();
            _present.ClearAll();
            _count = 0;
        }
    }
}
