// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBind.cs
// Purpose: Delta dispatch contracts that notify binders about component changes.
// Key concepts:
//   • Value-type deltas: Added / Changed / Removed per component T.
//   • Pull + push: binders may read contexts while reacting to deltas.
//   • Lightweight: struct payloads to avoid GC on hot paths.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Kind of component change that occurred on an entity.
    /// </summary>
    public enum ComponentDeltaKind
    {
        /// <summary>Component was added.</summary>
        Added,
        /// <summary>Component value changed (in-place update).</summary>
        Changed,
        /// <summary>Component was removed.</summary>
        Removed
    }

    /// <summary>
    /// Immutable value describing a component delta for an <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public readonly struct ComponentDelta<T> where T : struct
    {
        /// <summary>Entity where the change happened.</summary>
        public Entity Entity { get; }

        /// <summary>Kind of change.</summary>
        public ComponentDeltaKind Kind { get; }

        /// <summary>New value (for <see cref="ComponentDeltaKind.Added"/> / <see cref="ComponentDeltaKind.Changed"/>).</summary>
        public T Value { get; }

        /// <summary>Create a new delta value.</summary>
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
        /// <summary>Called when a delta for <typeparamref name="T"/> is dispatched.</summary>
        /// <param name="delta">Delta payload (value-type).</param>
        void OnDelta(in ComponentDelta<T> delta);
    }
}
