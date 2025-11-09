// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IVariableRunSystem.cs
// Purpose: Systems that execute every frame with a variable timestep.
// Key concepts:
//   • Equivalent to an Update()-style pass (dt varies per frame).
//   • Use for non-deterministic logic: input, UI, dynamic gameplay.
//   • Runs in the Simulation group during variable-timestep frames.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Runs once per variable-time frame (dt varies). Suitable for non-deterministic logic.
    /// </summary>
    public interface IVariableRunSystem : ISystem { }
}
