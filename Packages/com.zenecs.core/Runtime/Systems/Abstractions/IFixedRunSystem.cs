// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFixedRunSystem.cs
// Purpose: Systems executed on a fixed timestep (deterministic updates).
// Key concepts:
//   • Physics/lockstep logic: consistent dt independent of frame rate.
//   • Runs during the Simulation group within the fixed-step pass.
//   • Scheduler supplies the fixed delta time to Run(IWorld, dt). 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Runs on a constant fixed step; ideal for physics or other deterministic updates.
    /// </summary>
    public interface IFixedRunSystem : ISystem { }
}