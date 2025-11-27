// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Identity
// File: WorldId.cs
// Purpose: Strongly-typed, value-semantics identifier for a World.
// Key concepts:
//   • Strong typedef over Guid: prevents accidental misuse of bare GUIDs.
//   • Pure value semantics: equality/hash based solely on the underlying Guid.
//   • Serialization-friendly: stable, compact, framework-agnostic.
//   • Readability: explicit intent in APIs (WorldId vs Guid).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Stable, value-type identifier for a <see cref="IWorld"/>.
    /// Wraps a <see cref="Guid"/> to provide strong typing, readable intent,
    /// and well-defined equality semantics across application domains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a wrapper around <see cref="Guid"/>?</b>
    /// A dedicated type prevents accidental misuse of arbitrary GUIDs where a
    /// world identity is required, and allows documenting/expanding semantics
    /// without leaking implementation details to callers.
    /// </para>
    /// <para>
    /// <b>Equality &amp; hashing</b> are based solely on the underlying
    /// <see cref="Guid"/> value, making this struct safe to use as a dictionary
    /// key or in sets.
    /// </para>
    /// </remarks>
    public readonly struct WorldId : IEquatable<WorldId>
    {
        /// <summary>
        /// The underlying globally unique value that represents this world id.
        /// </summary>
        public Guid Value { get; }

        /// <summary>
        /// Create a new <see cref="WorldId"/> from an existing <see cref="Guid"/>.
        /// </summary>
        /// <param name="value">The GUID to wrap.</param>
        public WorldId(Guid value) => Value = value;

        /// <summary>
        /// Value equality: returns <c>true</c> if the wrapped GUIDs are equal.
        /// </summary>
        /// <param name="other">The other id to compare against.</param>
        public bool Equals(WorldId other) => Value.Equals(other.Value);

        /// <summary>
        /// Value equality override: returns <c>true</c> if <paramref name="obj"/>
        /// is a <see cref="WorldId"/> with the same underlying GUID.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object? obj) => obj is WorldId o && Equals(o);

        /// <summary>
        /// Hash code derived from the underlying <see cref="Guid"/>.
        /// </summary>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Returns the canonical string representation of the underlying GUID.
        /// </summary>
        public override string ToString() => Value.ToString();

        /// <summary>
        /// Equality operator (see <see cref="Equals(WorldId)"/>).
        /// </summary>
        public static bool operator ==(WorldId a, WorldId b) => a.Equals(b);

        /// <summary>
        /// Inequality operator (see <see cref="Equals(WorldId)"/>).
        /// </summary>
        public static bool operator !=(WorldId a, WorldId b) => !a.Equals(b);
    }
}
