// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Context API)
// File: WorldContextApi.cs
// Purpose: Register per-entity view contexts used by binders/renderers.
// Key concepts:
//   • Context registry per world: attach arbitrary resources to entities.
//   • Binder access: binders resolve contexts to render/apply updates.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldContextApi"/> – context registration surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial world implementation forwards all context operations to the
    /// internal <c>_contextRegistry</c>, which is responsible for the actual
    /// storage, replacement policy, and lifecycle handling of per-entity
    /// contexts.
    /// </para>
    /// <para>
    /// Contexts are arbitrary adapter-side objects (for example, Unity components,
    /// view models, pooled handles) that are later consumed by binders when
    /// applying presentation deltas from the ECS world.
    /// </para>
    /// </remarks>
    internal sealed partial class World : IWorldContextApi
    {
        /// <summary>
        /// Registers a context object for the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance to associate with the entity.</param>
        /// <remarks>
        /// <para>
        /// The registry defines the replacement policy: depending on its
        /// configuration, registering a new context of the same runtime type
        /// may overwrite an existing one or coexist alongside it.
        /// </para>
        /// <para>
        /// Contexts are automatically cleaned up when the entity is despawned
        /// or when the world is reset.
        /// </para>
        /// </remarks>
        public void RegisterContext(Entity e, IContext ctx)
            => _contextRegistry.Register(this, e, ctx);

        /// <summary>
        /// Checks whether the entity has at least one context of the specified runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="contextType">Context runtime type to query for.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <paramref name="contextType"/>
        /// is registered for <paramref name="e"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool HasContext(Entity e, Type? contextType)
            => _contextRegistry.Has(this, e, contextType);

        /// <summary>
        /// Checks whether the entity has at least one context of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Context type to query for.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/>
        /// is registered for <paramref name="e"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool HasContext<T>(Entity e) where T : class, IContext
            => _contextRegistry.Has<T>(this, e);

        /// <summary>
        /// Returns all contexts currently registered for the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// An array of tuples where <c>type</c> is the runtime type of the
        /// context and <c>boxed</c> is the context instance boxed as
        /// <see cref="object"/>. Returns an empty array if no contexts are registered.
        /// </returns>
        public (Type type, object boxed)[] GetAllContexts(Entity e)
            => _contextRegistry.GetAllContexts(this, e);

        /// <summary>
        /// Removes a specific context instance from the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the specified context instance was registered
        /// and removed; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The call is a no-op if <paramref name="ctx"/> is not currently
        /// registered for <paramref name="e"/>.
        /// </remarks>
        public bool RemoveContext(Entity e, IContext ctx)
            => _contextRegistry.Remove(this, e, ctx);

        /// <summary>
        /// Requests that a context be reinitialized for the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance to reinitialize.</param>
        /// <returns>
        /// <see langword="true"/> if the context is known and the registry
        /// successfully reinitialized it; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Reinitialization semantics are defined by the registry and the
        /// concrete <see cref="IContext"/> implementation. A common pattern is
        /// to re-run setup logic after data changes or binder reattachment.
        /// </para>
        /// </remarks>
        public bool ReinitializeContext(Entity e, IContext ctx)
            => _contextRegistry.Reinitialize(this, e, ctx);
    }
}
