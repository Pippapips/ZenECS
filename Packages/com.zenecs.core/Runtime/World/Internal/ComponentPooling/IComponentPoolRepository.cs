// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: IComponentPoolRepository.cs
// Purpose: Abstract repository mapping component types to pools with lazy creation.
// Key concepts:
//   • Typed and untyped accessors.
//   • Factory cache for closed generic ComponentPool<T> creation.
//   • Per-world lifetime; RemoveEntity() fan-out to all pools.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.ComponentPooling.Internal
{
    /// <summary>
    /// Repository interface mapping component <see cref="Type"/> to <see cref="IComponentPool"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This repository owns all component pools for a single world and provides
    /// both typed and untyped accessors. Pools are created lazily on first use
    /// and cached for the lifetime of the world.
    /// </para>
    /// <para>
    /// When an entity is despawned, <see cref="RemoveEntity"/> is used to fan
    /// the removal out to all pools so they can drop the corresponding slots.
    /// </para>
    /// </remarks>
    internal interface IComponentPoolRepository
    {
        /// <summary>
        /// Gets a read-only view of the underlying pool map keyed by component <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// This collection is intended for internal and tooling use only.
        /// The dictionary is read-only and cannot be modified through this interface.
        /// </remarks>
        IReadOnlyDictionary<Type, IComponentPool> ReadOnlyPools { get; }

        /// <summary>
        /// Gets a typed pool for component <typeparamref name="T"/>, creating it if necessary.
        /// </summary>
        /// <typeparam name="T">Component struct type.</typeparam>
        /// <returns>
        /// An <see cref="IComponentPool"/> instance that stores components of type
        /// <typeparamref name="T"/>.
        /// </returns>
        IComponentPool GetPool<T>() where T : struct;

        /// <summary>
        /// Attempts to get a strongly-typed pool for component <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component struct type.</typeparam>
        /// <returns>
        /// The existing <see cref="ComponentPool{T}"/> instance if present;
        /// otherwise <see langword="null"/>.
        /// </returns>
        ComponentPool<T>? TryGetPool<T>() where T : struct;

        /// <summary>
        /// Gets or creates a pool for the specified component <see cref="Type"/>.
        /// </summary>
        /// <param name="t">Component type to resolve a pool for.</param>
        /// <returns>
        /// An <see cref="IComponentPool"/> that stores components of runtime type <paramref name="t"/>.
        /// </returns>
        IComponentPool GetOrCreatePoolByType(Type t);

        /// <summary>
        /// Gets or builds a cached factory that creates a pool for the given component type.
        /// </summary>
        /// <param name="compType">Component type for which a pool factory is requested.</param>
        /// <returns>
        /// A factory delegate that, when invoked, constructs a new <see cref="IComponentPool"/>
        /// for components of type <paramref name="compType"/>.
        /// </returns>
        /// <remarks>
        /// Implementations typically cache the closed generic constructor for
        /// <c>ComponentPool&lt;T&gt;</c> to avoid repeated reflection.
        /// </remarks>
        Func<IComponentPool> GetOrBuildPoolFactory(Type compType);

        /// <summary>
        /// Gets a pool by type without creating one if it does not exist.
        /// </summary>
        /// <param name="t">Component type.</param>
        /// <returns>
        /// The existing <see cref="IComponentPool"/> instance for <paramref name="t"/>,
        /// or <see langword="null"/> if no pool has been created yet.
        /// </returns>
        IComponentPool? GetPool(Type t);

        /// <summary>
        /// Removes the given entity from all known pools.
        /// </summary>
        /// <param name="e">Entity to remove from all component pools.</param>
        /// <remarks>
            /// This is typically called during despawn; implementations should
        /// iterate all pools and clear the component for <paramref name="e"/>,
        /// if present.
        /// </remarks>
        void RemoveEntity(Entity e);

        /// <summary>
        /// Sets or replaces a pool for the specified component type.
        /// </summary>
        /// <param name="componentType">Component type to set a pool for.</param>
        /// <param name="pool">Pool instance to associate with the component type.</param>
        /// <remarks>
        /// This method is intended for internal use only, such as during world reset operations.
        /// </remarks>
        void SetPool(Type componentType, IComponentPool pool);

        /// <summary>
        /// Clears all pools from the repository.
        /// </summary>
        /// <remarks>
        /// This method is intended for internal use only, such as during world hard reset operations.
        /// </remarks>
        void ClearAllPools();
    }
}
