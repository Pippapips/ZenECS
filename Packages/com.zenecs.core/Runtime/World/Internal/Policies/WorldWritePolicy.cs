// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World internals
// File: WorldWritePolicy.cs
// Purpose: Per-world coarse-grained write gating by simulation/presentation phase.
// Key concepts:
//   • WorldWritePhase: Simulation vs frame phases (Input / Sync / View / UI).
//   • Coarse switches: deny-all, structural-only toggle (spawn/despawn/add/remove).
//   • Layering: sits above PermissionHook (fine-grained per-entity/type checks).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Identifies the current logical write phase for a world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a coarse-grained phase indicator used by
    /// <see cref="WorldWritePolicy"/> to decide which kinds of writes are allowed
    /// at a given time in the frame or simulation loop.
    /// </para>
    /// <para>
    /// Engine- or game-specific adapters can map their update loops
    /// (for example fixed update, render update, UI update) to these phases
    /// in a way that best matches their architecture.
    /// </para>
    /// </remarks>
    internal enum WorldWritePhase
    {
        /// <summary>
        /// No active phase is set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Simulation phase (fixed-timestep, physics, core game logic).
        /// </summary>
        Simulation = 1,

        /// <summary>
        /// Per-frame input phase (player input sampling, commands).
        /// </summary>
        FrameInput = 2,

        /// <summary>
        /// Per-frame sync phase (state synchronization, networking, replication).
        /// </summary>
        FrameSync = 3,

        /// <summary>
        /// Per-frame view phase (presentation-layer world writes, if allowed).
        /// </summary>
        FrameView = 4,

        /// <summary>
        /// Per-frame UI phase (UI-driven world writes, if allowed).
        /// </summary>
        FrameUI = 5,
    }

    /// <summary>
    /// Per-world coarse-grained write policy used by the core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This policy controls which kinds of writes are permitted based on the
    /// current <see cref="WorldWritePhase"/>. It is independent from (and sits
    /// above) <c>PermissionHook</c>, which performs fine-grained checks on a
    /// per-entity / per-component-type basis.
    /// </para>
    /// <para>
    /// Typical usage:
    /// <list type="number">
    /// <item><description>At the beginning of a phase, call <see cref="Set"/> with the desired flags.</description></item>
    /// <item><description>In world write paths, query <see cref="CanValueWrite"/> and <see cref="CanStructuralWrite"/>.</description></item>
    /// <item><description>At the end of the frame or when leaving the phase, call <see cref="Clear"/>.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    internal sealed class WorldWritePolicy
    {
        /// <summary>
        /// Gets the current phase under which write decisions are evaluated.
        /// </summary>
        public WorldWritePhase CurrentPhase { get; private set; } = WorldWritePhase.None;

        /// <summary>
        /// Gets a value indicating whether <b>all</b> writes (structural and value)
        /// are currently denied.
        /// </summary>
        /// <remarks>
        /// When this flag is <see langword="true"/>, both
        /// <see cref="CanValueWrite"/> and <see cref="CanStructuralWrite"/> will
        /// return <see langword="false"/>.
        /// </remarks>
        public bool DenyAllWrites { get; private set; }

        /// <summary>
        /// Gets a value indicating whether structural changes are allowed.
        /// </summary>
        /// <remarks>
        /// Structural changes include operations such as spawn/despawn, and
        /// adding or removing components from entities. This flag has no effect
        /// when <see cref="DenyAllWrites"/> is <see langword="true"/>.
        /// </remarks>
        public bool StructuralChangesAllowed { get; private set; }

        /// <summary>
        /// Sets the current write policy for the world.
        /// </summary>
        /// <param name="phase">Logical write phase being entered.</param>
        /// <param name="denyAllWrites">
        /// When <see langword="true"/>, all writes are denied regardless of
        /// <paramref name="structuralChangesAllowed"/>.
        /// </param>
        /// <param name="structuralChangesAllowed">
        /// When <see langword="true"/>, structural writes are permitted (subject
        /// to <paramref name="denyAllWrites"/> and other checks in the world).
        /// </param>
        public void Set(WorldWritePhase phase, bool denyAllWrites, bool structuralChangesAllowed)
        {
            CurrentPhase = phase;
            DenyAllWrites = denyAllWrites;
            StructuralChangesAllowed = structuralChangesAllowed;
        }

        /// <summary>
        /// Resets the policy to its default state.
        /// </summary>
        /// <remarks>
        /// After calling this method:
        /// <list type="bullet">
        /// <item><description><see cref="CurrentPhase"/> is <see cref="WorldWritePhase.None"/>.</description></item>
        /// <item><description><see cref="DenyAllWrites"/> is <see langword="false"/>.</description></item>
        /// <item><description><see cref="StructuralChangesAllowed"/> is <see langword="true"/>.</description></item>
        /// </list>
        /// </remarks>
        public void Clear()
        {
            CurrentPhase = WorldWritePhase.None;
            DenyAllWrites = false;
            StructuralChangesAllowed = true;
        }

        /// <summary>
        /// Determines whether value writes are permitted under the current policy.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if value writes are allowed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Value writes typically include in-place modifications of existing
        /// components (for example <c>Ref&lt;T&gt;</c> setters, <c>Replace&lt;T&gt;</c>
        /// calls at the component level), not structural operations.
        /// </remarks>
        public bool CanValueWrite()
        {
            return !DenyAllWrites;
        }

        /// <summary>
        /// Determines whether structural writes are permitted under the current policy.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if structural writes are allowed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Structural writes include operations like spawning and despawning
        /// entities, or adding and removing components to or from entities.
        /// </remarks>
        public bool CanStructuralWrite()
        {
            return !DenyAllWrites && StructuralChangesAllowed;
        }
    }
}
