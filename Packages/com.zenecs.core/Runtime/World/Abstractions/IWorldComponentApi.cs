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
    /// <summary>
    /// Marker interface for components that must exist zero-or-one per world.
    /// </summary>
    public interface IWorldSingletonComponent {}
    
    /// <summary>Typed component operations and ref accessors.</summary>
    public interface IWorldComponentApi
    {
        // bool AddComponent<T>(Entity e, in T value) where T : struct;
        // bool AddComponentBoxed(Entity e, object? boxed);
        // bool ReplaceComponent<T>(Entity e, in T value) where T : struct;
        // bool RemoveComponent<T>(Entity e) where T : struct;
        // bool ReplaceComponentBoxed(Entity e, object? boxed);
        // bool RemoveComponentBoxed(Entity e, Type? componentType);
        
        bool HasComponent<T>(Entity e) where T : struct;
        bool HasComponentBoxed(Entity e, Type? componentType);
        bool SnapshotComponent<T>(Entity e) where T : struct;
        bool SnapshotComponentBoxed(Entity e, object? boxed);
        bool SnapshotComponentTyped(Entity e, Type? t);

        /// <summary>Read by <c>ref</c> (alias of <see><cref>RefComponent{T}(Entity)</cref></see>).</summary>
        T ReadComponent<T>(Entity e) where T : struct;
        
        /// <summary>Try read by value; returns <c>false</c> if the component is absent.</summary>
        bool TryReadComponent<T>(Entity e, out T value) where T : struct;

        /// <summary>Enumerate all present components (boxed) for the entity.</summary>
        IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e);

        // void SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent;
        // bool RemoveSingleton<T>(in T value) where T : struct, IWorldSingletonComponent;

        T GetSingleton<T>() where T : struct, IWorldSingletonComponent;
        bool TryGetSingleton<T>(out T value) where T : struct, IWorldSingletonComponent;
        bool HasSingleton(Entity e);
        IEnumerable<(Type type, Entity owner)> GetAllSingletons();
    }
}
