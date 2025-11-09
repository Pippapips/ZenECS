// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: ISystemRunner.cs
// Purpose: Contract for building, initializing, ticking, and shutting down
//          world-bound ECS systems grouped by execution phase.
// Key concepts:
//   • Three-phase ticking: BeginFrame (variable) / FixedStep (fixed) / LateFrame (presentation).
//   • Deterministic order: runner executes in the order provided by the planner.
//   • Lifecycle: explicit Initialize/Shutdown around the first/last tick.
//   • World-scoped: a runner always acts on a single IWorld instance.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal.Systems
{
    /// <summary>
    /// Runs ECS systems bound to a single <see cref="IWorld"/>. The runner is responsible
    /// for deterministic ordering, lifecycle calls, and per-phase execution.
    /// </summary>
    internal interface ISystemRunner
    {
        /// <summary>
        /// Analyze and cache the system set and execution plan.
        /// </summary>
        /// <param name="systems">Optional explicit system collection; may be <c>null</c>.</param>
        /// <param name="warn">Optional warning sink for non-fatal planning notes.</param>
        void Build(IEnumerable<ISystem>? systems, Action<string>? warn);

        /// <summary>
        /// Initialize the cached systems (called once before the first tick).
        /// </summary>
        /// <param name="w">Target world.</param>
        void Initialize(IWorld w);

        /// <summary>
        /// Shutdown the systems in reverse order (called once when the world is disposed or runner stops).
        /// </summary>
        /// <param name="w">Target world.</param>
        void Shutdown(IWorld w);

        /// <summary>
        /// Execute variable-timestep systems for the current frame (FrameSetup → Simulation).
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Delta time for this frame in seconds.</param>
        void BeginFrame(IWorld w, float dt);

        /// <summary>
        /// Execute fixed-timestep systems (FrameSetup → Simulation).
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="fixedDelta">Fixed step duration in seconds.</param>
        void FixedStep(IWorld w, float fixedDelta);

        /// <summary>
        /// Execute presentation (read-only) systems (Late stage).
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="dt">Delta time of the originating frame in seconds.</param>
        /// <param name="alpha">Interpolation factor in [0,1] for presentation.</param>
        void LateFrame(IWorld w, float dt, float alpha = 1.0f);
    }
}
