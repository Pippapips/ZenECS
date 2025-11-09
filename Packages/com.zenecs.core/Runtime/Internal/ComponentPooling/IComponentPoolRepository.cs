// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: IComponentPoolRepository.cs
// Purpose: Abstract repository mapping component types to pools with lazy creation.
// Key concepts:
//   • Typed and untyped accessors.
//   • Factory cache for closed generic ComponentPool<T> creation.
//   • Per-world lifetime; RemoveEntity() fan-out to all pools.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Repository interface mapping component <see cref="Type"/> to <see cref="IComponentPool"/>.
    /// </summary>
    internal interface IComponentPoolRepository
    {
        /// <summary>Access to the underlying pool map (Type → pool).</summary>
        Dictionary<Type, IComponentPool> Pools { get; }

        /// <summary>Get a typed pool, creating it if missing.</summary>
        IComponentPool GetPool<T>() where T : struct;

        /// <summary>Try get a typed pool (null if absent).</summary>
        ComponentPool<T>? TryGetPool<T>() where T : struct;

        /// <summary>Get or create a pool by component <see cref="Type"/>.</summary>
        IComponentPool GetOrCreatePoolByType(Type t);

        /// <summary>
        /// Get or build a cached factory that creates a pool for <paramref name="compType"/>.
        /// </summary>
        Func<IComponentPool> GetOrBuildPoolFactory(Type compType);

        /// <summary>Get a pool by type (null if absent).</summary>
        IComponentPool? GetPool(Type t);

        /// <summary>Remove an entity from all pools (used on despawn).</summary>
        void RemoveEntity(Entity e);
    }
}