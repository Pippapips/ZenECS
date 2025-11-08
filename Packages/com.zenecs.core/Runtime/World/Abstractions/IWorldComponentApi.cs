// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Component API
// File: IWorldComponentApi.cs
// Purpose: Typed component CRUD and ref accessors with validation/permission hooks.
// Key concepts:
//   • Ref access: allocation-free read/modify through pooled storage.
//   • Validation: object + typed validators run before writes.
//   • Presence checks and enumeration (boxed) for tooling/introspection.
// License: MIT — Copyright (c) 2025
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
        /// <summary>Add a component if absent; returns <c>false</c> on denial or already present.</summary>
        bool AddComponent<T>(Entity e, in T value) where T : struct;

        /// <summary>Check if an entity currently has the component.</summary>
        bool HasComponent<T>(Entity e) where T : struct;

        /// <summary>Get a <c>ref</c> to a component (creates storage if needed).</summary>
        ref T RefComponent<T>(Entity e) where T : struct;

        /// <summary>Get a <c>ref</c> to an existing component; throws if missing.</summary>
        ref T RefComponentExisting<T>(Entity e) where T : struct;

        /// <summary>Read by <c>ref</c> (alias of <see cref="RefComponent{T}(Entity)"/>).</summary>
        ref T ReadComponent<T>(Entity e) where T : struct;

        /// <summary>Replace a component value in place; returns <c>false</c> on denial.</summary>
        bool ReplaceComponent<T>(Entity e, in T value) where T : struct;

        /// <summary>Remove a component; returns <c>false</c> if absent or denied.</summary>
        bool RemoveComponent<T>(Entity e) where T : struct;

        /// <summary>Try read by value; returns <c>false</c> if the component is absent.</summary>
        bool TryRead<T>(Entity e, out T value) where T : struct;

        /// <summary>Enumerate all present components (boxed) for the entity.</summary>
        IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e);
    }
}
