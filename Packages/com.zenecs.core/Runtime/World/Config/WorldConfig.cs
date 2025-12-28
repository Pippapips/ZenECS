// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: WorldConfig.cs
// Purpose: Configure initial capacities and growth policies for entities,
//          component pools, and binder registries.
// Key concepts:
//   • Initial capacities: size arrays and pools at world creation.
//   • Growth policy: Doubling vs Step with configurable step size.
//   • Performance tuning: control rehash frequency and free-id stack capacity.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Controls how arrays and pools expand once capacity is exceeded.
    /// </summary>
    public enum GrowthPolicy
    {
        /// <summary>
        /// Double capacity on expansion (guaranteeing at least +256).
        /// Reduces resize frequency at the cost of larger jumps in memory usage.
        /// </summary>
        Doubling,

        /// <summary>
        /// Expand capacity by a fixed number of slots
        /// (<see cref="WorldConfig.GrowthStep"/>) on each expansion.
        /// Provides predictable memory growth.
        /// </summary>
        Step
    }

    /// <summary>
    /// Immutable configuration for world storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used at world construction time to size entity arrays, component pools,
    /// and binder registries, and to define growth behavior when capacity is
    /// exceeded.
    /// </para>
    /// <para>
    /// Values are clamped to sensible minimums to avoid degenerate allocations
    /// (for example, at least 16 entity slots, 16 pool buckets, etc.).
    /// </para>
    /// </remarks>
    public readonly struct WorldConfig
    {
        /// <summary>
        /// Initial entity slot count (sizes <c>Alive</c>/<c>Generation</c> arrays).
        /// </summary>
        public readonly int InitialEntityCapacity;

        /// <summary>
        /// Initial bucket count for the component-pool dictionary (hash table).
        /// Higher values reduce collisions and rehash frequency.
        /// </summary>
        public readonly int InitialPoolBuckets;

        /// <summary>
        /// Initial bucket count for binder registries.
        /// </summary>
        public readonly int InitialBinderBuckets;

        /// <summary>
        /// Initial per-entity binder-bucket count.
        /// </summary>
        public readonly int InitialBinderPerEntityBuckets;

        /// <summary>
        /// Initial capacity of the free-id stack used to recycle entity IDs.
        /// Increase if create/destroy churn is expected to be high.
        /// </summary>
        public readonly int InitialFreeIdCapacity;

        /// <summary>
        /// Array/pool expansion policy when capacity is exceeded.
        /// </summary>
        public readonly GrowthPolicy GrowthPolicy;

        /// <summary>
        /// Number of slots added per expansion when using
        /// <see cref="Core.GrowthPolicy.Step"/>.
        /// </summary>
        public readonly int GrowthStep;

        /// <summary>
        /// Create a new world configuration with sensible bounds.
        /// </summary>
        /// <param name="initialEntityCapacity">
        /// Entity slots; clamped to ≥ 16.
        /// </param>
        /// <param name="initialPoolBuckets">
        /// Pool dictionary buckets; clamped to ≥ 16.
        /// </param>
        /// <param name="initialBinderBuckets">
        /// Binder registry buckets; clamped to ≥ 16.
        /// </param>
        /// <param name="initialBinderPerEntityBuckets">
        /// Per-entity binder buckets; clamped to ≥ 4.
        /// </param>
        /// <param name="initialFreeIdCapacity">
        /// Free-id stack capacity; clamped to ≥ 16.
        /// </param>
        /// <param name="growthPolicy">
        /// Resize strategy:
        /// <see cref="Core.GrowthPolicy.Doubling"/> or
        /// <see cref="Core.GrowthPolicy.Step"/>.
        /// </param>
        /// <param name="growthStep">
        /// Slots added per expansion when using Step; clamped to ≥ 32.
        /// </param>
        public WorldConfig(
            int initialEntityCapacity = 256,
            int initialPoolBuckets = 256,
            int initialBinderBuckets = 1024,
            int initialBinderPerEntityBuckets = 4,
            int initialFreeIdCapacity = 128,
            GrowthPolicy growthPolicy = GrowthPolicy.Doubling,
            int growthStep = 256)
        {
            InitialEntityCapacity = Math.Max(16, initialEntityCapacity);
            InitialPoolBuckets = Math.Max(16, initialPoolBuckets);
            InitialBinderBuckets = Math.Max(16, initialBinderBuckets);
            InitialBinderPerEntityBuckets = Math.Max(4, initialBinderPerEntityBuckets);
            InitialFreeIdCapacity = Math.Max(16, initialFreeIdCapacity);
            GrowthPolicy = growthPolicy;
            GrowthStep = Math.Max(32, growthStep);
        }
    }
}
