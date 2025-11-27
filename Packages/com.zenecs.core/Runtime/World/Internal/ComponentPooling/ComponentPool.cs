// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: ComponentPool.cs
// Purpose: Generic value-type component pool (T[]) with presence bitset.
// Key concepts:
//   • O(1) id-indexed access with auto growth (power-of-two).
//   • Ref returns: zero-copy read/write; TryGet/Get for safe copies.
//   • Boxed accessors for tooling/snapshots; ClearAll for resets.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Infrastructure.Internal;

namespace ZenECS.Core.ComponentPooling.Internal
{
    /// <summary>
    /// Strongly typed pool for value-type components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Backed by a dense array for O(1) entityId-indexed access and a
    /// bitset to track presence. The pool grows automatically in powers
    /// of two as new entity ids are accessed.
    /// </para>
    /// <para>
    /// Provides ref-return accessors for zero-copy modifications as well as
    /// boxed accessors for tooling, snapshotting, and reflection-heavy use.
    /// </para>
    /// </remarks>
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        private const int DefaultInitialCapacity = 256;

        // Core data storage: maps entityId → component value.
        private T[] _data;

        // Presence flags per entityId.
        private BitSet _present;

        // Active component count.
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentPool{T}"/> class
        /// with a default initial capacity.
        /// </summary>
        /// <remarks>
        /// This parameterless constructor exists for reflection/AOT-friendly
        /// instantiation via <see cref="Activator.CreateInstance(Type)"/>.
        /// </remarks>
        public ComponentPool() : this(DefaultInitialCapacity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentPool{T}"/> class
        /// with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">
        /// Initial number of addressable entity ids. The internal capacity is
        /// clamped to at least 1.
        /// </param>
        public ComponentPool(int initialCapacity)
        {
            int cap = Math.Max(1, initialCapacity);
            _data = new T[cap];
            _present = new BitSet(cap);
            _count = 0;
        }

        /// <summary>
        /// Ensures internal fields are initialized with sane defaults.
        /// </summary>
        /// <remarks>
        /// This is defensive against potential default-construction scenarios
        /// (e.g., if the pool struct were ever stack-allocated) and keeps the
        /// rest of the code simple.
        /// </remarks>
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
        /// Returns a reference to the component for the specified entity,
        /// creating it if necessary.
        /// </summary>
        /// <param name="entityId">Entity id for which a component is requested.</param>
        /// <returns>
        /// A writable reference to the component value for <paramref name="entityId"/>.
        /// </returns>
        /// <remarks>
        /// If the component does not yet exist, it is added and the internal
        /// count is incremented.
        /// </remarks>
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
        /// Returns a reference to an existing component for the specified entity.
        /// </summary>
        /// <param name="entityId">Entity id for which a component is requested.</param>
        /// <returns>
        /// A writable reference to the existing component value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the component is not present on the entity.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T RefExisting(int entityId)
        {
            if (!Has(entityId))
                throw new InvalidOperationException($"Component '{typeof(T).Name}' not present on entity {entityId}.");
            return ref _data[entityId];
        }

        /// <summary>
        /// Gets a copy of the component value for the specified entity.
        /// </summary>
        /// <param name="entityId">Entity id to read from.</param>
        /// <returns>
        /// The component value if present; otherwise the default value of <typeparamref name="T"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int entityId)
            => Has(entityId) ? _data[entityId] : default;

        /// <summary>
        /// Tries to get a copy of the component value.
        /// </summary>
        /// <param name="entityId">Entity id to read from.</param>
        /// <param name="value">
        /// When this method returns <see langword="true"/>, contains the component
        /// value; otherwise contains the default value for <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the component is present; otherwise <see langword="false"/>.
        /// </returns>
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

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolEnumerator EnumerateIds()
            => new PoolEnumerator(this);

        /// <inheritdoc/>
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
