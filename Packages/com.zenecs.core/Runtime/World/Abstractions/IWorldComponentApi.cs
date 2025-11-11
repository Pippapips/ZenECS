// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Component API
// File: IWorldComponentApi.cs
// Purpose: Typed component CRUD and ref accessors with validation/permission hooks.
// Key concepts:
//   • Ref access: allocation-free read/modify through pooled storage.
//   • Validation: object + typed validators run before writes.
//   • Presence checks and enumeration (boxed) for tooling/introspection.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>Typed component operations and ref accessors.</summary>
    public interface IWorldComponentApi
    {
        // ── Typed (existing) ────────────────────────────────────────────────
        bool HasComponent<T>(Entity e) where T : struct;
        bool AddComponent<T>(Entity e, in T value) where T : struct;
        bool ReplaceComponent<T>(Entity e, in T value) where T : struct;
        bool RemoveComponent<T>(Entity e) where T : struct;

        // ── Boxed / non-generic (NEW) ───────────────────────────────────────
        /// <summary>
        /// Checks component presence using a runtime <paramref name="componentType"/>.
        /// </summary>
        bool HasComponentBoxed(Entity e, Type? componentType);

        /// <summary>
        /// Adds a component using a boxed struct value. Delegates to
        /// <see cref="AddComponent{T}(Entity, in T)"/>.
        /// </summary>
        bool AddComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Replaces a component using a boxed struct value. Delegates to
        /// <see cref="ReplaceComponent{T}(Entity, in T)"/>.
        /// </summary>
        bool ReplaceComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Removes a component using a runtime <paramref name="componentType"/>.
        /// Delegates to <see cref="RemoveComponent{T}(Entity)"/>.
        /// </summary>
        bool RemoveComponentBoxed(Entity e, Type? componentType);

        /// <summary>Read by <c>ref</c> (alias of <see><cref>RefComponent{T}(Entity)</cref></see>).</summary>
        ref T ReadComponent<T>(Entity e) where T : struct;
        
        /// <summary>Try read by value; returns <c>false</c> if the component is absent.</summary>
        bool TryRead<T>(Entity e, out T value) where T : struct;

        /// <summary>Enumerate all present components (boxed) for the entity.</summary>
        IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e);
    }
}
