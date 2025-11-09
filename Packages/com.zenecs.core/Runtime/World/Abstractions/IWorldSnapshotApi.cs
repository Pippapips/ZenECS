// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Snapshot API
// File: IWorldSnapshotApi.cs
// Purpose: Serialize/deserialize full world state to a portable binary format.
// Key concepts:
//   • Magic header: versioned signature for compatibility checks.
//   • Metadata-first: NextId, Generation[], FreeIds[], AliveBits.
//   • Pool payloads: formatter-driven per component type.
//   • Migrations: run post-load fixups after pools are restored.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.IO;

namespace ZenECS.Core
{
    /// <summary>
    /// Snapshot I/O surface for saving and restoring entire world state.
    /// </summary>
    public interface IWorldSnapshotApi
    {
        /// <summary>
        /// Save a complete snapshot of the world into <paramref name="s"/>.
        /// Implementations should write a magic header and use a stable, portable format.
        /// </summary>
        void SaveFullSnapshotBinary(Stream s);

        /// <summary>
        /// Load a complete snapshot from <paramref name="s"/> and replace in-memory state.
        /// Implementations should validate the header and run post-load migrations.
        /// </summary>
        void LoadFullSnapshotBinary(Stream s);
    }
}