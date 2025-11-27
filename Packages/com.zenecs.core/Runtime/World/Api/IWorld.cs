// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World contract
// File: IWorld.cs
// Purpose: Public façade for a single World instance, aggregating all world APIs.
// Key concepts:
//   • Aggregated surface: query, components, contexts, binders, messages, hooks.
//   • Lifetime control: initialize systems, pause/resume, reset with policy.
//   • Identity: stable WorldId + tags; per-world pause independent of Kernel.
//   • Safety: generation-based entity handles prevent stale references.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>
    /// Public façade for a single ECS world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A world instance owns and coordinates all ECS concerns for a given
    /// simulation space: entity/component storage, querying, contexts, binders,
    /// messaging, hooks, snapshots, command buffers, and workers.
    /// </para>
    /// <para>
    /// The world is designed to be composable: it can exist alongside other
    /// worlds inside a single <see cref="IKernel"/> and can be paused, reset,
    /// or destroyed independently.
    /// </para>
    /// </remarks>
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
        IWorldWorkerApi,
        IWorldSystemsApi
    {
        /// <summary>
        /// Gets the kernel that owns this world instance.
        /// </summary>
        /// <remarks>
        /// Use this to navigate back to multi-world coordination features such as
        /// world discovery, stepping, and selection.
        /// </remarks>
        IKernel Kernel { get; }

        /// <summary>
        /// Gets the stable identity of this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="WorldId"/> has value semantics and can safely be stored,
        /// logged, or transmitted. It does not change during the world's lifetime.
        /// </para>
        /// <para>
        /// To resolve an id back to a live world, use
        /// <see cref="IKernel.TryGet(WorldId, out IWorld)"/>.
        /// </para>
        /// </remarks>
        WorldId Id { get; }

        /// <summary>
        /// Gets or sets the human-readable name of this world.
        /// </summary>
        /// <remarks>
        /// The name is not guaranteed to be unique; use <see cref="Id"/> for
        /// identity and <see cref="IKernel.FindByName(string)"/> for discovery.
        /// </remarks>
        string Name { get; set; }

        /// <summary>
        /// Gets the read-only set of tags associated with this world.
        /// </summary>
        /// <remarks>
        /// Tags are intended for discovery and grouping, for example:
        /// <c>"Gameplay"</c>, <c>"Spectator"</c>, <c>"Server"</c>, etc.
        /// Use <see cref="IKernel.FindByTag(string)"/> or
        /// <see cref="IKernel.FindByAnyTag(string[])"/> to query by tag.
        /// </remarks>
        IReadOnlyCollection<string> Tags { get; }

        /// <summary>
        /// Gets the number of frames processed by this world.
        /// </summary>
        /// <remarks>
        /// Incremented once per successful frame tick (e.g. once per
        /// <c>BeginFrame/FrameInput/FrameSync</c> pass) while the world is not
        /// paused.
        /// </remarks>
        long FrameCount { get; }

        /// <summary>
        /// Gets the world-local simulation tick counter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is typically incremented once per fixed-step simulation tick and
        /// is useful for deterministic bookkeeping, such as time-based effects
        /// or recording when an event occurred in the simulation.
        /// </para>
        /// </remarks>
        long Tick { get; }

        /// <summary>
        /// Gets a value indicating whether this world is paused.
        /// </summary>
        /// <remarks>
        /// <para>
        /// World-level pause is independent of kernel-level pause:
        /// the kernel may continue ticking, but individual worlds can be stopped.
        /// </para>
        /// <para>
        /// System runners must respect this flag and avoid stepping paused worlds.
        /// </para>
        /// </remarks>
        bool IsPaused { get; }

        /// <summary>
        /// Gets a value indicating whether this world is in the process of disposing.
        /// </summary>
        /// <remarks>
        /// This flag can be used by systems or external code to avoid scheduling
        /// new work or spawning entities while the world is shutting down.
        /// </remarks>
        bool IsDisposing { get; }

        /// <summary>
        /// Pauses stepping for this world only.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When paused, core stepping callbacks (frame and fixed-step) should be
        /// skipped for this world even if the <see cref="IKernel"/> continues to tick.
        /// </para>
        /// <para>
        /// Pausing a world is idempotent; calling it multiple times has no extra effect.
        /// </para>
        /// </remarks>
        void Pause();

        /// <summary>
        /// Resumes stepping for this world after a previous pause.
        /// </summary>
        /// <remarks>
        /// This reverses the effect of <see cref="Pause"/> and allows the kernel
        /// to step this world again during frame and fixed-step ticks.
        /// </remarks>
        void Resume();

        /// <summary>
        /// Resets world storage and subsystems.
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to retain internal array capacities for a faster
        /// reset (soft reset); <see langword="false"/> to rebuild storage from the
        /// initial configuration (hard reset).
        /// </param>
        /// <remarks>
        /// <para>
        /// A reset clears entities, components, and subsystems, but the world
        /// object itself and its identity (<see cref="Id"/>, <see cref="Name"/>,
        /// <see cref="Tags"/>) remain valid.
        /// </para>
        /// <para>
        /// Systems may receive reinitialization according to the world's
        /// implementation policy.
        /// </para>
        /// </remarks>
        void Reset(bool keepCapacity);

        /// <summary>
        /// Gets the current generation value for an internal entity id.
        /// </summary>
        /// <param name="id">Internal entity id (index) to query.</param>
        /// <returns>
        /// The generation associated with the specified id.
        /// Returns <c>0</c> or another sentinel for ids that are not in use.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Generations are used to validate <c>Entity</c> handles and detect
        /// stale references: an <c>Entity</c> is valid only if both its id and
        /// generation match the values stored in the world.
        /// </para>
        /// </remarks>
        int GenerationOf(int id);
    }
}
