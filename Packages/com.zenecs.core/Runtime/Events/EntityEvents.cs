// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Events
// File: EntityEvents.cs
// Purpose: Global event hub for entity lifecycle notifications.
// Key concepts:
//   • World-internal signals: creation, destroy request, and finalized destruction.
//   • Ordering: Spawned → DestroyRequested → Destroyed (when applicable).
//   • Hygiene: Reset() clears all listeners to avoid leaks on reloads/tests.
//   • Scope: Events fire per-world and carry the target IWorld + Entity handle.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core
{
    /// <summary>
    /// Global event hub for entity lifecycle notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These events are raised by the world implementation to inform tools and
    /// observers about entity lifetime transitions. They are <b>not</b> intended to
    /// be used for gameplay logic; prefer systems and message buses for that.
    /// </para>
    /// <para>
    /// <b>Event order (when a full lifetime occurs):</b>
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <see cref="EntityCreated"/> — immediately after an entity is spawned.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="EntityDestroyRequested"/> — when a despawn is requested.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="EntityDestroy"/> — after the entity has been fully despawned and removed.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// Handlers should be <i>exception-safe</i>. Exceptions thrown from subscribers
    /// will propagate to the caller that raised the event.
    /// </para>
    /// </remarks>
    public static class EntityEvents
    {
        /// <summary>
        /// Raised immediately after an entity is created and has a valid handle.
        /// </summary>
        /// <remarks>
        /// At this point, the entity exists in the world's bookkeeping, but it may not
        /// yet have any components. If you need to react to specific components being
        /// added, listen to your component/binder deltas instead.
        /// </remarks>
        public static event Action<IWorld, Entity>? EntityCreated;

        /// <summary>
        /// Raised when a despawn has been requested for an entity.
        /// </summary>
        /// <remarks>
        /// This is emitted <i>before</i> the world actually removes the entity. Use this
        /// to preemptively invalidate caches or stop external processes that reference
        /// the entity. The entity may still be considered alive until removal completes.
        /// </remarks>
        public static event Action<IWorld, Entity>? EntityDestroyRequested;

        /// <summary>
        /// Raised after an entity has been fully removed from the world.
        /// </summary>
        /// <remarks>
        /// At this point, the entity is no longer alive and its id may be recycled in
        /// the future. Do not attempt to access components from the supplied handle.
        /// </remarks>
        public static event Action<IWorld, Entity>? EntityDestroy;

        /// <summary>
        /// Raise the <see cref="EntityCreated"/> event.
        /// </summary>
        /// <param name="w">World in which the entity was created.</param>
        /// <param name="e">The created entity handle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseCreated(IWorld w, Entity e) => EntityCreated?.Invoke(w, e);

        /// <summary>
        /// Raise the <see cref="EntityDestroyRequested"/> event.
        /// </summary>
        /// <param name="w">World requesting the entity's destruction.</param>
        /// <param name="e">The target entity handle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroyRequested(IWorld w, Entity e) => EntityDestroyRequested?.Invoke(w, e);

        /// <summary>
        /// Raise the <see cref="EntityDestroy"/> event.
        /// </summary>
        /// <param name="w">World in which the entity was destroyed.</param>
        /// <param name="e">The destroyed entity handle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroy(IWorld w, Entity e) => EntityDestroy?.Invoke(w, e);

        /// <summary>
        /// Clears all subscribers to prevent leaks during domain reloads, test runs, or runtime restarts.
        /// </summary>
        /// <remarks>
        /// Intended for internal use by the world/kernel during shutdown/reset paths.
        /// After calling this, all event handlers will be removed.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Reset()
        {
            EntityCreated = null;
            EntityDestroyRequested = null;
            EntityDestroy = null;
        }
    }
}
