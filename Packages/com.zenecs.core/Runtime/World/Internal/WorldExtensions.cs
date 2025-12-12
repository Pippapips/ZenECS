// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World Extension Methods
// File: WorldExtensions.cs
// Purpose: Extension methods for IWorld to safely access internal World methods
//          without explicit casting. Improves type safety and code readability.
// Key concepts:
//   • Type-safe casting: Uses pattern matching instead of 'as' operator
//   • Centralized casting logic: All World casting logic in one place
//   • Internal API access: Provides safe access to World internal methods
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Extension methods for <see cref="IWorld"/> to safely access internal
    /// <see cref="World"/> methods without explicit casting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods encapsulate the pattern matching logic for
    /// safely casting <see cref="IWorld"/> to <see cref="World"/>, improving
    /// type safety and code readability throughout the codebase.
    /// </para>
    /// </remarks>
    internal static class WorldExtensions
    {
        /// <summary>
        /// Safely invokes <see cref="World.BeginFrame"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance to step.</param>
        /// <param name="self">The world instance to pass to BeginFrame (typically same as world).</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        internal static void BeginFrameInternal(this IWorld? world, IWorld self, float dt)
        {
            if (world is World internalWorld)
            {
                internalWorld.BeginFrame(self, dt);
            }
        }

        /// <summary>
        /// Safely invokes <see cref="World.FixedStep"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance to step.</param>
        /// <param name="self">The world instance to pass to FixedStep (typically same as world).</param>
        /// <param name="fixedDelta">Fixed step duration in seconds.</param>
        internal static void FixedStepInternal(this IWorld? world, IWorld self, float fixedDelta)
        {
            if (world is World internalWorld)
            {
                internalWorld.FixedStep(self, fixedDelta);
            }
        }

        /// <summary>
        /// Safely invokes <see cref="World.LateFrame"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance to step.</param>
        /// <param name="self">The world instance to pass to LateFrame (typically same as world).</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        /// <param name="alpha">Interpolation factor in [0,1].</param>
        internal static void LateFrameInternal(this IWorld? world, IWorld self, float dt, float alpha)
        {
            if (world is World internalWorld)
            {
                internalWorld.LateFrame(self, dt, alpha);
            }
        }

        /// <summary>
        /// Safely invokes <see cref="World.SetWritePhase"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance.</param>
        /// <param name="phase">The write phase to set.</param>
        /// <param name="denyAllWrites">Whether to deny all writes during this phase.</param>
        /// <param name="structuralChangesAllowed">Whether structural changes are allowed.</param>
        internal static void SetWritePhaseInternal(
            this IWorld? world,
            WorldWritePhase phase,
            bool denyAllWrites,
            bool structuralChangesAllowed)
        {
            if (world is World internalWorld)
            {
                internalWorld.SetWritePhase(phase, denyAllWrites, structuralChangesAllowed);
            }
        }

        /// <summary>
        /// Safely invokes <see cref="World.ExternalCommandFlushTo"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance.</param>
        internal static void ExternalCommandFlushToInternal(this IWorld? world)
        {
            if (world is World internalWorld)
            {
                internalWorld.ExternalCommandFlushTo();
            }
        }

        /// <summary>
        /// Safely invokes <see cref="World.ClearWritePhase"/> if the world is a
        /// <see cref="World"/> instance.
        /// </summary>
        /// <param name="world">The world instance.</param>
        internal static void ClearWritePhaseInternal(this IWorld? world)
        {
            if (world is World internalWorld)
            {
                internalWorld.ClearWritePhase();
            }
        }
    }

    /// <summary>
    /// Test framework extensions for setting WritePhase in tests.
    /// </summary>
    public static class WorldTestExtensions
    {
        /// <summary>
        /// Sets the write phase to Simulation for testing purposes. This allows test code to
        /// configure WritePhase without accessing internal APIs.
        /// </summary>
        /// <param name="world">The world instance.</param>
        /// <param name="denyAllWrites">Whether to deny all writes during this phase.</param>
        /// <param name="structuralChangesAllowed">Whether structural changes are allowed.</param>
        public static void SetWritePhaseForTest(
            this IWorld world,
            bool denyAllWrites = false,
            bool structuralChangesAllowed = true)
        {
            // WorldWritePhase.Simulation = 1 (internal enum, so we use the value directly)
            var simulationPhase = (WorldWritePhase)1;
            WorldExtensions.SetWritePhaseInternal(world, simulationPhase, denyAllWrites, structuralChangesAllowed);
        }

        /// <summary>
        /// Clears the write phase for testing purposes.
        /// </summary>
        /// <param name="world">The world instance.</param>
        public static void ClearWritePhaseForTest(this IWorld world)
        {
            WorldExtensions.ClearWritePhaseInternal(world);
        }
    }
}

