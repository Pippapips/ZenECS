// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Component API
// File: IWorldComponentApi.cs
// Purpose: Typed component CRUD and ref accessors with validation/permission hooks.
// Key concepts:
//   • Ref access: allocation-free read/modify through pooled storage.
//   • Validation: object + typed validators run before writes.
//   • Presence checks and enumeration (boxed) for tooling/introspection.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>
    /// Marker interface for components that must exist zero-or-one times per world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Components that implement this interface are treated as world-level singletons:
    /// any given world may have at most one entity that carries a component of that
    /// type. The world implementation is responsible for enforcing this invariant.
    /// </para>
    /// <para>
    /// Typical usage:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Global configuration for a world (for example, gravity).</description></item>
    ///   <item><description>World-level runtime state (for example, match timer).</description></item>
    ///   <item><description>Shared references such as global spawners or managers.</description></item>
    /// </list>
    /// </remarks>
    public interface IWorldSingletonComponent { }

    /// <summary>
    /// Typed component operations and ref accessors for a world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface exposes read and presence checks, snapshot helpers, and
    /// singleton queries. Mutating operations (add/replace/remove) are typically
    /// part of the concrete world implementation and may be further gated by
    /// permission and validation hooks.
    /// </para>
    /// <para>
    /// The world guarantees that component storage is pooled, enabling
    /// allocation-free access through ref-returning APIs on the implementation side.
    /// </para>
    /// </remarks>
    public interface IWorldComponentApi
    {
        /// <summary>
        /// Checks whether an entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if the entity has a component of type
        /// <typeparamref name="T"/>; otherwise <see langword="false"/>.
        /// </returns>
        bool HasComponent<T>(Entity e) where T : struct;

        /// <summary>
        /// Checks whether an entity has a component for a runtime component type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="componentType">Component value type.</param>
        /// <returns>
        /// <see langword="true"/> if the entity has a component of the given type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool HasComponentBoxed(Entity e, Type? componentType);

        /// <summary>
        /// Dispatches a snapshot delta for an existing component without changing its value.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if the entity has the component and a snapshot
        /// was dispatched; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A snapshot delta is typically used when a binder attaches to an entity
        /// and needs the current component value to be pushed as-is into the
        /// presentation layer (the <c>Snapshot</c> kind in <c>ComponentDeltaKind</c>).
        /// </para>
        /// </remarks>
        bool SnapshotComponent<T>(Entity e) where T : struct;

        /// <summary>
        /// Dispatches a snapshot delta for a component whose type is inferred
        /// from a boxed value.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value; its runtime type is used to resolve the component
        /// type. Implementations may require this to be a non-null value type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a matching component exists and a snapshot
        /// was dispatched; otherwise <see langword="false"/>.
        /// </returns>
        bool SnapshotComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Dispatches a snapshot delta for a component identified by runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="t">Component value type.</param>
        /// <returns>
        /// <see langword="true"/> if a matching component exists and a snapshot
        /// was dispatched; otherwise <see langword="false"/>.
        /// </returns>
        bool SnapshotComponentTyped(Entity e, Type? t);

        /// <summary>
        /// Reads a component value by value (non-ref) from an entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// The current component value.
        /// </returns>
        /// <remarks>
        /// This is typically implemented as a thin wrapper around an internal
        /// ref-returning accessor such as <c>RefComponent&lt;T&gt;(Entity)</c>.
        /// </remarks>
        T ReadComponent<T>(Entity e) where T : struct;

        /// <summary>
        /// Attempts to read a component value from an entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">
        /// When this method returns, contains the component value if present;
        /// otherwise the default value of <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the entity has the component; otherwise
        /// <see langword="false"/>.
        /// </returns>
        bool TryReadComponent<T>(Entity e, out T value) where T : struct;

        /// <summary>
        /// Enumerates all components currently present on an entity as boxed values.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// A sequence of tuples where <c>type</c> is the component type and
        /// <c>boxed</c> is the boxed component value.
        /// </returns>
        /// <remarks>
        /// This is primarily intended for tooling, debugging, and generic inspectors
        /// where the component types are not known at compile time.
        /// </remarks>
        IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e);

        /// <summary>
        /// Gets the singleton component value for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <returns>The singleton component value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is no singleton of type <typeparamref name="T"/> or if
        /// the singleton invariant is violated (multiple entities own the component).
        /// </exception>
        T GetSingleton<T>() where T : struct, IWorldSingletonComponent;

        /// <summary>
        /// Attempts to get the singleton component value for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <param name="value">
        /// When this method returns, contains the singleton value if present;
        /// otherwise the default value for <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a singleton exists; otherwise
        /// <see langword="false"/>.
        /// </returns>
        bool TryGetSingleton<T>(out T value) where T : struct, IWorldSingletonComponent;

        /// <summary>
        /// Checks whether the specified entity owns any singleton component.
        /// </summary>
        /// <param name="e">Entity to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the entity is the owner of at least one
        /// <see cref="IWorldSingletonComponent"/>; otherwise <see langword="false"/>.
        /// </returns>
        bool HasSingleton(Entity e);

        /// <summary>
        /// Returns all singleton components currently registered in this world.
        /// </summary>
        /// <returns>
        /// A sequence of tuples containing the component type and the owning entity.
        /// </returns>
        IEnumerable<(Type type, Entity owner)> GetAllSingletons();
    }
}
