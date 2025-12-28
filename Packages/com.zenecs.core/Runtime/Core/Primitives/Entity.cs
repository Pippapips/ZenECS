// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World API
// File: Entity.cs
// Purpose: Lightweight 64-bit entity handle (generation|id) and helpers.
// Key concepts:
//   • Upper 32 bits: generation; lower 32 bits: id.
//   • Value semantics: equality/hash by packed handle; explicit casts.
//   • Safety: use world APIs to validate liveness/generation before access.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Packed 64-bit entity handle:
    /// <code>[ Gen (32 bits) | Id (32 bits) ]</code>
    /// </summary>
    /// <remarks>
    /// All data access should go through the world to validate liveness/generation.
    /// </remarks>
    public readonly struct Entity : IEquatable<Entity>
    {
        public static readonly Entity None = default;
        
        /// <summary>Raw 64-bit handle value (upper 32 = generation, lower 32 = id).</summary>
        public readonly ulong Handle;

        /// <summary>Bit shift amount for extracting the generation (upper 32 bits).</summary>
        public const int GenShift = 32;

        /// <summary>Mask for the lower 32 bits (entity id).</summary>
        public const ulong IdMask  = 0x00000000_FFFFFFFFUL;

        /// <summary>Mask for the upper 32 bits (generation).</summary>
        public const ulong GenMask = 0xFFFFFFFF_00000000UL;

        /// <summary>Gets the entity id (lower 32 bits).</summary>
        public int Id  => (int)(Handle & IdMask);

        /// <summary>Gets the generation (upper 32 bits).</summary>
        public int Gen => (int)(Handle >> GenShift);

        /// <summary>Create a new packed handle from an <paramref name="id"/> and <paramref name="gen"/>.</summary>
        public Entity(int id, int gen) => Handle = Pack(id, gen);

        /// <summary>Packs an id and generation into a 64-bit handle.</summary>
        public static ulong Pack(int id, int gen)
            => ((ulong)(uint)gen << GenShift) | (uint)id;

        /// <summary>Unpacks a 64-bit handle into its (<c>id</c>, <c>gen</c>) components.</summary>
        public static (int id, int gen) Unpack(ulong handle)
            => ((int)(handle & IdMask), (int)(handle >> GenShift));

        /// <inheritdoc />
        public bool Equals(Entity other) => Handle == other.Handle;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Entity e && e.Handle == Handle;

        /// <inheritdoc />
        public override int GetHashCode() => Handle.GetHashCode();

        /// <summary>Returns a human-readable string showing id and generation.</summary>
        public override string ToString() => $"Entity({Id}:{Gen})";

        /// <summary>Explicitly converts an <see cref="Entity"/> to its id (lower 32 bits).</summary>
        public static explicit operator int(Entity e) => e.Id;

        /// <summary>Explicitly converts an <see cref="Entity"/> to its raw 64-bit handle.</summary>
        public static explicit operator ulong(Entity e) => e.Handle;
        
        /// <summary>
        /// Returns true if this entity is Entity.None (zero handle).
        /// </summary>
        public bool IsNone => Handle == 0UL;

        /// <summary>
        /// Returns true if this entity is not Entity.None.
        /// (Note: does NOT guarantee the entity is alive; world must validate liveness.)
        /// </summary>
        public bool IsValid => Handle != 0UL;
    }
}
