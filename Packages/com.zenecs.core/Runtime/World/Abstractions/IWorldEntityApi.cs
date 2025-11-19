// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Entity API
// File: IWorldEntityApi.cs
// Purpose: Entity lifetime & indexing: spawn/despawn, alive checks, enumeration.
// Key concepts:
//   • Stable handles: (id, generation) prevent zombie references.
//   • Capacity policy: growth strategy is implementation-defined.
//   • Bulk ops: fast destroy-all with/without per-entity events.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>Entity lifetime management for a world.</summary>
    public interface IWorldEntityApi
    {
        /// <summary>Number of alive entities (snapshot value).</summary>
        int AliveCount { get; }

        /// <summary>Returns <c>true</c> if the handle refers to a live entity.</summary>
        bool IsAlive(Entity e);

        /// <summary>Returns <c>true</c> if the handle refers to a live entity.</summary>
        bool IsAlive(int id, int gen);

        // /// <summary>Create a new entity (optionally with a fixed id for restores/tests).</summary>
        // Entity SpawnEntity(int? fixedId = null);
        //
        // /// <summary>Destroy a live entity; id may later be recycled.</summary>
        // void DespawnEntity(Entity e);

        /// <summary>Destroy all entities; optionally fire per-entity events.</summary>
        // void DespawnAllEntities(bool fireEvents = false);

        /// <summary>Get a snapshot list of all currently alive entities.</summary>
        IReadOnlyList<Entity> GetAllEntities();
    }
}