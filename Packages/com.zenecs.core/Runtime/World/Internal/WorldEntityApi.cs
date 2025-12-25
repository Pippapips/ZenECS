// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Entity API)
// File: WorldEntityApi.cs
// Purpose: Entity lifetime & indexing: create/destroy, alive checks, enumeration.
// Key concepts:
//   • Stable handles: (id, generation) prevent zombie references.
//   • Capacity growth policy: step-based or doubling with floor increments.
//   • Fast reset path: bulk clear without per-entity events when desired.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldEntityApi"/> for entity creation, destruction, and enumeration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial <c>World</c> implementation owns the low-level entity table
    /// (alive bitset, generation array, free-list, capacity growth policy) and
    /// exposes a small, stable surface to the rest of the runtime:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Spawn/respawn via <see cref="CreateEntity(int?)"/>.</description></item>
    ///   <item><description>Explicit reservation via <see cref="ReserveEntity(int?)"/> + <see cref="CreateReserved"/>.</description></item>
    ///   <item><description>Safe destruction via <see cref="DestroyEntity"/> and <see cref="DestroyAllEntities"/>.</description></item>
    /// </list>
    /// </remarks>
    internal sealed partial class World : IWorldEntityApi
    {
        /// <summary>
        /// Gets a snapshot of the number of currently alive entities.
        /// </summary>
        /// <remarks>
        /// The count is computed from <see cref="GetAllEntities"/> and reflects
        /// the state at the moment of access only.
        /// </remarks>
        public int AliveCount => GetAllEntities().Count;

        /// <summary>
        /// Checks whether the given handle refers to a live entity in this world.
        /// </summary>
        /// <param name="e">Entity handle to validate.</param>
        /// <returns>
        /// <see langword="true"/> if the entity is alive and the generation
        /// matches; otherwise <see langword="false"/>.
        /// </returns>
        public bool IsAlive(Entity e)
        {
            // Check bounds to prevent IndexOutOfRangeException after Reset
            if (e.Id < 0 || _generation == null || e.Id >= _generation.Length)
                return false;
            return _alive.Get(e.Id) && _generation[e.Id] == e.Gen;
        }

        /// <summary>
        /// Checks whether a raw (id, generation) pair refers to a live entity
        /// in this world.
        /// </summary>
        /// <param name="id">Internal entity id to check.</param>
        /// <param name="gen">Expected generation for the id.</param>
        /// <returns>
        /// <see langword="true"/> if the pair matches a live entity; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool IsAlive(int id, int gen)
        {
            return IsAlive(new Entity(id, gen));
        }

        /// <summary>
        /// Gets a snapshot list of all currently alive entities.
        /// </summary>
        /// <returns>
        /// A read-only list containing all entities that are alive at the time
        /// the method is called.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned list is a snapshot; subsequent spawn/despawn operations
        /// will not be reflected. Callers should still guard lookups with
        /// <see cref="IsAlive(Entity)"/> if safety is required.
        /// </para>
        /// </remarks>
        public IReadOnlyList<Entity> GetAllEntities()
        {
            var list = new List<Entity>(_nextId);
            for (int id = 1; id < _nextId; id++)
                if (_alive.Get(id))
                    list.Add(new Entity(id, _generation[id]));
            return list;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal helpers (capacity/growth)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures that internal entity storage can address the given id.
        /// </summary>
        /// <param name="id">Entity id to be allocated or validated.</param>
        /// <remarks>
        /// <para>
        /// This method is responsible for growing auxiliary arrays such as
        /// <c>_generation</c> in accordance with the configured growth policy.
        /// </para>
        /// </remarks>
        private void EnsureEntityCapacity(int id)
        {
            if (!_alive.Get(id)) _alive.Set(id, false);

            if (id >= _generation.Length)
            {
                int required = id + 1;
                int newLen = ComputeNewCapacity(_generation.Length, required);
                Array.Resize(ref _generation, newLen);
            }
        }

        /// <summary>
        /// Computes a new capacity for entity-related storage based on the
        /// current size, the required size, and the configured growth policy.
        /// </summary>
        /// <param name="current">Current capacity.</param>
        /// <param name="required">Minimum required capacity.</param>
        /// <returns>The new capacity that satisfies the requirement.</returns>
        /// <remarks>
        /// <para>
        /// When using <see cref="GrowthPolicy.Step"/>, capacity grows in fixed
        /// blocks of <c>GrowthStep</c>. Otherwise, capacity doubles until it
        /// reaches <paramref name="required"/>, with a minimum step of 256.
        /// </para>
        /// </remarks>
        private int ComputeNewCapacity(int current, int required)
        {
            if (_cfg.GrowthPolicy == GrowthPolicy.Step)
            {
                int step = _cfg.GrowthStep;
                int blocks = (required + step - 1) / step;
                return Math.Max(required, blocks * step);
            }
            else
            {
                int cap = Math.Max(16, current);
                while (cap < required)
                {
                    int next = cap * 2;
                    if (next - cap < 256) next = cap + 256;
                    cap = next;
                }
                return cap;
            }
        }

        /// <summary>
        /// Creates a new entity, optionally using a fixed id for restores/tests.
        /// </summary>
        /// <param name="fixedId">
        /// Optional explicit id to claim. When <see langword="null"/>, a new id
        /// is taken from the free-list or allocated at the end of the table.
        /// </param>
        /// <returns>
        /// A live <see cref="Entity"/> handle (id + current generation).
        /// </returns>
        /// <remarks>
        /// Internally this delegates to <see cref="ReserveEntity(int?)"/> followed
        /// by <see cref="CreateReserved(Entity)"/>.
        /// </remarks>
        internal Entity CreateEntity(int? fixedId = null)
        {
            var e = ReserveEntity(fixedId);
            CreateReserved(e);
            return e;
        }

        /// <summary>
        /// Reserves an entity id/generation pair without marking it alive.
        /// </summary>
        /// <param name="fixedId">
        /// Optional explicit id to claim. When <see langword="null"/>, either a
        /// free id is reused or a new one is allocated.
        /// </param>
        /// <returns>
        /// A reserved <see cref="Entity"/> handle (id + current generation)
        /// whose alive flag is still <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is intended for deferred structural changes via command buffers:
        /// systems can record operations against reserved entities and let the
        /// runner transition them into the alive state at a deterministic
        /// barrier using <see cref="CreateReserved(Entity)"/>.
        /// </para>
        /// </remarks>
        internal Entity ReserveEntity(int? fixedId = null)
        {
            int id;
            if (fixedId.HasValue)
            {
                id = fixedId.Value;
                EnsureEntityCapacity(id);
                // NOTE: do NOT set _alive here; stays false until CreateReserved
            }
            else if (_freeIds.Count > 0)
            {
                id = _freeIds.Pop();
                EnsureEntityCapacity(id);
                // _alive[id] stays false
            }
            else
            {
                id = _nextId++;
                EnsureEntityCapacity(id);
                // _alive[id] stays false
            }

            return new Entity(id, _generation[id]);
        }

        /// <summary>
        /// Transitions a previously reserved entity into the alive state
        /// and fires spawn events.
        /// </summary>
        /// <param name="e">Reserved entity handle to activate.</param>
        /// <remarks>
        /// Calling this method with an already alive entity is safe and
        /// treated as a no-op.
        /// </remarks>
        internal void CreateReserved(Entity e)
        {
            // Already alive? then do nothing (idempotent guard)
            if (IsAlive(e))
                return;

            // Mark as alive and raise events
            _alive.Set(e.Id, true);
            EntityEvents.RaiseCreated(this, e);
        }

        /// <summary>
        /// Destroys a live entity and tears down its associated data.
        /// </summary>
        /// <param name="e">Entity to destroy.</param>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="number">
        ///   <item><description>Raises <see cref="EntityEvents.EntityDestroyRequested"/>.</description></item>
        ///   <item><description>Clears associated singleton index entries.</description></item>
        ///   <item><description>Notifies binders and contexts for teardown.</description></item>
        ///   <item><description>Removes all components from the entity.</description></item>
        ///   <item><description>Marks the slot as free and increments generation.</description></item>
        ///   <item><description>Raises <see cref="EntityEvents.EntityDestroy"/>.</description></item>
        /// </list>
        /// <para>
        /// Calling this with a non-alive entity is a no-op.
        /// </para>
        /// </remarks>
        internal void DestroyEntity(Entity e)
        {
            if (!IsAlive(e)) return;

            EntityEvents.RaiseDestroyRequested(this, e);

            clearSingletonIndex(e);

            _bindingRouter.OnEntityDestroyed(this, e);
            _contextRegistry.Clear(this, e);
            _componentPoolRepository.RemoveEntity(e);

            _alive.Set(e.Id, false);
            _generation[e.Id]++;
            _freeIds.Push(e.Id);

            EntityEvents.RaiseDestroy(this, e);
        }

        /// <summary>
        /// Destroys all currently alive entities in this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each entity is destroyed via <see cref="DestroyEntity"/>, ensuring
        /// that all associated teardown logic and events are executed.
        /// </para>
        /// </remarks>
        internal void DestroyAllEntities()
        {
            for (int id = 1; id < _alive.Length; id++)
            {
                if (_alive.Get(id))
                    DestroyEntity(new Entity(id, GenerationOf(id)));
            }
        }
    }
}
