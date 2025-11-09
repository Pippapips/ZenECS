// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFrameSetupSystem.cs
// Purpose: Systems that run once per frame before Simulation.
// Key concepts:
//   • Frame preparation: input snapshotting, buffer swapping, world preconditions.
//   • Runs prior to Simulation on variable frames and also in fixed-step pass if needed.
//   • Should not depend on precise delta time (dt may be ignored or advisory only). 
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
}