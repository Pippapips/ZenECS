// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBindingRouter.cs
// Purpose: Router interface for binder attach/detach, delta dispatch, and frame apply.
// Key concepts:
//   • Ordered per-entity binder list (Priority + attach order).
//   • Type-routed deltas → IBind<T>.
//   • Single frame barrier: ApplyAll() before Presentation.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding.Internal
{
    /// <summary>
    /// Router that owns binder lists per entity, validates requirements,
    /// fans out deltas, and applies binders each frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The router is responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Managing binder attachments per <see cref="Entity"/>.</description></item>
    /// <item><description>Maintaining a stable ordered list (by <see cref="IBinder.ApplyOrder"/> and attach order).</description></item>
    /// <item><description>Dispatching component deltas to matching <see cref="IBind{T}"/> binders.</description></item>
    /// <item><description>Running a per-frame <see cref="ApplyAll"/> pass to update views.</description></item>
    /// </list>
    /// <para>
    /// This is an internal engine service; gameplay code should typically not
    /// interact with it directly.
    /// </para>
    /// </remarks>
    internal interface IBindingRouter
    {
        /// <summary>
        /// Determines whether at least one binder of type <typeparamref name="T"/>
        /// is attached to the specified entity.
        /// </summary>
        /// <typeparam name="T">Binder type to check for.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if there is at least one binder of type
        /// <typeparamref name="T"/> attached to <paramref name="e"/>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool Has<T>(Entity e) where T : class, IBinder;

        /// <summary>
        /// Attach a binder to the specified entity.
        /// </summary>
        /// <param name="w">World that owns the entity.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">
        /// Binder instance to attach. If <see langword="null"/>, the call is ignored.
        /// </param>
        /// <remarks>
        /// Implementations should:
        /// <list type="bullet">
        /// <item><description>Call <see cref="IBinder.Bind"/> once during attachment.</description></item>
        /// <item><description>Insert the binder into the per-entity list according to its <see cref="IBinder.ApplyOrder"/> and attach order.</description></item>
        /// </list>
        /// </remarks>
        void Attach(IWorld w, Entity e, IBinder? binder);

        /// <summary>
        /// Detach a specific binder instance from the entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">Binder instance to detach.</param>
        /// <remarks>
        /// Implementations must call <see cref="IBinder.Unbind"/> exactly once
        /// when detaching the binder. If the binder is not attached, this
        /// operation should be a no-op.
        /// </remarks>
        void Detach(Entity e, IBinder binder);

        /// <summary>
        /// Detach the first binder whose runtime type matches
        /// <paramref name="binderType"/> from the entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binderType">Binder type to remove.</param>
        /// <returns>
        /// <see langword="true"/> if a matching binder was found and detached;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Matching is typically done via exact type comparison, not
        /// assignability, unless otherwise documented by an implementation.
        /// </remarks>
        bool Detach(Entity e, Type binderType);

        /// <summary>
        /// Detach all binders from the specified entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <remarks>
        /// This must call <see cref="IBinder.Unbind"/> for every attached binder,
        /// and clear the per-entity binder list.
        /// </remarks>
        void DetachAll(Entity e);

        /// <summary>
        /// Notify the router that the entity was destroyed so it can
        /// automatically detach all binders.
        /// </summary>
        /// <param name="w">World in which the entity was destroyed.</param>
        /// <param name="e">Destroyed entity handle.</param>
        /// <remarks>
        /// This is typically invoked by the world when an entity is despawned.
        /// Implementations are expected to treat this similar to
        /// <see cref="DetachAll(Entity)"/> but may also perform additional
        /// bookkeeping related to entity death.
        /// </remarks>
        void OnEntityDestroyed(IWorld w, Entity e);

        /// <summary>
        /// Invoke <see cref="IBinder.Apply"/> for all binders attached to all entities.
        /// </summary>
        /// <param name="w">World for which the apply pass is being executed.</param>
        /// <remarks>
        /// <para>
        /// This method is typically called once per frame (or tick) during the
        /// Presentation phase to flush all collected deltas into the view layer.
        /// </para>
        /// <para>
        /// Implementations must honor the ordering defined by
        /// <see cref="IBinder.ApplyOrder"/> and the binder attach order.
        /// </para>
        /// </remarks>
        void ApplyAll(IWorld w);

        /// <summary>
        /// Dispatch a component delta to binders attached to the target entity
        /// that implement <see cref="IBind{T}"/>.
        /// </summary>
        /// <typeparam name="T">Component value type for the delta.</typeparam>
        /// <param name="d">Delta payload.</param>
        /// <remarks>
        /// <para>
        /// Implementations route the delta only to binders on
        /// <see cref="ComponentDelta{T}.Entity"/> that implement
        /// <see cref="IBind{T}"/>, calling <see cref="IBind{T}.OnDelta"/>
        /// for each.
        /// </para>
        /// <para>
        /// The router is not responsible for deciding when deltas are generated;
        /// it only delivers them to subscribers.
        /// </para>
        /// </remarks>
        void Dispatch<T>(in ComponentDelta<T> d) where T : struct;

        /// <summary>
        /// Returns all binders attached to the specified entity as boxed instances.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// An array of pairs where <c>type</c> is the binder's runtime
        /// <see cref="Type"/> and <c>boxed</c> is the binder instance.
        /// Returns an empty array if the entity has no binders.
        /// </returns>
        /// <remarks>
        /// This method is intended primarily for tooling (e.g., inspectors,
        /// explorers) and should not be used in hot gameplay paths.
        /// </remarks>
        (Type type, object boxed)[] GetAllBinders(Entity e);

        /// <summary>
        /// Returns a read-only list view of all binders attached to the specified entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
            /// A read-only list of <see cref="IBinder"/> instances, or
        /// <see langword="null"/> if the entity has no binders tracked by the router.
        /// </returns>
        /// <remarks>
        /// The returned list is intended to be treated as immutable by callers.
        /// Implementations may reuse internal storage; do not cache the list
        /// across frames unless the implementation explicitly guarantees stability.
        /// </remarks>
        IReadOnlyList<IBinder>? GetAllBinderList(Entity e);
    }
}
