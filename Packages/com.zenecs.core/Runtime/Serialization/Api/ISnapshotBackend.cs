// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization
// File: ISnapshotBackend.cs
// Purpose: Abstract I/O surface for snapshot read/write operations.
// Key concepts:
//   • Primitive & block operations: bytes, numbers, strings, booleans.
//   • Cursor control: Position, Length, Rewind.
//   • Pluggable backends: streams, memory buffers, custom stores.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Backend-agnostic I/O interface used by snapshot serializers.
    /// Implementations must be deterministic and consistent about endianness.
    /// </summary>
    public interface ISnapshotBackend : IDisposable
    {
        // ---- Raw bytes -------------------------------------------------------

        /// <summary>Write a contiguous span of bytes.</summary>
        void WriteBytes(ReadOnlySpan<byte> data);

        /// <summary>Read exactly <paramref name="length"/> bytes into <paramref name="dst"/>.</summary>
        void ReadBytes(Span<byte> dst, int length);

        // ---- Primitives ------------------------------------------------------

        /// <summary>Write a 32-bit signed integer.</summary>
        void WriteInt(int v);

        /// <summary>Read a 32-bit signed integer.</summary>
        int ReadInt();

        /// <summary>Write a 32-bit unsigned integer.</summary>
        void WriteUInt(uint v);

        /// <summary>Read a 32-bit unsigned integer.</summary>
        uint ReadUInt();

        /// <summary>Write a 32-bit floating-point value.</summary>
        void WriteFloat(float v);

        /// <summary>Read a 32-bit floating-point value.</summary>
        float ReadFloat();

        /// <summary>Write a UTF-8 string (implementation-defined length encoding).</summary>
        void WriteString(string s);

        /// <summary>Read a UTF-8 string previously written by <see cref="WriteString"/>.</summary>
        string ReadString();

        /// <summary>Write a Boolean value.</summary>
        void WriteBool(bool v);

        /// <summary>Read a Boolean value.</summary>
        bool ReadBool();

        // ---- Cursor & length -------------------------------------------------

        /// <summary>Current cursor position.</summary>
        long Position { get; set; }

        /// <summary>Reset the cursor to the beginning.</summary>
        void Rewind();

        /// <summary>Total readable length (when applicable).</summary>
        long Length { get; }
    }
}
