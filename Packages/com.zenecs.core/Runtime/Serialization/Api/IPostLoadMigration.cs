// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization
// File: IPostLoadMigration.cs
// Purpose: Define post-load migrations executed after pools are restored.
// Key concepts:
//   • Idempotent: multiple runs yield the same final state.
//   • Ordered: ascending Order, then type-name tie-break for determinism.
//   • Scope: world-wide fixups (rebinding, index rebuilds, data corrections). 
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// A migration step that runs after a full snapshot has been loaded and all
    /// component pools have been restored.
    /// </summary>
    /// <remarks>
    /// Implementations <b>must be idempotent</b>. Use to rebuild indices, rebind
    /// contexts, or adjust data layouts between versions.
    /// </remarks>
    public interface IPostLoadMigration
    {
        /// <summary>Execution priority; lower values run earlier.</summary>
        int Order { get; }

        /// <summary>
        /// Execute the migration against the provided <paramref name="world"/>.
        /// </summary>
        /// <param name="world">Target world instance.</param>
        void Run(IWorld world);
    }
}