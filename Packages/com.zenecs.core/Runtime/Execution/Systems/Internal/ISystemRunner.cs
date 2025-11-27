// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: ISystemRunner.cs
// Purpose: Contract for building, initializing, ticking, and shutting down
//          world-bound ECS systems grouped by execution phase.
// Key concepts:
//   • Three-phase ticking: BeginFrame (variable) / FixedStep (fixed) / LateFrame (presentation).
//   • Deterministic order: runner executes in the order provided by the planner.
//   • World-scoped: a runner always acts on a single IWorld instance.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Systems.Internal
{
    /// <summary>
    /// Runs ECS systems bound to a single <see cref="IWorld"/>. The runner coordinates
    /// deterministic ordering, lifecycle, and per-phase execution.
    /// </summary>
    internal interface ISystemRunner : IDisposable
    {
        /// <summary>
        /// Queues a system instance for addition. The system is materialized at the next
        /// frame boundary (before <see cref="BeginFrame"/>), not immediately.
        /// </summary>
        void RequestAdd(ISystem system);

        /// <summary>
        /// Queues a batch of system instances for addition at the next frame boundary.
        /// </summary>
        void RequestAddRange(IEnumerable<ISystem> systems);

        /// <summary>
        /// Queues removal of the first system matching <typeparamref name="T"/>.
        /// The removal is applied at the next frame boundary.
        /// </summary>
        void RequestRemove<T>() where T : ISystem;

        /// <summary>
        /// Queues removal of the first system matching the provided <paramref name="t"/>.
        /// The removal is applied at the next frame boundary.
        /// </summary>
        void RequestRemove(Type t);

        /// <summary>
        /// Attempts to retrieve the first active system of type <typeparamref name="T"/>.
        /// </summary>
        bool TryGet<T>(out T? system) where T : class, ISystem;

        /// <summary>
        /// Attempts to retrieve the first active system of type/>.
        /// </summary>
        bool TryGet(Type t, out ISystem? system);
        
        /// <summary>
        /// Get all active systems.
        /// </summary>
        IReadOnlyList<ISystem> GetAllSystems();

        /// <summary>
        /// Enables or disables execution of the first active system of type <typeparamref name="T"/>.
        /// Systems must implement <c>ISystemEnabledFlag</c> to support this toggle.
        /// </summary>
        bool SetEnabled<T>(bool enabled) where T : ISystem;
        
        /// <summary>
        /// Is enable system of type <typeparamref name="T"/>.
        /// Systems must implement <c>ISystemEnabledFlag</c> to support this toggle.
        /// </summary>
        bool IsEnabled<T>() where T : ISystem;

        /// <summary>
        /// Is enable system of type/>.
        /// Systems must implement <c>ISystemEnabledFlag</c> to support this toggle.
        /// </summary>
        bool IsEnabled(Type t);

        /// <summary>
        /// Executes variable-timestep groups for the current frame (FrameSetup → Simulation).
        /// Pending mutations are applied before the first group runs.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Delta time (seconds).</param>
        void BeginFrame(IWorld w, float dt);

        /// <summary>
        /// Executes fixed-timestep groups (FrameSetup → Simulation). Structural changes are
        /// still deferred to the main-frame boundary.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="fixedDelta">Fixed step duration (seconds).</param>
        void FixedStep(IWorld w, float fixedDelta);

        /// <summary>
        /// Executes presentation (read-only) systems after applying router deltas. Write
        /// operations to the world should be temporarily denied in this phase.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Originating frame's delta (seconds).</param>
        /// <param name="alpha">Interpolation factor [0..1] for presentation.</param>
        void LateFrame(IWorld w, float dt, float alpha = 1.0f);
    }
}