// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Write Phase facade)
// File: WorldWritePhase.cs
// Purpose: Expose the current write phase and configure write policy
//          (deny/allow writes and structural changes) for the world.
// Key concepts:
//   • Centralized write-phase tracking via WorldWritePolicy.
//   • Phase-aware guards in write APIs (components/entities).
//   • Clear() helper to return to default/unrestricted state.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Partial <c>World</c> implementation exposing the write-phase API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This façade wraps an internal <see cref="WorldWritePolicy"/> instance
    /// and is used by component/entity APIs to decide whether a given write
    /// is permitted in the current phase.
    /// </para>
    /// <para>
    /// Typical usage is to bracket critical sections of code (for example,
    /// system execution) with <c>SetWritePhase</c> calls to either allow or
    /// restrict structural changes and component writes.
    /// </para>
    /// </remarks>
    internal sealed partial class World
    {
        private readonly WorldWritePolicy _writePolicy = new WorldWritePolicy();

        /// <summary>
        /// Gets the current write phase for this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is used by write helpers and APIs to make phase-aware decisions,
        /// such as throwing or logging when writes occur in a read-only phase.
        /// </para>
        /// </remarks>
        internal WorldWritePhase CurrentWritePhase => _writePolicy.CurrentPhase;

        /// <summary>
        /// Sets the active write phase and associated policy flags.
        /// </summary>
        /// <param name="phase">
        /// Logical phase value describing the current execution context
        /// (for example, <c>SystemUpdate</c>, <c>Presentation</c>, etc.).
        /// </param>
        /// <param name="denyAllWrites">
        /// When <see langword="true"/>, all component writes are rejected while
        /// this phase is active.
        /// </param>
        /// <param name="structuralChangesAllowed">
        /// When <see langword="true"/>, structural changes such as entity
        /// creation/destruction and add/remove component operations are allowed
        /// during this phase.
        /// </param>
        /// <remarks>
        /// <para>
        /// Callers typically ensure that <c>SetWritePhase</c> is paired with
        /// <see cref="ClearWritePhase"/> once they leave the corresponding
        /// execution region.
        /// </para>
        /// </remarks>
        internal void SetWritePhase(
            WorldWritePhase phase,
            bool denyAllWrites,
            bool structuralChangesAllowed)
        {
            _writePolicy.Set(phase, denyAllWrites, structuralChangesAllowed);
        }

        /// <summary>
        /// Clears the current write phase and returns to the default policy.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After calling this method, the world is considered to be in a neutral
        /// phase with no explicit restrictions, and base write guards (if any)
        /// are applied.
        /// </para>
        /// </remarks>
        internal void ClearWritePhase()
        {
            _writePolicy.Clear();
        }
    }
}
