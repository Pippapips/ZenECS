// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IKernel.cs
// Purpose: Public interface for the Kernel – the adapter‑facing contract that
//          exposes multi‑world lifecycle, selection, and frame stepping.
// Key concepts:
//   • Adapters call three ticks: BeginFrame → FixedStep×N → LateFrame.
//   • Worlds are discoverable by id/name/tag and one can be selected as current.
//   • Events surface per‑world hooks for systems/integration layers.
//   • Options influence creation and stepping (e.g., step‑only‑current).
//   • Time fields track uptime, counts, and accumulated fixed‑step seconds.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>
    /// Kernel lifecycle and multi-world management interface.
    /// Adapters (Unity/Godot/…) create a Kernel, start it, create Worlds, and drive the ticks.
    /// External code should not mutate world state via Kernel internals; use the world's APIs instead.
    /// </summary>
    public interface IKernel : IDisposable
    {
        /// <summary>True after construction until <see cref="IDisposable.Dispose"/> is called.</summary>
        bool IsRunning { get; }

        /// <summary>Whether the kernel is paused (ticks are ignored while paused).</summary>
        bool IsPaused { get; }

        /// <summary>Unconsumed delta time accumulated for fixed stepping (seconds).</summary>
        float SimulationAccumulatorSeconds { get; }

        /// <summary>Total elapsed time passed to <see cref="BeginFrame(float)"/> while running (seconds).</summary>
        float TotalTimeSeconds { get; }

        /// <summary>Total number of <see cref="BeginFrame(float)"/> calls processed while running.</summary>
        long FrameCount { get; }

        /// <summary>Total number of <see cref="FixedStep(float)"/> calls processed while running.</summary>
        long FixedFrameCount { get; }

        /// <summary>1‑based index of the current fixed step within the last frame (resets each frame).</summary>
        int FixedFrameIndexInFrame { get; }

        /// <summary>
        /// Accumulated simulated time in seconds processed by <see cref="FixedStep(float)"/> only.
        /// This excludes raw wall‑clock deltas and reflects strictly the sum of fixed‑step deltas.
        /// </summary>
        double TotalSimulatedSeconds { get; }

        /// <summary>The currently selected world, or null if none.</summary>
        IWorld? CurrentWorld { get; }

        /// <summary>Behavioral options for world creation and ticking.</summary>
        KernelOptions? Options { get; }

        /// <summary>Raised when a world is created and registered.</summary>
        event Action<IWorld>? WorldCreated;

        /// <summary>Raised after a world has been destroyed and deregistered.</summary>
        event Action<IWorld>? WorldDestroyed;

        /// <summary>Raised whenever <see cref="CurrentWorld"/> changes (argument may be null).</summary>
        event Action<IWorld?>? CurrentWorldChanged;

        /// <summary>
        /// Create a new world (and its DI scope).
        /// If <paramref name="setAsCurrent"/> is true, the kernel also sets it as <see cref="CurrentWorld"/>.
        /// </summary>
        /// <param name="cfg">Optional world configuration; a default is used when omitted.</param>
        /// <param name="name">Optional display name; auto‑generated when omitted.</param>
        /// <param name="tags">Optional tags for discovery/grouping.</param>
        /// <param name="presetId">Optional preassigned id; otherwise generated via <see cref="KernelOptions.NewWorldId"/>.</param>
        /// <param name="setAsCurrent">If true, immediately select the created world as current.</param>
        /// <returns>The created world instance.</returns>
        IWorld CreateWorld(WorldConfig? cfg = null, string? name = null, IEnumerable<string>? tags = null, WorldId? presetId = null,
            bool setAsCurrent = false);

        /// <summary>Destroy a previously created world. No‑op if the world is not registered.</summary>
        void DestroyWorld(IWorld world);

        IEnumerable<IWorld> GetAllWorld();
        
        /// <summary>Try to get a live world by id.</summary>
        /// <param name="id">The world identifier.</param>
        /// <param name="world">When this method returns, contains the world if found.</param>
        /// <returns>true if found; otherwise false.</returns>
        bool TryGet(WorldId id, out IWorld world);

        /// <summary>Find worlds by exact name (multiple worlds may share a name).</summary>
        IEnumerable<IWorld> FindByName(string name);

        /// <summary>Find worlds that contain the given tag.</summary>
        IEnumerable<IWorld> FindByTag(string tag);

        /// <summary>Return worlds that match any of the provided tags (case‑insensitive).</summary>
        IEnumerable<IWorld> FindByAnyTag(params string[] tags);

        /// <summary>Find worlds whose names start with the given prefix (case‑insensitive).</summary>
        IEnumerable<IWorld> FindByNamePrefix(string prefix);

        /// <summary>Set the current world by instance (wraps into a safe handle).</summary>
        void SetCurrentWorld(IWorld world);

        /// <summary>Set the current world using a safe handle that resolves or throws.</summary>
        void SetCurrentWorld(WorldHandle handle);

        /// <summary>Clear the current selection so that there is no current world.</summary>
        void ClearCurrentWorld();

        /// <summary>Advance variable‑rate frame logic for all eligible worlds.</summary>
        /// <param name="dt">Delta time for this frame (seconds). Negative values are clamped to 0.</param>
        void BeginFrame(float dt);

        /// <summary>Advance fixed‑rate simulation logic for all eligible worlds.</summary>
        /// <param name="fixedDelta">Fixed step duration (seconds).</param>
        void FixedStep(float fixedDelta);

        /// <summary>Advance presentation phase and interpolation for all eligible worlds.</summary>
        /// <param name="alpha">Blend factor in [0,1] from accumulator/fixedDelta.</param>
        void LateFrame(float alpha = 1.0f);

        /// <summary>Hook fired at the beginning of a frame for each stepped world.</summary>
        event Action<IWorld, float>? OnBeginFrame;

        /// <summary>Hook fired for each fixed step on each stepped world.</summary>
        event Action<IWorld, float>? OnFixedStep;

        /// <summary>Hook fired at the end of the frame for each stepped world (delta &amp; alpha provided).</summary>
        event Action<IWorld, float, float>? OnLateFrame;

        /// <summary>
        /// Convenience helper that performs BeginFrame, then consumes the accumulator into up to
        /// <paramref name="maxSubSteps"/> FixedStep calls/>, and finally calls LateFrame.
        /// Returns the number of FixedStep calls executed.
        /// </summary>
        /// <param name="dt">Delta time to add to the accumulator (seconds).</param>
        /// <param name="fixedDelta">Fixed step duration (seconds).</param>
        /// <param name="maxSubSteps">Maximum number of fixed substeps to process this frame.</param>
        /// <returns>The number of fixed steps executed.</returns>
        int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps);

        /// <summary>Pause all ticking (Begin/Fixed/Late are ignored while paused).</summary>
        void Pause();

        /// <summary>Resume ticking after <see cref="Pause"/>.</summary>
        void Resume();

        /// <summary>Toggle the paused state.</summary>
        void TogglePause();
    }
}
