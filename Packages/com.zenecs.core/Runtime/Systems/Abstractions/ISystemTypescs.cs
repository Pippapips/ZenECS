// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystemLifecycle.cs
// Purpose: Optional lifecycle hooks for systems (Initialize/Shutdown).
// Key concepts:
//   • Allows setup/teardown around system execution.
//   • Called by SystemRunner before first tick and after the last tick.
//   • Implementations should be idempotent and resilient to multiple calls in tools/tests.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Executed before Simulation each frame to prepare state (input, buffers, etc.).
    /// </summary>
    public interface IFrameSetupSystem : ISystem { }
    
    /// <summary>
    /// Runs once per variable-time frame (dt varies). Suitable for non-deterministic logic.
    /// </summary>
    public interface IFrameRunSystem : ISystem { }
    
    /// <summary>
    /// Executed during fixed-step before physics/deterministic updates to prepare state.
    /// </summary>
    public interface IFixedSetupSystem : ISystem { }
 
    /// <summary>
    /// Runs on a constant fixed step; ideal for physics or other deterministic updates.
    /// </summary>
    public interface IFixedRunSystem : ISystem { }
    
    /// <summary>
    /// Executed during the Presentation phase. Use for rendering, UI updates,
    /// and data→view synchronization. Avoid mutating ECS state here.
    /// </summary>
    public interface IPresentationSystem : ISystem
    {
        /// <summary>
        /// Execute the presentation logic with interpolation.
        /// </summary>
        /// <param name="w">The ECS world.</param>
        /// <param name="dt">Delta time of the originating frame in seconds.</param>
        /// <param name="alpha">Interpolation factor in [0,1] (1=current, 0=previous).</param>
        void Run(IWorld w, float dt, float alpha);

        /// <summary>
        /// Default shim to satisfy <see cref="ISystem.Run(ZenECS.Core.IWorld, float)"/>.
        /// Calls <see cref="Run(IWorld, float, float)"/> with <c>alpha = 1f</c>.
        /// </summary>
        void ISystem.Run(IWorld w, float dt) => Run(w, dt, 1f);
    }
}