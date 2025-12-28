// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IContextRegistry.cs
// Purpose: Registry + lookup interface for per-entity contexts.
// Key concepts:
//   • Extends IContextLookup for read paths.
//   • Manages Initialize/Deinitialize/Reinitialize for registered contexts.
//   • World-scoped lifetime; Clear and ClearAll for teardown.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding.Internal
{
    /// <summary>
    /// Registry that stores contexts per entity and manages their lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="IContextRegistry"/> is owned by a single world and is responsible
    /// for associating <see cref="IContext"/> instances with entities, as well as
    /// driving optional lifecycle hooks (<see cref="IContextInitialize"/> and
    /// <see cref="IContextReinitialize"/>).
    /// </para>
    /// <para>
    /// It also implements <see cref="IContextLookup"/>, providing read-only access
    /// for binders and systems to discover contexts attached to entities.
    /// </para>
    /// </remarks>
    internal interface IContextRegistry : IContextLookup
    {
        // Register / Remove (registry manages Initialize/Deinitialize & initialized flag)

        /// <summary>
        /// Registers a context instance for the specified entity and world.
        /// </summary>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity to attach the context to.</param>
        /// <param name="ctx">Context instance to register.</param>
        /// <remarks>
        /// <para>
        /// If a context of the same runtime type is already registered for
        /// <paramref name="e"/>, it is replaced. If the old instance implements
        /// <see cref="IContextInitialize"/> and was initialized, its
        /// <see cref="IContextInitialize.Deinitialize"/> method is invoked before
        /// being replaced.
        /// </para>
        /// <para>
        /// If <paramref name="ctx"/> implements <see cref="IContextInitialize"/>,
        /// its <see cref="IContextInitialize.Initialize"/> method is called after
        /// registration and its initialized state is tracked.
        /// </para>
        /// </remarks>
        void Register(IWorld w, Entity e, IContext ctx);

        /// <summary>
        /// Removes a specific context instance from an entity.
        /// </summary>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <param name="ctx">Exact context instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the context was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// If the removed context implements <see cref="IContextInitialize"/> and
        /// was previously initialized, its <see cref="IContextInitialize.Deinitialize"/>
        /// method is invoked before removal.
        /// </remarks>
        bool Remove(IWorld w, Entity e, IContext ctx);

        /// <summary>
        /// Removes a context from an entity by type.
        /// </summary>
        /// <typeparam name="T">Context type to remove.</typeparam>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/>
        /// was found and removed; otherwise <see langword="false"/>.
        /// </returns>
        bool Remove<T>(IWorld w, Entity e) where T : class, IContext;

        // Reinitialize (fast path or Deinit→Init fallback)

        /// <summary>
        /// Reinitializes a specific context instance if supported.
        /// </summary>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <param name="ctx">Context instance to reinitialize.</param>
        /// <returns>
        /// <see langword="true"/> if a matching registered context was found and
        /// reinitialized; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the registered context implements <see cref="IContextReinitialize"/>,
        /// <see cref="IContextReinitialize.Reinitialize"/> is invoked.
        /// </para>
        /// <para>
        /// Otherwise, if it implements <see cref="IContextInitialize"/>, the
        /// registry falls back to calling
        /// <see cref="IContextInitialize.Deinitialize"/> followed by
        /// <see cref="IContextInitialize.Initialize"/>.
        /// </para>
        /// </remarks>
        bool Reinitialize(IWorld w, Entity e, IContext ctx);

        /// <summary>
        /// Reinitializes the context of type <typeparamref name="T"/> attached to the entity.
        /// </summary>
        /// <typeparam name="T">Context type to reinitialize.</typeparam>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/>
        /// was found and reinitialized; otherwise <see langword="false"/>.
        /// </returns>
        bool Reinitialize<T>(IWorld w, Entity e) where T : class, IContext;

        // State / cleanup

        /// <summary>
        /// Returns whether the specific context instance is currently marked as initialized.
        /// </summary>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <param name="ctx">Context instance to query.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="ctx"/> is registered for
        /// <paramref name="e"/> and has been initialized; otherwise <see langword="false"/>.
        /// </returns>
        bool IsInitialized(IWorld w, Entity e, IContext ctx);

        /// <summary>
        /// Returns whether a context of type <typeparamref name="T"/> is currently
        /// marked as initialized for the specified entity.
        /// </summary>
        /// <typeparam name="T">Context type to query.</typeparam>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity the context is attached to.</param>
        /// <returns>
        /// <see langword="true"/> if a context of type <typeparamref name="T"/> is
        /// registered for <paramref name="e"/> and initialized; otherwise <see langword="false"/>.
        /// </returns>
        bool IsInitialized<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>
        /// Removes all contexts for a single entity and runs their deinitializers.
        /// </summary>
        /// <param name="w">World owning the entity.</param>
        /// <param name="e">Entity whose contexts should be cleared.</param>
        /// <remarks>
        /// For every registered context that implements <see cref="IContextInitialize"/>
        /// and is marked as initialized, <see cref="IContextInitialize.Deinitialize"/>
        /// is invoked before removal.
        /// </remarks>
        void Clear(IWorld w, Entity e);

        /// <summary>
        /// Removes all contexts for all entities in the world and runs their deinitializers.
        /// </summary>
        /// <remarks>
        /// Intended for world teardown or reset paths. After this method returns,
        /// the registry no longer holds any references to contexts.
        /// </remarks>
        void ClearAll();
    }
}
