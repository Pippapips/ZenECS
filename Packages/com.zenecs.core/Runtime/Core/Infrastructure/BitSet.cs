// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: Internal/BitSet.cs
// Purpose: Dense bitset used for liveness and component presence tracking.
// Key concepts:
//   • Grow-on-demand, fast Set/Clear/Test operations.
//   • Backs entity alive flags and per-pool presence maps.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Infrastructure.Internal
{
    /// <summary>
    /// A compact bitset backed by a <see cref="uint"/> word array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each index represents a single Boolean flag; the storage is expanded on demand.
    /// Designed for hot-path checks (presence/liveness) with O(1) <see cref="Get"/>/<see cref="Set"/> operations.
    /// </para>
    /// <para>
    /// Word size is 32 bits. <see cref="Length"/> reports the number of addressable bits,
    /// i.e. <c>_words.Length * 32</c>. <see cref="EnsureCapacity"/> grows the internal storage
    /// and preserves all existing bits.
    /// </para>
    /// <para>
    /// This type is not thread-safe; external synchronization is required if mutated
    /// from multiple threads.
    /// </para>
    /// </remarks>
    internal sealed class BitSet
    {
        /// <summary>
        /// Backing storage. Each element stores 32 flags
        /// (least-significant bit is <c>index % 32</c>).
        /// </summary>
        private uint[] _words;

        /// <summary>
        /// Gets the number of addressable bits (capacity in bits).
        /// </summary>
        public int Length => _words.Length << 5; // * 32

        /// <summary>
        /// Initializes a new instance of the <see cref="BitSet"/> class
        /// with the specified capacity in bits.
        /// </summary>
        /// <param name="capacityBits">
        /// Requested capacity (in bits). Values &lt;= 0 will still allocate
        /// a minimal backing array.
        /// </param>
        public BitSet(int capacityBits)
        {
            var words = Math.Max(1, (capacityBits + 31) >> 5); // ceil(capacityBits / 32)
            _words = new uint[words];
        }

        /// <summary>
        /// Ensures the bitset can address at least <paramref name="capacityBits"/> bits.
        /// </summary>
        /// <param name="capacityBits">Minimum required capacity in bits.</param>
        /// <remarks>
        /// Expands the underlying word array and preserves all existing bits.
        /// </remarks>
        public void EnsureCapacity(int capacityBits)
        {
            int needWords = (capacityBits + 31) >> 5; // ceil(bits / 32)
            if (needWords <= _words.Length) return;

            var old = _words;
            var nw = new uint[needWords];
            // Preserve all previously set flags.
            Array.Copy(old, nw, old.Length);
            _words = nw;
        }

        /// <summary>
        /// Clears all bits (sets every flag to <see langword="false" />).
        /// </summary>
        public void ClearAll()
        {
            Array.Clear(_words, 0, _words.Length);
        }

        /// <summary>
        /// Reads the flag at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Bit index (0-based).</param>
        /// <returns>
        /// <see langword="true" /> if the bit is set; otherwise, <see langword="false" />.
        /// Returns <see langword="false" /> for out-of-range indices.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            int w = index >> 5; // word = index / 32
            int b = index & 31; // bit  = index % 32
            if (w >= _words.Length) return false;
            return (_words[w] & (1u << b)) != 0;
        }

        /// <summary>
        /// Sets or clears the bit at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Bit index (0-based).</param>
        /// <param name="value">
        /// <see langword="true" /> to set the bit; <see langword="false" /> to clear it.
        /// </param>
        /// <remarks>
        /// Ensures sufficient capacity (may allocate) before writing.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            int w = index >> 5;
            int b = index & 31;
            EnsureCapacity(index + 1); // ensure index is addressable

            if (value)
            {
                // Set bit.
                _words[w] |= (1u << b);
            }
            else
            {
                // Clear bit.
                _words[w] &= ~(1u << b);
            }
        }

        // ---------------------------------------------------------------------
        // Snapshot serialization / deserialization (based on uint[] storage)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Serializes the internal words into a byte array in native endianness.
        /// </summary>
        /// <returns>
        /// A byte array containing a shallow copy of the word storage.
        /// </returns>
        /// <remarks>
        /// The total number of bits can be inferred by
        /// <c>bytes.Length / sizeof(uint) * 32</c>. Consumers may store the
        /// logical length separately if needed.
        /// </remarks>
        public byte[] ToByteArray()
        {
            int len = _words.Length * sizeof(uint);
            var bytes = new byte[len];
            if (len > 0)
                Buffer.BlockCopy(_words, 0, bytes, 0, len);
            return bytes;
        }

        /// <summary>
        /// Restores the bitset from a byte array produced by <see cref="ToByteArray"/>.
        /// </summary>
        /// <param name="bytes">
        /// Source byte array. <see langword="null" /> or empty clears the storage.
        /// </param>
        public void FromByteArray(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                _words = Array.Empty<uint>();
                return;
            }

            int count = bytes.Length / sizeof(uint);
            _words = new uint[count];
            Buffer.BlockCopy(bytes, 0, _words, 0, count * sizeof(uint));
        }
    }
}
