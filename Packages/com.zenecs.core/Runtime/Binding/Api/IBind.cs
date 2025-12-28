// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBind.cs
// Purpose: Delta dispatch contracts that notify binders about component changes.
// Key concepts:
//   • Value-type deltas: Added / Changed / Removed per component T.
//   • Pull + push: binders may read contexts while reacting to deltas.
//   • Lightweight: struct payloads to avoid GC on hot paths.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Kind of component change that occurred on an entity.
    /// </summary>
    public enum ComponentDeltaKind
    {
        /// <summary>
        /// Component was added to the entity.
        /// </summary>
        Added,

        /// <summary>
        /// Component value changed in place on the entity.
        /// </summary>
        Changed,

        /// <summary>
        /// Component was removed from the entity.
        /// </summary>
        Removed,

        /// <summary>
        /// Snapshot of the current component value on the entity, dispatched
        /// when a binder is first attached so it can mirror the existing state.
        /// No structural change occurred; this is a sync-of-record event.
        /// </summary>
        Snapshot,
    }

    /// <summary>
    /// Immutable value describing a component delta for an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public readonly struct ComponentDelta<T> where T : struct
    {
        /// <summary>
        /// Entity where the change happened.
        /// </summary>
        public Entity Entity { get; }

        /// <summary>
        /// Kind of change (added, changed, removed, or snapshot).
        /// </summary>
        public ComponentDeltaKind Kind { get; }

        /// <summary>
        /// Component value associated with this delta.
        /// For <see cref="ComponentDeltaKind.Added"/>,
        /// <see cref="ComponentDeltaKind.Changed"/> and
        /// <see cref="ComponentDeltaKind.Snapshot"/> this is the
        /// current value on the entity at dispatch time.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Create a new delta value.
        /// </summary>
        /// <param name="e">Entity where the delta occurred.</param>
        /// <param name="k">Kind of delta.</param>
        /// <param name="v">Component value at the time of the delta.</param>
        public ComponentDelta(Entity e, ComponentDeltaKind k, in T v = default)
        {
            Entity = e;
            Kind = k;
            Value = v;
        }
    }

    /// <summary>
    /// Implemented by binders interested in receiving deltas for component <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public interface IBind<T> where T : struct
    {
        /// <summary>
        /// Called when a delta for <typeparamref name="T"/> is dispatched.
        /// </summary>
        /// <param name="delta">Delta payload (value-type).</param>
        void OnDelta(in ComponentDelta<T> delta);
    }
}
