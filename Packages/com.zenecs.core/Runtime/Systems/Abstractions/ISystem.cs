// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystem.cs
// Purpose: Base system interface and group categorization.
// Key concepts:
//   • All systems implement ISystem and belong to a single execution group.
//   • Systems receive (IWorld, dt) on execution; specialized interfaces refine phases.
//   • Deterministic ordering is provided externally by the SystemRunner/Planner.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// High-level groups used by the scheduler to order system execution.
    /// </summary>
    public enum SystemGroup
    {
        /// <summary>Executed before Simulation — input polling, buffer swaps, preparation.</summary>
        FrameSetup,
        /// <summary>Main game logic — physics, AI, gameplay updates.</summary>
        Simulation,
        /// <summary>Executed after Simulation — rendering, UI, data→view presentation.</summary>
        Presentation
    }

    /// <summary>
    /// Base interface for all ECS systems. Specialized interfaces (e.g.,
    /// <see cref="IFrameSetupSystem"/>, <see cref="IFixedRunSystem"/>,
    /// <see cref="IVariableRunSystem"/>, <see cref="IPresentationSystem"/>)
    /// refine the phase/semantics of <see cref="Run"/>.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Execute the system logic against the given world.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="dt">
        /// Time delta in seconds. For fixed-step calls this equals the fixed step;
        /// for FrameSetup it may be ignored; for Presentation it’s the source frame delta.
        /// </param>
        void Run(IWorld w, float dt);
    }
}