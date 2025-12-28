// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemGroupAttributes.cs
// Purpose: Attribute helpers for assigning systems to high-level SystemGroup
//          categories (fixed-step and frame-step execution groups).
// Key concepts:
//   • GroupAttribute is the low-level base used by planners/runners.
//   • Fixed* group attributes for deterministic simulation pipeline.
//   • Frame* group attributes for non-deterministic, view/UI oriented pipeline.
//   • Exactly one group attribute per system class (non-inherited, non-multiple).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Declares which high-level <see cref="SystemGroup"/> a system belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the generic, low-level attribute understood by system planners.
    /// New code is encouraged to use one of the concrete group attributes:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="FixedInputGroupAttribute"/></description></item>
    ///   <item><description><see cref="FixedDecisionGroupAttribute"/></description></item>
    ///   <item><description><see cref="FixedGroupAttribute"/></description></item>
    ///   <item><description><see cref="FixedPostGroupAttribute"/></description></item>
    ///   <item><description><see cref="FrameInputGroupAttribute"/></description></item>
    ///   <item><description><see cref="FrameSyncGroupAttribute"/></description></item>
    ///   <item><description><see cref="FrameViewGroupAttribute"/></description></item>
    ///   <item><description><see cref="FrameUIGroupAttribute"/></description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class GroupAttribute : Attribute
    {
        /// <summary>
        /// Gets the system group this attribute assigns.
        /// </summary>
        public SystemGroup Group { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupAttribute"/> class
        /// with the specified <paramref name="group"/>.
        /// </summary>
        /// <param name="group">Logical execution group the system belongs to.</param>
        protected GroupAttribute(SystemGroup group)
        {
            Group = group;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fixed-step deterministic groups
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedInput"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>Fixed-step input ingestion (movement input, network commands).</description></item>
    ///   <item><description>
    /// Converting raw device/network/UI input into deterministic components or messages
    /// that later <see cref="SystemGroup.FixedDecision"/> systems will consume.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedInputGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInputGroupAttribute"/> class.
        /// </summary>
        public FixedInputGroupAttribute()
            : base(SystemGroup.FixedInput)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedDecision"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>Heading/forward vector calculation.</description></item>
    ///   <item><description>AI decision making (targets, skills, paths).</description></item>
    ///   <item><description>
    /// Generating deterministic commands (e.g. fire projectile, dash, cast spell)
    /// that the simulation stage will apply.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedDecisionGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedDecisionGroupAttribute"/> class.
        /// </summary>
        public FixedDecisionGroupAttribute()
            : base(SystemGroup.FixedDecision)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedSimulation"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>Movement and collision resolution.</description></item>
    ///   <item><description>Projectiles and physics-like deterministic updates.</description></item>
    ///   <item><description>
    /// Core gameplay state changes (health, buffs/debuffs, timers, resource updates).
    ///   </description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedGroupAttribute"/> class.
        /// </summary>
        public FixedGroupAttribute()
            : base(SystemGroup.FixedSimulation)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedPost"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>Damage resolution and death tagging.</description></item>
    ///   <item><description>Respawn scheduling and score/XP accumulation.</description></item>
    ///   <item><description>End-of-tick cleanup and logging.</description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedPostGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedPostGroupAttribute"/> class.
        /// </summary>
        public FixedPostGroupAttribute()
            : base(SystemGroup.FixedPost)
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Variable-step frame groups (non-deterministic, view-oriented)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameInput"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>
    /// Polling Unity/engine devices, UI, or other non-deterministic sources.
    ///   </description></item>
    ///   <item><description>
    /// Writing transient "view-side input" into ECS (for example,
    /// <c>RawInputBuffer</c>, <c>ViewClickEvent</c>) that the deterministic core
    /// will later consume.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameInputGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameInputGroupAttribute"/> class.
        /// </summary>
        public FrameInputGroupAttribute()
            : base(SystemGroup.FrameInput)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameSync"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>Camera follow/orbit logic.</description></item>
    ///   <item><description>Client-side prediction for visuals only.</description></item>
    ///   <item><description>
    /// Other view-only logic that derives camera/viewport state from the
    /// deterministic core without mutating it.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameSyncGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameSyncGroupAttribute"/> class.
        /// </summary>
        public FrameSyncGroupAttribute()
            : base(SystemGroup.FrameSync)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameView"/> group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This stage is typically used for interpolation, transform updates, animation,
    /// and view binding that read from simulation state but do not change it.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameViewGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameViewGroupAttribute"/> class.
        /// </summary>
        public FrameViewGroupAttribute()
            : base(SystemGroup.FrameView)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameUI"/> group.
    /// </summary>
    /// <remarks>
    /// <para>Typical usage:</para>
    /// <list type="bullet">
    ///   <item><description>HUD and UI widgets (HP bars, ammo counters, minimaps).</description></item>
    ///   <item><description>Debug overlays and editor-only visualizations.</description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameUIGroupAttribute : GroupAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameUIGroupAttribute"/> class.
        /// </summary>
        public FrameUIGroupAttribute()
            : base(SystemGroup.FrameUI)
        {
        }
    }
}
