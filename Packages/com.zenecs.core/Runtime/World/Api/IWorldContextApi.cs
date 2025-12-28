// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Context API
// File: IWorldContextApi.cs
// Purpose: Register per-entity view contexts consumed by binders/renderers.
// Key concepts:
//   • Registry per world: attach arbitrary resources to entities.
//   • Binder integration: binders resolve contexts to render/apply updates.
//   • Ownership: registry defines replacement/merge policy by type/key.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    /// <summary>
    /// World-side API for registering and managing per-entity view contexts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contexts are arbitrary objects (for example, Unity components, pooled
    /// view models, or adapter-specific handles) that are attached to entities
    /// and later consumed by binders and renderers when applying presentation
    /// deltas from the ECS world.
    /// </para>
    /// <para>
    /// The world is the single source of truth for which contexts are associated
    /// with a given entity; binders should not cache long-lived references
    /// without listening for lifecycle changes.
    /// </para>
    /// </remarks>
    public interface IWorldContextApi
    {
        /// <summary>
        /// Registers a context object for an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance to associate with the entity.</param>
        /// <remarks>
        /// <para>
        /// The exact replacement policy (for example, whether multiple contexts
        /// of the same runtime type are allowed, or whether a new registration
        /// overwrites an existing one) is defined by the world implementation.
        /// </para>
        /// <para>
        /// Implementations are expected to detach or recycle contexts when
        /// entities are despawned or the world is reset.
        /// </para>
        /// </remarks>
        void RegisterContext(Entity e, IContext ctx);

        /// <summary>
        /// Checks whether an entity has at least one context of a given runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="contextType">Context runtime type to query for.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <paramref name="contextType"/>
        /// is registered for <paramref name="e"/>; otherwise <see langword="false"/>.
        /// </returns>
        bool HasContext(Entity e, Type? contextType);

        /// <summary>
        /// Checks whether an entity has at least one context of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Context type to query for.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/>
        /// is registered for <paramref name="e"/>; otherwise <see langword="false"/>.
        /// </returns>
        bool HasContext<T>(Entity e) where T : class, IContext;

        /// <summary>
        /// Gets all contexts currently registered for an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// An array of tuples where <c>type</c> is the runtime type of the
        /// context and <c>boxed</c> is the context instance boxed as
        /// <see cref="object"/>. Returns an empty array if no contexts
        /// are registered.
        /// </returns>
        (Type type, object boxed)[] GetAllContexts(Entity e);

        /// <summary>
        /// Removes a specific context instance from an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the specified context instance was
        /// registered and removed; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The call is a no-op if the context is not currently registered for
        /// <paramref name="e"/>.
        /// </remarks>
        bool RemoveContext(Entity e, IContext ctx);

        /// <summary>
        /// Requests that a context be reinitialized for an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">
        /// Context instance to reinitialize (typically one that is already
        /// registered).
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the context is known and was successfully
        /// reinitialized; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The exact semantics of "reinitialize" are implementation-specific,
        /// but a common pattern is to re-run context setup logic after data
        /// changes, or after a binder has reattached to an existing view object.
        /// </para>
        /// </remarks>
        bool ReinitializeContext(Entity e, IContext ctx);
    }
}
