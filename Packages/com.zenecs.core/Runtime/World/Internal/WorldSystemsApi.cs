// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Systems API (Adapter)
// File: WorldSystemsApi.cs
// Purpose: Bridge world-level system requests to the runner implementation.
// Key concepts:
//   • Thin delegation: world forwards to its runner.
//   • Encapsulation: runner remains internal to the world.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// <see cref="IWorldSystemsApi"/> implementation that forwards calls to the
    /// internal <c>ISystemRunner</c> instance.
    /// </summary>
    internal sealed partial class World : IWorldSystemsApi
    {
        /// <inheritdoc/>
        public void AddSystem(ISystem system) => _runner.RequestAdd(system);

        /// <inheritdoc/>
        public void AddSystems(IEnumerable<ISystem> systems) => _runner.RequestAddRange(systems);

        /// <inheritdoc/>
        public void RemoveSystem<T>() where T : ISystem => _runner.RequestRemove<T>();

        /// <inheritdoc/>
        public void RemoveSystem(Type t) => _runner.RequestRemove(t);

        /// <inheritdoc/>
        public bool TryGetSystem<T>(out T? system) where T : class, ISystem => _runner.TryGet(out system);

        /// <inheritdoc/>
        public bool TryGetSystem(Type t, out ISystem? system) => _runner.TryGet(t, out system);

        /// <inheritdoc/>
        public IReadOnlyList<ISystem> GetAllSystems() => _runner.GetAllSystems();

        /// <inheritdoc/>
        public bool SetEnabledSystem<T>(bool enabled) where T : ISystem => _runner.SetEnabled<T>(enabled);

        /// <inheritdoc/>
        public bool IsEnabledSystem<T>() where T : ISystem => _runner.IsEnabled<T>();
        
        /// <inheritdoc/>
        public bool IsEnabledSystem(Type t) => _runner.IsEnabled(t);
    }
}