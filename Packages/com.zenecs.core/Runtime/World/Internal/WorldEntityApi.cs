// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Entity API)
// File: WorldEntityApi.cs
// Purpose: Entity lifetime & indexing: spawn/despawn, alive checks, enumeration.
// Key concepts:
//   • Stable handles: (id, generation) prevent zombie references.
//   • Capacity growth policy: step-based or doubling with floor increments.
//   • Fast reset path: bulk clear without per-entity events when desired.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Events;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldEntityApi"/> for entity creation, destruction, and enumeration.
    /// </summary>
    internal sealed partial class World : IWorldEntityApi
    {
        /// <inheritdoc/>
        public int AliveCount => GetAllEntities().Count;

        /// <inheritdoc/>
        public bool IsAlive(Entity e) => _alive.Get(e.Id) && _generation[e.Id] == e.Gen;

        /// <summary>
        /// Get a snapshot list of all currently alive entities.
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var list = new List<Entity>(_nextId);
            for (int id = 1; id < _nextId; id++)
                if (_alive.Get(id))
                    list.Add(new Entity(id, _generation[id]));
            return list;
        }

        // Internal helpers (capacity/growth) ----------------------------------

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
        /// Create a new entity, optionally using a fixed id (for restores/tests).
        /// </summary>
        /// <param name="fixedId">Optional explicit id to claim.</param>
        /// <returns>A live <see cref="Entity"/> handle (id + current generation).</returns>
        public Entity SpawnEntity(int? fixedId = null)
        {
            int id;
            if (fixedId.HasValue)
            {
                id = fixedId.Value;
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }
            else if (_freeIds.Count > 0)
            {
                id = _freeIds.Pop();
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }
            else
            {
                id = _nextId++;
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }

            var e = new Entity(id, _generation[id]);
            EntityEvents.RaiseSpawned(this, e);
            return e;
        }

        /// <summary>
        /// Destroy a live entity. Dispatches binder/context teardown and events.
        /// </summary>
        /// <param name="e">Entity to destroy.</param>
        public void DespawnEntity(Entity e)
        {
            if (!IsAlive(e)) return;

            EntityEvents.RaiseDespawnRequested(this, e);

            _bindingRouter.OnEntityDestroyed(this, e);
            _contextRegistry.Clear(this, e);
            _componentPoolRepository.RemoveEntity(e);

            _alive.Set(e.Id, false);
            _generation[e.Id]++;
            _freeIds.Push(e.Id);

            EntityEvents.RaiseDespawned(this, e);
        }

        /// <summary>
        /// Destroy all currently alive entities.
        /// </summary>
        /// <param name="fireEvents">
        /// If <see langword="true"/>, per-entity events are fired (slower).
        /// If <see langword="false"/>, uses a fast reset path.
        /// </param>
        public void DespawnAllEntities(bool fireEvents = false)
        {
            if (!fireEvents)
            {
                ResetButKeepCapacity();
                return;
            }

            for (int id = 1; id < _alive.Length; id++)
            {
                if (_alive.Get(id))
                    DespawnEntity(new Entity(id, GenerationOf(id)));
            }
        }
    }
}
