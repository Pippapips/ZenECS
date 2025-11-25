// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Reset API)
// File: WorldResetApi.cs
// Purpose: Reset/rehydrate world storage and subsystems with/without capacity reuse.
// Key concepts:
//   • Fast path reset: keep arrays/capacity; clear data; rebuild pools empty.
//   • Hard reset: discard storage; recreate from initial configuration.
//   • Subsystem hooks: pre/post reset partials to coordinate services.
//   • Safety: flush jobs/cmd buffers; clear caches and hook queues before reuse.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal.ComponentPooling;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldResetApi"/>: world-level reset operations.
    /// </summary>
    internal sealed partial class World : IWorldResetApi
    {
        /// <summary>
        /// Reset the world either by preserving current capacity (fast) or by fully rebuilding (hard).
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to keep current array capacities and just clear/reinit;
        /// <see langword="false"/> to discard storage and rebuild from initial config.
        /// </param>
        public void Reset(bool keepCapacity)
        {
            if (keepCapacity) ResetButKeepCapacity();
            else HardReset();
        }

        /// <summary>
        /// Called <b>before</b> the reset sequence — subsystems can cleanup transient state.
        /// </summary>
        /// <param name="keepCapacity">Mirror of <see cref="Reset(bool)"/>; see remarks on strategy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnBeforeWorldReset(bool keepCapacity);

        /// <summary>
        /// Called <b>after</b> the reset sequence — subsystems can rebuild caches or state.
        /// </summary>
        /// <param name="keepCapacity">Mirror of <see cref="Reset(bool)"/>; indicates path taken.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnAfterWorldReset(bool keepCapacity);
        
        /// <summary>
        /// Reset cross-cutting subsystems (jobs, command buffers, hooks, caches, events).
        /// </summary>
        /// <param name="keepCapacity">Whether the fast path was chosen.</param>
        private void ResetSubsystems(bool keepCapacity)
        {
            OnBeforeWorldReset(keepCapacity);

            // Command buffers
            ClearAllCommandBuffers();

            // Job scheduler
            _worker.ClearAllScheduledJobs();

            // Hooks / event queues
            _permissionHook.ClearAllHookQueues();
            
            ClearWritePhase();

            // Query / filter caches
            ResetQueryCaches();

            // Static entity events (global within process)
            EntityEvents.Reset();
            
            OnAfterWorldReset(keepCapacity);
        }

        /// <summary>
        /// Fast reset: retain capacity, clear data, and recreate empty pools sized to current cap.
        /// </summary>
        private void ResetButKeepCapacity()
        {
            int entityCap = Math.Max(_alive.Length, _generation?.Length ?? 0);

            // Reinitialize alive / generation arrays but keep their size
            _alive = new BitSet(entityCap);
            if (_generation == null || _generation.Length != entityCap)
                _generation = new int[entityCap];
            else
                Array.Clear(_generation, 0, _generation.Length);

            // Reset ID allocation state
            _nextId = 1; // 0 is reserved
            if (_freeIds == null)
                _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity);
            else
                _freeIds.Clear();

            // Recreate each component pool in an empty state (capacity retained)
            var types = new List<Type>(_componentPoolRepository.Pools.Keys);
            foreach (var t in types)
            {
                _componentPoolRepository.Pools[t] = CreateEmptyPoolForType(t, entityCap);
            }

            ResetSubsystems(keepCapacity: true);
        }

        /// <summary>
        /// Hard reset: discard all storage and caches, rebuild to initial configured capacities.
        /// </summary>
        private void HardReset()
        {
            _alive = new BitSet(_cfg.InitialEntityCapacity);
            _generation = new int[_cfg.InitialEntityCapacity];

            _nextId = 1;
            _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity);

            _componentPoolRepository.Pools.Clear();

            ResetSubsystems(keepCapacity: false);
        }

        /// <summary>
        /// Create a new empty component pool for <paramref name="compType"/> sized to <paramref name="cap"/>.
        /// </summary>
        /// <param name="compType">Component type to allocate a pool for.</param>
        /// <param name="cap">Desired capacity (entity slots).</param>
        /// <returns>An empty pool instance ready for use.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IComponentPool CreateEmptyPoolForType(Type compType, int cap)
        {
            var closed = typeof(ComponentPool<>).MakeGenericType(compType);
            var ctorWithCap = closed.GetConstructor(new[] { typeof(int) });
            if (ctorWithCap != null)
            {
                return (IComponentPool)Activator.CreateInstance(closed, cap)!;
            }

            // Fallback: use default factory and expand manually.
            var factory = _componentPoolRepository.GetOrBuildPoolFactory(compType);
            var pool = factory();
            if (cap > 0) pool.EnsureCapacity(cap - 1);
            return pool;
        }
    }
}
