// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystem.cs
// Purpose: Base system interface and group categorization.
// Key concepts:
//   • All systems implement ISystem and belong to a single execution group.
//   • Systems receive (IWorld, dt) on execution; specialized interfaces refine phases.
//   • Deterministic ordering is provided externally by the SystemRunner/Planner.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Optional enable/disable flag for systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Systems that implement this interface allow tooling and infrastructure
    /// code to toggle their active state without removing them from the world.
    /// </para>
    /// <para>
    /// System runners should honor this flag and skip execution when
    /// <see cref="Enabled"/> is <see langword="false"/>.
    /// </para>
    /// </remarks>
    public interface ISystemEnabledFlag
    {
        /// <summary>
        /// Gets or sets a value indicating whether the system is enabled.
        /// </summary>
        /// <remarks>
        /// When set to <see langword="false"/>, the system should not be executed
        /// by the scheduler or system runner.
        /// </remarks>
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Logical execution group for systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// System groups are used by planners and runners to build deterministic
    /// execution pipelines. Each system should belong to exactly one group.
    /// </para>
    /// <para>
    /// Fixed-step groups are intended for deterministic simulation, while
    /// frame groups are variable-step and typically used for presentation,
    /// input, and UI.
    /// </para>
    /// </remarks>
    public enum SystemGroup
    {
        /// <summary>
        /// Group is unknown or not explicitly specified.
        /// </summary>
        Unknown,

        // Fixed-step deterministic simulation

        /// <summary>
        /// Fixed-step input phase (player input sampling, commands, etc.).
        /// </summary>
        FixedInput,

        /// <summary>
        /// Fixed-step decision phase (AI, pathfinding, control decisions).
        /// </summary>
        FixedDecision,

        /// <summary>
        /// Fixed-step simulation phase (physics, gameplay state updates).
        /// </summary>
        FixedSimulation,

        /// <summary>
        /// Fixed-step post-simulation phase (cleanup, events, bookkeeping).
        /// </summary>
        FixedPost,

        // Variable-step frame run (non-deterministic)

        /// <summary>
        /// Per-frame input phase (device input, view events, transient commands).
        /// </summary>
        FrameInput,

        /// <summary>
        /// Per-frame sync phase (camera, client prediction, view-space logic).
        /// </summary>
        FrameSync,

        /// <summary>
        /// Per-frame view phase (interpolation, transforms, animation, mesh binding).
        /// </summary>
        FrameView,

        /// <summary>
        /// Per-frame UI phase (UI, HUD, debug overlays).
        /// </summary>
        FrameUI,
    }

    /// <summary>
    /// Base interface for all ECS systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specialized interfaces such as <c>IFixedRunSystem</c>,
    /// <c>IFrameRunSystem</c>, or <c>IFrameLateSystem</c> refine when and how
    /// <see cref="Run"/> is invoked (for example fixed vs variable timestep,
    /// early vs late frame).
    /// </para>
    /// <para>
    /// Deterministic ordering between systems is handled externally by the
    /// system planner/runner and is typically based on <see cref="SystemGroup"/>
    /// plus an explicit ordering key.
    /// </para>
    /// </remarks>
    public interface ISystem
    {
        /// <summary>
        /// Executes the system logic against the given world.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="dt">
        /// Time delta in seconds.
        /// For fixed-step systems this equals the fixed timestep;
        /// for frame systems it is the frame delta;
        /// some setup systems may ignore this value.
        /// </param>
        void Run(IWorld w, float dt);
    }
}
