// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EntityEvents.cs
// Purpose: Global event hub for entity lifecycle notifications.
// Key concepts:
//   • World-internal signals: creation, destroy request, and finalized destruction.
//   • Ordering: Spawned → DestroyRequested → Destroyed (when applicable).
//   • Hygiene: Reset() clears all listeners to avoid leaks on reloads/tests.
//   • Scope: Events fire per-world and carry the target IWorld + Entity handle.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Events
{
    /// <summary>
    /// Global event hub for entity lifecycle notifications.
    /// </summary>
    /// <remarks>
    /// These events are raised by the world implementation to inform tools and
    /// observers about entity lifetime transitions. They are <b>not</b> intended to
    /// be used for gameplay logic; prefer systems and message buses for that.
    ///
    /// <para><b>Event order (when a full lifetime occurs):</b></para>
    /// <list type="number">
    ///   <item><description><see cref="EntitySpawned"/> — immediately after an entity is spawned.</description></item>
    ///   <item><description><see cref="EntityDespawnRequested"/> — when a despawn is requested.</description></item>
    ///   <item><description><see cref="EntityDespawned"/> — after the entity has been fully despawned and removed.</description></item>
    /// </list>
    ///
    /// <para>
    /// Handlers should be <i>exception-safe</i>. Exceptions thrown from subscribers
    /// will propagate to the caller that raised the event.
    /// </para>
    /// </remarks>
    public static class ComponentEvents
    {
        public static event Action<IWorld, Entity, Type, object>? ComponentAdded;
        public static event Action<IWorld, Entity, Type>? ComponentRemoved;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseAdded<T>(IWorld w, Entity e, in T value) where T : struct
            => ComponentAdded?.Invoke(w, e, typeof(T), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseRemoved<T>(IWorld w, Entity e) where T : struct
            => ComponentRemoved?.Invoke(w, e, typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Reset()
        {
            ComponentAdded = null;
            ComponentRemoved = null;
        }
    }
}
