// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IContext.cs
// Purpose: Context contracts — per-entity resource containers consumed by binders.
// Key concepts:
//   • Lookup first: binders resolve contexts via IContextLookup during Bind/Apply.
//   • Optional lifecycle: Initialize/Deinitialize/Reinitialize hooks on registry events.
//   • Sharing: contexts may be shared references; ownership is defined by the registry policy.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Marker interface for context objects.
    /// A context is a resource container (data or references) that binders use
    /// to drive external presentation systems (rendering, UI, audio, etc.).
    /// </summary>
    public interface IContext { }

    /// <summary>
    /// Read-only lookup surface used by binders and systems to discover
    /// contexts attached to a given entity in a world.
    /// </summary>
    public interface IContextLookup
    {
        /// <summary>
        /// Attempts to get a context of type <typeparamref name="T"/> for the
        /// specified entity in the given world.
        /// </summary>
        /// <typeparam name="T">Concrete context type to retrieve.</typeparam>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for the context.</param>
        /// <param name="ctx">
        /// When this method returns <see langword="true"/>, contains the
        /// context instance; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the context exists and was returned;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool TryGet<T>(IWorld w, Entity e, out T ctx) where T : class, IContext;

        /// <summary>
        /// Gets a context of type <typeparamref name="T"/> for the specified
        /// entity in the given world, or throws if it does not exist.
        /// </summary>
        /// <typeparam name="T">Concrete context type to retrieve.</typeparam>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for the context.</param>
        /// <returns>The context instance.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no context of type <typeparamref name="T"/> is registered
        /// for the entity.
        /// </exception>
        T Get<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>
        /// Determines whether a context of type <typeparamref name="T"/> exists
        /// for the specified entity in the given world.
        /// </summary>
        /// <typeparam name="T">Concrete context type to check.</typeparam>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for the context.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/>
        /// is registered for the entity; otherwise <see langword="false"/>.
        /// </returns>
        bool Has<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>
        /// Determines whether the exact context instance <paramref name="ctx"/>
        /// is registered for the specified entity in the given world.
        /// Uses reference equality.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for the context.</param>
        /// <param name="ctx">
        /// The exact context instance to test. May be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the given instance is registered for the
        /// entity; otherwise <see langword="false"/>.
        /// </returns>
        bool Has(IWorld w, Entity e, IContext? ctx);

        /// <summary>
        /// Determines whether an <see cref="IContext"/> instance assignable to
        /// <paramref name="contextType"/> is registered for the specified entity.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for the context.</param>
        /// <param name="contextType">
        /// Target type to check for. If <see langword="null"/>, this always
        /// returns <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a context instance compatible with
        /// <paramref name="contextType"/> is registered; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This uses type compatibility
        /// (e.g. <c>contextType.IsInstanceOfType(instance)</c>), so derived
        /// or implementing context types will also match.
        /// </remarks>
        bool Has(IWorld w, Entity e, Type? contextType);

        /// <summary>
        /// Gets a read-only list of all contexts currently registered for
        /// the specified entity in the given world.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for contexts.</param>
        /// <returns>
        /// A read-only list of <see cref="IContext"/> instances, or
        /// <see langword="null"/> if no contexts are registered.
        /// </returns>
        IReadOnlyList<IContext>? GetAllContextList(IWorld w, Entity e);

        /// <summary>
        /// Returns an array of all contexts currently registered for the
        /// specified entity in the given world, with each item containing
        /// the runtime type and boxed instance.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity to query for contexts.</param>
        /// <returns>
        /// An array of pairs where <c>type</c> is the context's runtime
        /// <see cref="Type"/> and <c>boxed</c> is the context instance.
        /// </returns>
        (Type type, object boxed)[] GetAllContexts(IWorld w, Entity e);
    }

    /// <summary>
    /// Optional lifecycle interface called by the context registry when
    /// contexts are added to or removed from an entity.
    /// </summary>
    public interface IContextInitialize
    {
        /// <summary>
        /// Called once when the context is first registered for the entity.
        /// Implementations may resolve additional contexts via
        /// <paramref name="l"/> and cache any required state.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <param name="l">Lookup service for resolving other contexts.</param>
        void Initialize(IWorld w, Entity e, IContextLookup l);

        /// <summary>
        /// Called when the context is removed, or when the entity or world
        /// is being destroyed. Implementations should release resources and
        /// unsubscribe from any external systems here.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity the context was attached to.</param>
        void Deinitialize(IWorld w, Entity e);
    }

    /// <summary>
    /// Optional fast re-initialization path that can be used when a context
    /// can rebind without a full deinitialize/initialize cycle.
    /// </summary>
    /// <remarks>
    /// If this interface is not implemented, the registry falls back to
    /// calling <see cref="IContextInitialize.Deinitialize"/> followed by
    /// <see cref="IContextInitialize.Initialize"/>.
    /// </remarks>
    public interface IContextReinitialize : IContextInitialize
    {
        /// <summary>
        /// Reinitializes the context when a quick rebind is possible, allowing
        /// reuse of existing state instead of a full teardown and re-setup.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <param name="l">Lookup service for resolving other contexts.</param>
        void Reinitialize(IWorld w, Entity e, IContextLookup l);
    }
}
