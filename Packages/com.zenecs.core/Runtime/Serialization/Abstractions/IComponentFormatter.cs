// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization
// File: IComponentFormatter.cs
// Purpose: Define serializers for component values consumed by snapshot backends.
// Key concepts:
//   • Dual surface: non-generic (boxed) and generic (typed) contracts.
//   • Backend-agnostic: works with any ISnapshotBackend implementation.
//   • Symmetry & tolerance: Write ↔ Read symmetry; version tolerance recommended.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Non-generic formatter contract for component values.
    /// Implementations should be symmetric (Write → Read) and, when feasible,
    /// tolerant to forward/backward version changes.
    /// </summary>
    public interface IComponentFormatter
    {
        /// <summary>The concrete component <see cref="Type"/> handled by this formatter.</summary>
        Type ComponentType { get; }

        /// <summary>
        /// Write a boxed component instance to the snapshot backend.
        /// </summary>
        /// <param name="boxed">Boxed component value.</param>
        /// <param name="backend">Snapshot I/O backend.</param>
        void Write(object boxed, ISnapshotBackend backend);

        /// <summary>
        /// Read a boxed component instance from the snapshot backend.
        /// </summary>
        /// <param name="backend">Snapshot I/O backend.</param>
        /// <returns>Boxed component value.</returns>
        object Read(ISnapshotBackend backend);
    }

    /// <summary>
    /// Strongly-typed formatter that avoids boxing and exposes typed read/write.
    /// </summary>
    /// <typeparam name="T">Component value type (typically a <see langword="struct"/>).</typeparam>
    public interface IComponentFormatter<T> : IComponentFormatter where T : struct
    {
        /// <summary>Write a typed component value to the backend.</summary>
        /// <param name="value">Component value.</param>
        /// <param name="backend">Snapshot I/O backend.</param>
        void Write(in T value, ISnapshotBackend backend);

        /// <summary>Read a typed component value from the backend.</summary>
        /// <param name="backend">Snapshot I/O backend.</param>
        /// <returns>Deserialized component value.</returns>
        T ReadTyped(ISnapshotBackend backend);
    }
}
