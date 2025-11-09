// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFixedSetupSystem.cs
// Purpose: Systems that prepare state before fixed-timestep Simulation.
// Key concepts:
//   • Snapshot/prepare: queue swapping, pre-physics buffers, cached queries.
//   • Invoked inside the fixed-step pass prior to fixed Simulation.
//   • Should not rely on frame delta; use the provided fixed step if needed.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Executed during fixed-step before physics/deterministic updates to prepare state.
    /// </summary>
    public interface IFixedSetupSystem : ISystem { }
}