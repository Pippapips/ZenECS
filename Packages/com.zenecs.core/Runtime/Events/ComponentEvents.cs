// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Events
// File: ComponentEvents.cs
// Purpose: Global event hub for component add/remove notifications.
// Key concepts:
//   • World-internal signals: component added / removed on a specific entity.
//   • Type-safe payload: strongly typed generic raisers, boxed + Type in event.
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
    /// Global event hub for component lifecycle notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These events are raised by the world implementation to inform tools and
    /// observers when components are added to or removed from entities.
    /// They are <b>not</b> intended to be used for core gameplay logic;
    /// prefer systems, queries, and message buses for that.
    /// </para>
    /// <para>
    /// Events are fired per-world and always carry the target
    /// <see cref="IWorld"/> instance and <see cref="Entity"/> handle, plus
    /// the component <see cref="Type"/> (and value for additions).
    /// </para>
    /// <para>
    /// Handlers should be <i>exception-safe</i>. Exceptions thrown from
    /// subscribers will propagate to the caller that raised the event.
    /// </para>
    /// </remarks>
    public static class ComponentEvents
    {
        /// <summary>
        /// Raised when a component is added to an entity.
        /// </summary>
        /// <remarks>
        /// The <see cref="Type"/> argument is the component type (e.g., <c>typeof(MyComponent)</c>),
        /// and the <see cref="object"/> argument is the boxed component value at the time of addition.
        /// </remarks>
        public static event Action<IWorld, Entity, Type, object>? ComponentAdded;

        /// <summary>
        /// Raised when a component is removed from an entity.
        /// </summary>
        /// <remarks>
        /// The <see cref="Type"/> argument is the component type that was removed.
        /// </remarks>
        public static event Action<IWorld, Entity, Type>? ComponentRemoved;

        /// <summary>
        /// Raises the <see cref="ComponentAdded"/> event for a strongly typed component value.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="w">World where the component was added.</param>
        /// <param name="e">Entity to which the component was added.</param>
        /// <param name="value">Component value at the time of addition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseAdded<T>(IWorld w, Entity e, in T value) where T : struct
            => ComponentAdded?.Invoke(w, e, typeof(T), value!);

        /// <summary>
        /// Raises the <see cref="ComponentRemoved"/> event for a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="w">World where the component was removed.</param>
        /// <param name="e">Entity from which the component was removed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseRemoved<T>(IWorld w, Entity e) where T : struct
            => ComponentRemoved?.Invoke(w, e, typeof(T));

        /// <summary>
        /// Clears all listeners from <see cref="ComponentAdded"/> and <see cref="ComponentRemoved"/>.
        /// </summary>
        /// <remarks>
        /// Call this during world shutdown, domain reload, or test teardown to
        /// avoid leaking event handlers across runs.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Reset()
        {
            ComponentAdded = null;
            ComponentRemoved = null;
        }
    }
}
