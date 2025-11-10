// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Systems API
// File: IWorldSystemsApi.cs
// Purpose: Runtime-safe system management surface exposed by IWorld.
// Key concepts:
//   • Frame-bound mutations: add/remove/enable are queued and applied at frame start.
//   • Discovery: try-get to query active systems.
//   • Adapter-agnostic: no DI framework types leak into the Core surface.
// Copyright (c) 2025 Pippapips Limited
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
        void RequestAdd(ISystem system);

        /// <summary>Queues multiple systems for addition at the next frame boundary.</summary>
        void RequestAddRange(IEnumerable<ISystem> systems);

        /// <summary>Queues the first system of type <typeparamref name="T"/> for removal.</summary>
        void RequestRemove<T>() where T : ISystem;

        /// <summary>Queues the first system of the specified <paramref name="t"/> for removal.</summary>
        void RequestRemove(Type t);

        /// <summary>
        /// Attempts to retrieve the first active system of type <typeparamref name="T"/>.
        /// </summary>
        bool TryGet<T>(out T? system) where T : class, ISystem;

        /// <summary>
        /// Enables or disables execution of the first active system of type <typeparamref name="T"/>.
        /// The system must implement <c>ISystemEnabledFlag</c>.
        /// </summary>
        bool SetEnabled<T>(bool enabled) where T : ISystem;
    }
}
