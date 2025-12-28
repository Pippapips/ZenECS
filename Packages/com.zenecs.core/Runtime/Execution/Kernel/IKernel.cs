// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IKernel.cs
// Purpose: Public interface for the Kernel – the adapter-facing contract that
//          exposes multi-world lifecycle, selection, and frame stepping.
// Key concepts:
//   • Adapters call three ticks: BeginFrame → FixedStep×N → LateFrame.
//   • Worlds are discoverable by id/name/tag and one can be selected as current.
//   • Events surface per-world hooks for systems/integration layers.
//   • Options influence creation and stepping (e.g., step-only-current).
//   • Time fields track uptime, counts, and accumulated fixed-step seconds.
// Copyright (c) 2026 Pippapips Limited
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
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adapters (Unity, Godot, custom engines, etc.) create a kernel instance,
    /// start it, create worlds, and drive the three main ticks:
    /// <see cref="BeginFrame(float)"/>, <see cref="FixedStep(float)"/>,
    /// and <see cref="LateFrame(float)"/>.
    /// </para>
    /// <para>
    /// External code should not mutate world state by reaching inside the kernel
    /// implementation; always use the public APIs exposed by <see cref="IWorld"/>.
    /// </para>
    /// </remarks>
    public interface IKernel : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the kernel is currently running.
        /// </summary>
        /// <remarks>
        /// Becomes <see langword="true"/> after construction and remains so until
        /// <see cref="IDisposable.Dispose"/> is called.
        /// </remarks>
        bool IsRunning { get; }

        /// <summary>
        /// Gets a value indicating whether the kernel is paused.
        /// </summary>
        /// <remarks>
        /// When paused, the kernel ignores tick calls (<see cref="BeginFrame(float)"/>,
        /// <see cref="FixedStep(float)"/>, <see cref="LateFrame(float)"/>).
        /// </remarks>
        bool IsPaused { get; }

        /// <summary>
        /// Gets the unconsumed delta time accumulated for fixed stepping (in seconds).
        /// </summary>
        float SimulationAccumulatorSeconds { get; }

        /// <summary>
        /// Gets the total elapsed time (in seconds) that has been passed to
        /// <see cref="BeginFrame(float)"/> while running.
        /// </summary>
        float TotalTimeSeconds { get; }

        /// <summary>
        /// Gets the total number of <see cref="BeginFrame(float)"/> calls processed.
        /// </summary>
        long FrameCount { get; }

        /// <summary>
        /// Gets the total number of <see cref="FixedStep(float)"/> calls processed.
        /// </summary>
        long FixedFrameCount { get; }

        /// <summary>
        /// Gets the 1-based index of the current fixed step within the last frame.
        /// </summary>
        /// <remarks>
        /// This index resets to 0 at the beginning of each frame and is incremented
        /// on every <see cref="FixedStep(float)"/> call during that frame.
        /// </remarks>
        int FixedFrameIndexInFrame { get; }

        /// <summary>
        /// Gets the accumulated simulated time in seconds processed by
        /// <see cref="FixedStep(float)"/> only.
        /// </summary>
        /// <remarks>
        /// This value excludes raw wall-clock deltas and reflects strictly the sum
        /// of fixed-step deltas.
        /// </remarks>
        double TotalSimulatedSeconds { get; }

        /// <summary>
        /// Gets the currently selected world, or <see langword="null"/> if none.
        /// </summary>
        IWorld? CurrentWorld { get; }

        /// <summary>
        /// Gets the behavioral options for world creation and ticking.
        /// </summary>
        KernelOptions? Options { get; }

        /// <summary>
        /// Occurs when a world is created and registered with the kernel.
        /// </summary>
        event Action<IWorld>? WorldCreated;

        /// <summary>
        /// Occurs after a world has been destroyed and deregistered.
        /// </summary>
        event Action<IWorld>? WorldDestroyed;

        /// <summary>
        /// Occurs whenever <see cref="CurrentWorld"/> changes.
        /// </summary>
        /// <remarks>
        /// The first parameter is the previous current world (may be null), and
        /// the second is the new one (may also be null).
        /// </remarks>
        event Action<IWorld?, IWorld?>? CurrentWorldChanged;

        /// <summary>
        /// Occurs when the kernel is disposed.
        /// </summary>
        event Action? Disposed;

        /// <summary>
        /// Creates a new world (and its DI scope).
        /// </summary>
        /// <param name="cfg">Optional world configuration; a default is used when omitted.</param>
        /// <param name="name">Optional display name; auto-generated when omitted.</param>
        /// <param name="tags">Optional tags for discovery and grouping.</param>
        /// <param name="presetId">
        /// Optional preassigned id; otherwise generated via <see cref="KernelOptions.NewWorldId"/>.
        /// </param>
        /// <param name="setAsCurrent">
        /// If <see langword="true"/>, the created world is immediately selected as
        /// <see cref="CurrentWorld"/>.
        /// </param>
        /// <returns>The created world instance.</returns>
        IWorld CreateWorld(
            WorldConfig? cfg = null,
            string? name = null,
            IEnumerable<string>? tags = null,
            WorldId? presetId = null,
            bool setAsCurrent = false);

        /// <summary>
        /// Destroys a previously created world.
        /// </summary>
        /// <param name="world">World instance to destroy.</param>
        /// <remarks>
        /// This method is a no-op if the world is not registered in the kernel.
        /// </remarks>
        void DestroyWorld(IWorld world);

        /// <summary>
        /// Returns a snapshot enumerable of all registered worlds.
        /// </summary>
        IEnumerable<IWorld> GetAllWorld();

        /// <summary>
        /// Attempts to get a live world by id.
        /// </summary>
        /// <param name="id">World identifier.</param>
        /// <param name="world">
        /// When this method returns, contains the world if found; otherwise
        /// <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a matching world was found; otherwise <see langword="false"/>.
        /// </returns>
        bool TryGet(WorldId id, out IWorld? world);

        /// <summary>
        /// Finds worlds by exact name.
        /// </summary>
        /// <param name="name">Name to match.</param>
        /// <returns>
        /// An enumerable of worlds whose <see cref="IWorld.Name"/> equals <paramref name="name"/>.
        /// Multiple worlds may share the same name.
        /// </returns>
        IEnumerable<IWorld> FindByName(string name);

        /// <summary>
        /// Finds worlds that contain the given tag.
        /// </summary>
        /// <param name="tag">Tag to search for.</param>
        /// <returns>Worlds that have <paramref name="tag"/> in their tag list.</returns>
        IEnumerable<IWorld> FindByTag(string tag);

        /// <summary>
        /// Returns worlds that match any of the provided tags (case-insensitive).
        /// </summary>
        /// <param name="tags">Tags to match against.</param>
        IEnumerable<IWorld> FindByAnyTag(params string[] tags);

        /// <summary>
        /// Finds worlds whose names start with the given prefix (case-insensitive).
        /// </summary>
        /// <param name="prefix">Name prefix to match.</param>
        IEnumerable<IWorld> FindByNamePrefix(string prefix);

        /// <summary>
        /// Sets the current world by instance.
        /// </summary>
        /// <param name="world">World to mark as current.</param>
        /// <remarks>
        /// Internally wraps the world into a <see cref="WorldHandle"/> and resolves
        /// it again to ensure a consistent code path.
        /// </remarks>
        void SetCurrentWorld(IWorld world);

        /// <summary>
        /// Sets the current world using a safe handle that resolves or throws.
        /// </summary>
        /// <param name="handle">World handle that resolves to an <see cref="IWorld"/>.</param>
        void SetCurrentWorld(WorldHandle handle);

        /// <summary>
        /// Clears the current selection so that there is no current world.
        /// </summary>
        void ClearCurrentWorld();

        /// <summary>
        /// Advances variable-rate frame logic for all eligible worlds.
        /// </summary>
        /// <param name="dt">Delta time for this frame (seconds). Negative values are clamped to 0.</param>
        void BeginFrame(float dt);

        /// <summary>
        /// Advances fixed-rate simulation logic for all eligible worlds.
        /// </summary>
        /// <param name="fixedDelta">Fixed step duration (seconds).</param>
        void FixedStep(float fixedDelta);

        /// <summary>
        /// Advances presentation phase and interpolation for all eligible worlds.
        /// </summary>
        /// <param name="alpha">Blend factor in [0,1] derived from accumulator/fixedDelta.</param>
        void LateFrame(float alpha = 1.0f);

        /// <summary>
        /// Performs a combined step:
        /// <see cref="BeginFrame(float)"/>, zero or more <see cref="FixedStep(float)"/> calls,
        /// and finally <see cref="LateFrame(float)"/>.
        /// </summary>
        /// <param name="dt">Delta time to add to the accumulator (seconds).</param>
        /// <param name="fixedDelta">Fixed step duration (seconds).</param>
        /// <param name="maxSubSteps">Maximum number of fixed substeps to process this frame.</param>
        /// <returns>The number of fixed steps executed.</returns>
        int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps);

        /// <summary>
        /// Pauses all ticking.
        /// </summary>
        /// <remarks>
        /// While paused, <see cref="BeginFrame(float)"/>, <see cref="FixedStep(float)"/>,
        /// and <see cref="LateFrame(float)"/> calls are ignored.
        /// </remarks>
        void Pause();

        /// <summary>
        /// Resumes ticking after a previous call to <see cref="Pause"/>.
        /// </summary>
        void Resume();

        /// <summary>
        /// Toggles the paused state.
        /// </summary>
        void TogglePause();
    }
}