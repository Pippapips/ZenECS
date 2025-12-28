// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization (Binary)
// File: BinaryComponentFormatter.cs
// Purpose: Base class for compact binary component formatters.
// Key concepts:
//   • Bridges typed formatter API to boxed IComponentFormatter calls.
//   • Single code path: boxed Read/Write forward to typed implementations.
//   • Versioning: encourage explicit field layout / headers for resilience.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Base class for binary component formatters providing a strongly-typed
    /// serialization API with boxed fallbacks via <see cref="IComponentFormatter"/>.
    /// </summary>
    /// <typeparam name="T">Component value type (struct).</typeparam>
    public abstract class BinaryComponentFormatter<T> : IComponentFormatter<T> where T : struct
    {
        /// <summary>Component <see cref="Type"/> handled by this formatter.</summary>
        public Type ComponentType => typeof(T);

        /// <summary>
        /// Write a typed component to the backend in a compact binary form.
        /// </summary>
        /// <param name="value">Component value.</param>
        /// <param name="backend">Snapshot backend.</param>
        public abstract void Write(in T value, ISnapshotBackend backend);

        /// <summary>
        /// Read a typed component value from the backend.
        /// </summary>
        /// <param name="backend">Snapshot backend.</param>
        /// <returns>Deserialized component value.</returns>
        public abstract T ReadTyped(ISnapshotBackend backend);

        /// <summary>
        /// Boxed write entry point (for <see cref="IComponentFormatter"/>). Forwards to
        /// <see cref="Write(in T, ISnapshotBackend)"/>.
        /// </summary>
        void IComponentFormatter.Write(object boxed, ISnapshotBackend backend)
            => Write((T)boxed, backend);

        /// <summary>
        /// Boxed read entry point (for <see cref="IComponentFormatter"/>). Forwards to
        /// <see cref="ReadTyped(ISnapshotBackend)"/>.
        /// </summary>
        object IComponentFormatter.Read(ISnapshotBackend backend)
            => ReadTyped(backend);
    }
}
