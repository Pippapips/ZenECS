// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Entity API
// File: IWorldEntityApi.cs
// Purpose: Entity lifetime & indexing: spawn/despawn, alive checks, enumeration.
// Key concepts:
//   • Stable handles: (id, generation) prevent zombie references.
//   • Capacity policy: growth strategy is implementation-defined.
//   • Bulk ops: fast destroy-all with/without per-entity events.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>
    /// Entity lifetime management surface for a world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A world internally tracks entities as (id, generation) pairs. The
    /// <see cref="Entity"/> handle presented to callers wraps these values and
    /// allows the world to detect stale or "zombie" references: if an entity is
    /// despawned and its slot is later reused with a higher generation, the
    /// original handle no longer passes <see cref="IsAlive(Entity)"/>.
    /// </para>
    /// <para>
    /// This interface focuses on read-only aspects of entity lifetime:
    /// querying how many entities are alive, whether a handle still refers to
    /// a live entity, and enumerating all alive entities. Creation and
    /// destruction APIs are typically exposed by the concrete world
    /// implementation.
    /// </para>
    /// </remarks>
    public interface IWorldEntityApi
    {
        /// <summary>
        /// Gets a snapshot of the number of currently alive entities.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value is a snapshot at the time of access; concurrent spawns or
        /// despawns may change the underlying count immediately after reading.
        /// </para>
        /// <para>
        /// This is primarily intended for debugging, diagnostics, and high-level
        /// UI such as "entity count" overlays.
        /// </para>
        /// </remarks>
        int AliveCount { get; }

        /// <summary>
        /// Checks whether a handle refers to a live entity.
        /// </summary>
        /// <param name="e">Entity handle to validate.</param>
        /// <returns>
        /// <see langword="true"/> if the handle refers to an entity that is
        /// currently alive in this world; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method compares both the internal id and generation stored in
        /// <paramref name="e"/> against the world's entity table. If the slot
        /// has been reclaimed and its generation incremented, the handle is
        /// considered invalid and the method returns <see langword="false"/>.
        /// </para>
        /// </remarks>
        bool IsAlive(Entity e);

        /// <summary>
        /// Checks whether a raw (id, generation) pair refers to a live entity.
        /// </summary>
        /// <param name="id">Internal entity id (index) to check.</param>
        /// <param name="gen">Expected generation for the id.</param>
        /// <returns>
        /// <see langword="true"/> if the pair refers to an entity that is
        /// currently alive in this world; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This overload is useful in low-level code paths where you already
        /// have separate id and generation values (for example, from serialized
        /// data or custom handles) and want to validate them without constructing
        /// an <see cref="Entity"/> wrapper.
        /// </para>
        /// </remarks>
        bool IsAlive(int id, int gen);

        /// <summary>
        /// Gets a snapshot list of all currently alive entities.
        /// </summary>
        /// <returns>
        /// A read-only list containing all entities that are alive at the moment
        /// the snapshot is taken.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned collection is a snapshot; subsequent spawns or despawns
        /// will not be reflected. Callers should not assume that entities remain
        /// alive for the duration of iteration and should still guard lookups
        /// with <see cref="IsAlive(Entity)"/> if safety is required.
        /// </para>
        /// <para>
        /// This method is primarily intended for debugging, profiling, and
        /// editor tooling that needs a quick, enumerable view of world contents.
        /// </para>
        /// </remarks>
        IReadOnlyList<Entity> GetAllEntities();
    }
}
