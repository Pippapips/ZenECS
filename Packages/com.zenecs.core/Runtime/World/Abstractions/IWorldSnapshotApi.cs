// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Snapshot API
// File: IWorldSnapshotApi.cs
// Purpose: Serialize/deserialize full world state to a portable binary format.
// Key concepts:
//   • Magic header: versioned signature for compatibility checks.
//   • Metadata-first: NextId, Generation[], FreeIds[], AliveBits.
//   • Pool payloads: formatter-driven per component type.
//   • Migrations: run post-load fixups after pools are restored.
// Copyright (c) 2026 Pippapips Limited
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
    /// <remarks>
    /// <para>
    /// Implementations are responsible for emitting a stable, portable binary format
    /// that can be loaded across process runs (and, ideally, platforms), including a
    /// versioned magic header to detect incompatibilities early.
    /// </para>
    /// <para>
    /// A snapshot is expected to capture:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Entity metadata (next id, generations, free list, alive mask).</description></item>
    ///   <item><description>All component pools and their per-entity values.</description></item>
    ///   <item><description>Any additional world-level state required for correct restore.</description></item>
    /// </list>
    /// </remarks>
    public interface IWorldSnapshotApi
    {
        /// <summary>
        /// Saves a complete snapshot of the world into the given stream.
        /// </summary>
        /// <param name="s">Writable stream that will receive the snapshot bytes.</param>
        /// <remarks>
        /// <para>
        /// Implementations should:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>Write a magic header and version tag.</description></item>
        ///   <item><description>Encode metadata such as NextId, Generation[], FreeIds[], AliveBits.</description></item>
        ///   <item><description>Serialize each component pool using the configured formatter set.</description></item>
        /// </list>
        /// <para>
        /// The stream is not closed by this call; lifetime management remains with the caller.
        /// </para>
        /// </remarks>
        void SaveFullSnapshotBinary(Stream s);

        /// <summary>
        /// Loads a complete snapshot from the given stream and replaces in-memory state.
        /// </summary>
        /// <param name="s">Readable stream that provides the snapshot bytes.</param>
        /// <remarks>
        /// <para>
        /// Implementations should:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>Validate the magic header and version.</description></item>
        ///   <item><description>Restore entity metadata and rebuild alive/free lists.</description></item>
        ///   <item><description>Clear existing component pools before repopulating them.</description></item>
        ///   <item><description>Run any registered post-load migrations after data is restored.</description></item>
        /// </list>
        /// <para>
        /// The stream is not closed by this call; lifetime management remains with the caller.
        /// </para>
        /// </remarks>
        void LoadFullSnapshotBinary(Stream s);
    }
}
