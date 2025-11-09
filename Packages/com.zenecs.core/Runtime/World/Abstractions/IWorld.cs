// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World contract
// File: IWorld.cs
// Purpose: Public façade for a single World instance, aggregating all world APIs.
// Key concepts:
//   • Aggregated surface: query, components, contexts, binders, messages, hooks.
//   • Lifetime control: initialize systems, pause/resume, reset with policy.
//   • Identity: stable WorldId + tags; per-world pause independent of Kernel.
//   • Safety: generation-based entity handles prevent stale references.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>
    /// Public façade for a single ECS world. Combines querying, entity/component
    /// management, binder/context integration, messaging, hooks, and worker APIs.
    /// </summary>
    public interface IWorld : IDisposable,
        IWorldQueryApi,
        IWorldQuerySpanApi,
        IWorldEntityApi,
        IWorldComponentApi,
        IWorldContextApi,
        IWorldBinderApi,
        IWorldSnapshotApi,
        IWorldMessagesApi,
        IWorldHookApi,
        IWorldCommandBufferApi,
        IWorldWorkerApi
    {
        /// <summary>Stable identity of this world (value semantics).</summary>
        WorldId Id { get; }

        /// <summary>Human-readable world name (no uniqueness guarantee).</summary>
        string Name { get; set; }

        /// <summary>Readonly set of tags for discovery/grouping.</summary>
        IReadOnlyCollection<string> Tags { get; }

        /// <summary>Whether this world is paused (independent of kernel pause).</summary>
        bool IsPaused { get; }

        /// <summary>Pause stepping for this world only.</summary>
        void Pause();

        /// <summary>Resume stepping for this world.</summary>
        void Resume();

        /// <summary>
        /// Reset world storage and subsystems.
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to keep array capacities (fast path);
        /// <see langword="false"/> to rebuild from initial config (hard reset).
        /// </param>
        void Reset(bool keepCapacity);

        /// <summary>
        /// Get the current generation value for an internal entity id (for handle validation).
        /// </summary>
        int GenerationOf(int id);

        /// <summary>
        /// Build and initialize systems for this world with the configured runner.
        /// </summary>
        /// <param name="systems">Optional explicit system set; runner may provide defaults.</param>
        /// <param name="warn">Optional warning sink for runner diagnostics.</param>
        void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null);
    }
}
