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
    public interface ISystemEnabledFlag
    {
        bool Enabled { get; set; }
    }
    
    public enum SystemGroup
    {
        // Fixed-step deterministic simulation
        FixedInput,
        FixedDecision,
        FixedSimulation,
        FixedPost,

        // Variable-step frame run (non-deterministic)
        FrameInput,   // Unity Input, 디바이스, 뷰 이벤트 수집
        FrameView,    // 카메라, 클라 예측, 뷰 로직
        Presentation, // 보간 + Transform/애니/메시 바인딩
        FrameUI,      // UI/HUD/디버그
    }
    
    /// <summary>
    /// Base interface for all ECS systems. Specialized interfaces (e.g.,
    /// <see cref="IFrameSetupSystem"/>, <see cref="IFixedRunSystem"/>,
    /// <see cref="IFrameRunSystem"/>, <see cref="IPresentationSystem"/>)
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