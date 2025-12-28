// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemStateMachine.cs
// Purpose: State machine for managing system lifecycle states in SystemRunner.
// Key concepts:
//   • State transitions: Pending → Active → Initialized → ShuttingDown → Disposed
//   • Thread-safe state queries and transitions
//   • Clear separation of concerns for system lifecycle management
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Systems.Internal
{
    /// <summary>
    /// Represents the lifecycle state of a system within the SystemRunner.
    /// </summary>
    internal enum SystemState
    {
        /// <summary>
        /// System is queued for addition but not yet active.
        /// </summary>
        Pending,

        /// <summary>
        /// System is active but not yet initialized.
        /// </summary>
        Active,

        /// <summary>
        /// System has been initialized and is ready for execution.
        /// </summary>
        Initialized,

        /// <summary>
        /// System is in the process of shutting down.
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// System has been disposed and is no longer active.
        /// </summary>
        Disposed
    }

    /// <summary>
    /// State machine for managing system lifecycle states in SystemRunner.
    /// Provides type-safe state transitions and queries.
    /// </summary>
    internal sealed class SystemStateMachine
    {
        /// <summary>
        /// Maps each system to its current state.
        /// </summary>
        private readonly Dictionary<ISystem, SystemState> _systemStates = new();

        /// <summary>
        /// Systems queued for addition; applied at the next state transition.
        /// </summary>
        private readonly List<ISystem> _pendingAdd = new();

        /// <summary>
        /// System types queued for removal; applied at the next state transition.
        /// </summary>
        private readonly List<Type> _pendingRemove = new();

        /// <summary>
        /// Indicates whether there are pending mutations requiring state updates.
        /// </summary>
        private bool _dirty;

        /// <summary>
        /// Gets the current state of a system.
        /// </summary>
        /// <param name="system">The system to query.</param>
        /// <returns>The current state, or <see cref="SystemState.Disposed"/> if not found.</returns>
        public SystemState GetState(ISystem system)
        {
            return _systemStates.TryGetValue(system, out var state) ? state : SystemState.Disposed;
        }

        /// <summary>
        /// Gets all systems in a specific state.
        /// </summary>
        /// <param name="state">The state to filter by.</param>
        /// <returns>All systems in the specified state.</returns>
        public IEnumerable<ISystem> GetSystemsInState(SystemState state)
        {
            return _systemStates
                .Where(kvp => kvp.Value == state)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Gets all active systems (Active or Initialized state).
        /// </summary>
        /// <returns>All active systems.</returns>
        public IReadOnlyList<ISystem> GetActiveSystems()
        {
            return _systemStates
                .Where(kvp => kvp.Value == SystemState.Active || kvp.Value == SystemState.Initialized)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets all initialized systems.
        /// </summary>
        /// <returns>All initialized systems.</returns>
        public IEnumerable<ISystem> GetInitializedSystems()
        {
            return GetSystemsInState(SystemState.Initialized);
        }

        /// <summary>
        /// Queues a system for addition. The system will be in Pending state until ApplyPending is called.
        /// </summary>
        /// <param name="system">The system to add.</param>
        public void QueueAdd(ISystem system)
        {
            if (system == null) return;
            if (!_pendingAdd.Contains(system))
            {
                _pendingAdd.Add(system);
                _dirty = true;
            }
        }

        /// <summary>
        /// Queues multiple systems for addition.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        public void QueueAddRange(IEnumerable<ISystem> systems)
        {
            if (systems == null) return;

            foreach (var system in systems)
            {
                if (system != null && !_pendingAdd.Contains(system))
                {
                    _pendingAdd.Add(system);
                }
            }

            if (_pendingAdd.Count > 0)
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// Queues a system type for removal. The system will transition to ShuttingDown when ApplyPending is called.
        /// </summary>
        /// <param name="systemType">The type of system to remove.</param>
        public void QueueRemove(Type systemType)
        {
            if (systemType == null) return;
            if (!_pendingRemove.Contains(systemType))
            {
                _pendingRemove.Add(systemType);
                _dirty = true;
            }
        }

        /// <summary>
        /// Checks if there are pending mutations.
        /// </summary>
        public bool HasPendingMutations => _dirty;

        /// <summary>
        /// Applies pending additions and removals, transitioning systems to appropriate states.
        /// </summary>
        /// <param name="onRemove">Callback invoked when a system is being removed (before Shutdown).</param>
        /// <param name="onShutdown">Callback invoked to shutdown a system.</param>
        /// <returns>List of systems that were newly added (in Active state).</returns>
        public List<ISystem> ApplyPending(
            Action<ISystem>? onRemove = null,
            Action<ISystem>? onShutdown = null)
        {
            var newlyAdded = new List<ISystem>();

            if (!_dirty) return newlyAdded;

            // Process removals: Transition Initialized → ShuttingDown → Disposed
            if (_pendingRemove.Count > 0)
            {
                var systemsToRemove = _systemStates
                    .Where(kvp => _pendingRemove.Contains(kvp.Key.GetType()))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var system in systemsToRemove)
                {
                    var currentState = GetState(system);
                    
                    if (currentState == SystemState.Initialized)
                    {
                        // Transition: Initialized → ShuttingDown
                        _systemStates[system] = SystemState.ShuttingDown;
                        onShutdown?.Invoke(system);
                    }
                    else if (currentState == SystemState.Active)
                    {
                        // System was active but not initialized, no shutdown needed
                        // Just proceed to removal
                    }

                    // Transition: ShuttingDown/Active → Disposed and remove from state machine
                    _systemStates.Remove(system);
                    onRemove?.Invoke(system);
                }

                _pendingRemove.Clear();
            }

            // Process additions: Transition Pending → Active
            if (_pendingAdd.Count > 0)
            {
                foreach (var system in _pendingAdd)
                {
                    // Skip if already exists
                    if (_systemStates.ContainsKey(system))
                    {
                        var existingState = _systemStates[system];
                        // If it's disposed, we can reactivate it
                        if (existingState == SystemState.Disposed)
                        {
                            _systemStates[system] = SystemState.Active;
                            newlyAdded.Add(system);
                        }
                        continue;
                    }

                    // Transition: Pending → Active
                    _systemStates[system] = SystemState.Active;
                    newlyAdded.Add(system);
                }

                _pendingAdd.Clear();
            }

            _dirty = false;
            return newlyAdded;
        }

        /// <summary>
        /// Transitions a system from Active to Initialized state.
        /// </summary>
        /// <param name="system">The system to initialize.</param>
        /// <returns>True if the transition was successful, false if the system was not in Active state.</returns>
        public bool TransitionToInitialized(ISystem system)
        {
            if (system == null) return false;

            if (_systemStates.TryGetValue(system, out var currentState))
            {
                if (currentState == SystemState.Active)
                {
                    _systemStates[system] = SystemState.Initialized;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a system is in an active state (Active or Initialized).
        /// </summary>
        /// <param name="system">The system to check.</param>
        /// <returns>True if the system is active, false otherwise.</returns>
        public bool IsActive(ISystem system)
        {
            if (system == null) return false;
            var state = GetState(system);
            return state == SystemState.Active || state == SystemState.Initialized;
        }

        /// <summary>
        /// Checks if a system is initialized.
        /// </summary>
        /// <param name="system">The system to check.</param>
        /// <returns>True if the system is initialized, false otherwise.</returns>
        public bool IsInitialized(ISystem system)
        {
            if (system == null) return false;
            return GetState(system) == SystemState.Initialized;
        }

        /// <summary>
        /// Clears all state and pending operations. Used during disposal.
        /// </summary>
        public void Clear()
        {
            _systemStates.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
            _dirty = false;
        }

        /// <summary>
        /// Gets all systems that need to be initialized (in Active state).
        /// </summary>
        /// <returns>Systems that need initialization.</returns>
        public IEnumerable<ISystem> GetSystemsNeedingInitialization()
        {
            return GetSystemsInState(SystemState.Active);
        }

        /// <summary>
        /// Gets all systems that need to be shut down (in Initialized state, ordered for shutdown).
        /// </summary>
        /// <param name="shutdownOrder">The order in which systems should be shut down (reverse of initialization order).</param>
        /// <returns>Systems that need shutdown, in the specified order.</returns>
        public IEnumerable<ISystem> GetSystemsNeedingShutdown(IEnumerable<ISystem> shutdownOrder)
        {
            var initializedSet = GetSystemsInState(SystemState.Initialized).ToHashSet();
            return shutdownOrder.Where(initializedSet.Contains);
        }
    }
}

