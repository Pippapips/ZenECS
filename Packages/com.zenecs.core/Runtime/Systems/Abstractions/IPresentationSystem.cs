// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IPresentationSystem.cs
// Purpose: Systems responsible for post-simulation rendering or view updates.
// Key concepts:
//   • Read-only presentation: apply router deltas, then render/UI sync.
//   • Interpolation support: alpha provided for smoothing between frames.
//   • Avoid mutating ECS state here (writes may be guarded/denied by runner). 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Systems
{
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