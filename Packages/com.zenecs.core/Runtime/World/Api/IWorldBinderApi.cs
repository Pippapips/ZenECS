// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Binder API
// File: IWorldBinderApi.cs
// Purpose: Attach/detach view binders to entities and control attach policy.
// Key concepts:
//   • Decoupled view: binders consume contexts and deltas; world hosts lifecycle.
//   • Safety: binders are detached on entity despawn/reset.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    /// <summary>
    /// Adapter-facing binder surface: attach and detach view binders to entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Binders live on the view side (for example Unity GameObjects) and are
    /// associated with ECS entities via this API. The world remains the single
    /// source of truth for binder lifetimes and ensures that:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    /// Binders are detached when entities are despawned or the world is reset.
    ///   </description></item>
    ///   <item><description>
    /// Attach policies (see <c>AttachOptions</c> in the binding layer) are honored
    /// when required contexts are missing.
    ///   </description></item>
    ///   <item><description>
    /// Multiple binders can be attached to the same entity, but each binder
    /// instance is tracked only once.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public interface IWorldBinderApi
    {
        /// <summary>
        /// Checks whether an entity currently has a binder of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Binder type to query for.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if at least one binder of type
        /// <typeparamref name="T"/> is attached to <paramref name="e"/>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool HasBinder<T>(Entity e) where T : class, IBinder;

        /// <summary>
        /// Attaches a binder instance to an entity using the world's default attach policy.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">
        /// Binder instance to attach. If <see langword="null"/>, the call is ignored.
        /// </param>
        /// <remarks>
        /// <para>
        /// Implementations are expected to:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>
        /// Validate that required contexts are present and behave according to
        /// the world's configured attach policy.
        ///   </description></item>
        ///   <item><description>
        /// Ensure the same binder instance is not attached multiple times.
        ///   </description></item>
        ///   <item><description>
        /// Register the binder so it will be detached automatically if
        /// <paramref name="e"/> is despawned or the world is reset.
        ///   </description></item>
        /// </list>
        /// </remarks>
        void AttachBinder(Entity e, IBinder? binder);

        /// <summary>
        /// Detaches all binders currently attached to an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <remarks>
        /// This is typically used during teardown of a view object or when
        /// re-binding an entity to a new set of view components.
        /// </remarks>
        void DetachAllBinders(Entity e);

        /// <summary>
        /// Detaches a specific binder instance from an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">Binder instance to detach.</param>
        /// <remarks>
        /// The call is a no-op if the specified binder is not currently
        /// attached to <paramref name="e"/>.
        /// </remarks>
        void DetachBinder(Entity e, IBinder binder);

        /// <summary>
        /// Detaches binders from an entity by binder type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="t">Binder type to remove.</param>
        /// <returns>
        /// <see langword="true"/> if at least one binder of type <paramref name="t"/>
        /// was detached; otherwise <see langword="false"/>.
        /// </returns>
        bool DetachBinder(Entity e, Type t);

        /// <summary>
        /// Returns all binders attached to an entity as a type/value pair array.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// An array of tuples where <c>type</c> is the binder runtime type and
        /// <c>boxed</c> is the binder instance boxed as <see cref="object"/>.
        /// Returns an empty array if no binders are attached.
        /// </returns>
        (Type type, object boxed)[] GetAllBinders(Entity e);

        /// <summary>
        /// Returns a read-only list view of all binder instances attached to an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// A read-only list of <see cref="IBinder"/> instances, or
        /// <see langword="null"/> if no binders are attached.
        /// </returns>
        IReadOnlyList<IBinder>? GetAllBinderList(Entity e);
    }
}