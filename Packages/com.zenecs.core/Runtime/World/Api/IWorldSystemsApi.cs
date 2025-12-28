// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Systems API
// File: IWorldSystemsApi.cs
// Purpose: Runtime-safe system management surface exposed by IWorld.
// Key concepts:
//   • Frame-bound mutations: add/remove/enable are queued and applied at frame start.
//   • Discovery: try-get to query active systems.
//   • Adapter-agnostic: no DI framework types leak into the Core surface.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>
    /// Public system-management API surfaced by <see cref="IWorld"/> for runtime
    /// extensibility. Mutations are applied at the next frame boundary to keep
    /// ticks deterministic and thread-safe.
    /// </summary>
    public interface IWorldSystemsApi
    {
        /// <summary>Queues a system instance for addition at the next frame boundary.</summary>
        void AddSystem(ISystem system);

        /// <summary>Queues multiple systems for addition at the next frame boundary.</summary>
        void AddSystems(IEnumerable<ISystem> systems);

        /// <summary>Queues the first system of type <typeparamref name="T"/> for removal.</summary>
        void RemoveSystem<T>() where T : ISystem;

        /// <summary>Queues the first system of the specified <paramref name="t"/> for removal.</summary>
        void RemoveSystem(Type t);

        /// <summary>
        /// Attempts to retrieve the first active system of type <typeparamref name="T"/>.
        /// </summary>
        bool TryGetSystem<T>(out T? system) where T : class, ISystem;

        /// <summary>
        /// Attempts to retrieve the first active system of the specified type.
        /// </summary>
        /// <param name="t">System type to retrieve.</param>
        /// <param name="system">
        /// When this method returns, contains the system instance if found;
        /// otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a system of type <paramref name="t"/> was found;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool TryGetSystem(Type t, out ISystem? system);

        /// <summary>
        /// Gets all currently active systems registered in this world.
        /// </summary>
        /// <returns>
        /// A read-only list containing all active systems, in the order they
        /// are scheduled for execution.
        /// </returns>
        IReadOnlyList<ISystem> GetAllSystems();

        /// <summary>
        /// Enables or disables execution of the first active system of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">System type that must implement <see cref="ISystemEnabledFlag"/>.</typeparam>
        /// <param name="enabled">
        /// <see langword="true"/> to enable the system; <see langword="false"/> to disable it.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a system of type <typeparamref name="T"/> was found
        /// and its enabled state was updated; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The system must implement <see cref="ISystemEnabledFlag"/>. Systems that are
        /// disabled will be skipped during execution by the system runner.
        /// </remarks>
        bool SetEnabledSystem<T>(bool enabled) where T : ISystem;
        
        /// <summary>
        /// Checks whether the first active system of type <typeparamref name="T"/> is currently enabled.
        /// </summary>
        /// <typeparam name="T">System type that must implement <see cref="ISystemEnabledFlag"/>.</typeparam>
        /// <returns>
        /// <see langword="true"/> if the system is enabled; <see langword="false"/> if it is
        /// disabled or does not exist.
        /// </returns>
        /// <remarks>
        /// The system must implement <see cref="ISystemEnabledFlag"/>. If the system does not
        /// exist, this method returns <see langword="false"/>.
        /// </remarks>
        bool IsEnabledSystem<T>() where T : ISystem;
        
        /// <summary>
        /// Checks whether the first active system of the specified type is currently enabled.
        /// </summary>
        /// <param name="t">System type that must implement <see cref="ISystemEnabledFlag"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the system is enabled; <see langword="false"/> if it is
        /// disabled or does not exist.
        /// </returns>
        /// <remarks>
        /// The system must implement <see cref="ISystemEnabledFlag"/>. If the system does not
        /// exist, this method returns <see langword="false"/>.
        /// </remarks>
        bool IsEnabledSystem(Type t);
    }
}
