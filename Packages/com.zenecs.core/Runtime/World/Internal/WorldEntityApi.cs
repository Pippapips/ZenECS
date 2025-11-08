#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ZenECS.Core.Binding;
using ZenECS.Core.DI;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.Bootstrap;
using ZenECS.Core.Internal.ComponentPooling;
using ZenECS.Core.Internal.Contexts;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Serialization;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// World implementation: storage, messaging, events, runner, binding host.
    /// All per-world services are resolved from the provided ServiceHost scope.
    /// </summary>
    internal sealed partial class World : IWorldEntityApi
    {
        public int AliveCount => GetAllEntities().Count;
        
        public bool IsAlive(Entity e) => _alive.Get(e.Id) && _generation[e.Id] == e.Gen;

        /// <summary>
        /// Returns a list of all currently alive entities.
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var list = new List<Entity>(_nextId);
            for (int id = 1; id < _nextId; id++)
                if (_alive.Get(id))
                    list.Add(new Entity(id, _generation[id]));
            return list;
        }

        private void EnsureEntityCapacity(int id)
        {
            // BitSet expansion and preservation are handled internally by Set().
            if (!_alive.Get(id)) _alive.Set(id, false);

            // Expand the generation array based on the configured growth policy.
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
                // Round up to the nearest multiple of step.
                int blocks = (required + step - 1) / step;
                return Math.Max(required, blocks * step);
            }
            else // Doubling
            {
                int cap = Math.Max(16, current);
                while (cap < required)
                {
                    int next = cap * 2;
                    // Guarantee at least +256 to avoid too small incremental growth.
                    if (next - cap < 256) next = cap + 256;
                    cap = next;
                }

                return cap;
            }
        }

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

            // The current slot's generation is embedded into the handle.
            var e = new Entity(id, _generation[id]);
            //EntityEvents.RaiseCreated(this, e);
            return e;
        }

        public void DespawnEntity(Entity e)
        {
            if (!IsAlive(e)) return;

            //EntityEvents.RaiseDestroyRequested(this, e);
            _bindingRouter.OnEntityDestroyed(this, e);
            _contextRegistry.Clear(this, e);

            _componentPoolRepository.RemoveEntity(e);

            _alive.Set(e.Id, false);

            // Increment generation: ensures that even if the same id is reused, the handle differs.
            _generation[e.Id]++;
            _freeIds.Push(e.Id);

            //EntityEvents.RaiseDestroyed(this, e);
        }
    }
}