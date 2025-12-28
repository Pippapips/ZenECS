// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystemLifecycle.cs
// Purpose: Optional lifecycle hooks for systems (Initialize/Shutdown).
// Key concepts:
//   • Allows setup/teardown around system execution.
//   • Called by SystemRunner before first tick and after the last tick.
//   • Implementations should be idempotent and resilient to multiple calls in tools/tests.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Provides optional lifecycle hooks for systems that need explicit setup/teardown.
    /// </summary>
    public interface ISystemLifecycle : ISystem
    {
        /// <summary>
        /// Called once before the first execution of any systems in the world.
        /// Use this to allocate caches or resolve references.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        void Initialize(IWorld w);

        /// <summary>
        /// Called once when the system runner shuts down systems (e.g., world dispose).
        /// Use this to release resources or unsubscribe from external events.
        /// </summary>
        void Shutdown();
    }
}