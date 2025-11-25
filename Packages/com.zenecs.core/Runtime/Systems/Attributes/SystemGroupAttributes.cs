#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Declares which high-level <see cref="SystemGroup"/> a system belongs to.
    /// <para>
    /// Recommended usage:
    ///   • For new code, use specific group attributes such as
    ///     <see cref="FixedInputGroupAttribute"/>,
    ///     <see cref="FixedDecisionGroupAttribute"/>,
    ///     <see cref="FixedGroupAttribute"/>,
    ///     <see cref="FixedPostGroupAttribute"/>,
    ///     <see cref="FrameInputGroupAttribute"/>,
    ///     <see cref="FrameSyncGroupAttribute"/>,
    ///     <see cref="FrameViewGroupAttribute"/>.
    ///     <see cref="FrameUIGroupAttribute"/>,
    ///   • This attribute is the generic, low-level form used by the planner.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class GroupAttribute : Attribute
    {
        /// <summary>
        /// The group this system belongs to.
        /// </summary>
        public SystemGroup Group { get; }

        /// <summary>
        /// Creates a new group attribute with the specified <see cref="SystemGroup"/>.
        /// </summary>
        public GroupAttribute(SystemGroup group)
        {
            Group = group;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fixed-step deterministic groups
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedInput"/> group.
    /// <para>
    /// Typical usage:
    ///   • Fixed-step input ingestion (MoveInput2D, network input snapshot, etc.)
    ///   • Converting raw input buffers into deterministic components/messages
    ///     that later FixedDecision systems will consume.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedInputGroupAttribute : GroupAttribute
    {
        public FixedInputGroupAttribute()
            : base(SystemGroup.FixedInput)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedDecision"/> group.
    /// <para>
    /// Typical usage:
    ///   • Heading/Forward calculation (FixedRotation2D)
    ///   • AI decisions, skill/command generation (FireProjectileRequest, DashCommand, etc.)
    ///   • Any deterministic "decision stage" that prepares data for simulation.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedDecisionGroupAttribute : GroupAttribute
    {
        public FixedDecisionGroupAttribute()
            : base(SystemGroup.FixedDecision)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedSimulation"/> group.
    /// <para>
    /// Typical usage:
    ///   • Movement, physics, projectiles, collisions
    ///   • Core gameplay state changes (HP, buffs, debuffs, etc.)
    ///   • Any deterministic mutation of the simulation state.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedGroupAttribute : GroupAttribute
    {
        public FixedGroupAttribute()
            : base(SystemGroup.FixedSimulation)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FixedPost"/> group.
    /// <para>
    /// Typical usage:
    ///   • Damage resolution, death tagging, respawn scheduling
    ///   • Score/XP accumulation, logging, end-of-tick cleanup
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FixedPostGroupAttribute : GroupAttribute
    {
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
    /// <para>
    /// Typical usage:
    ///   • Unity / device / UI input → RawInputBuffer, ViewClickEvent 등
    ///   • 결정론 코어(Fixed*)가 소비할 "비결정론 입력"을 ECS에 기록하는 단계.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameInputGroupAttribute : GroupAttribute
    {
        public FrameInputGroupAttribute()
            : base(SystemGroup.FrameInput)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameSync"/> group.
    /// <para>
    /// Typical usage:
    ///   • Camera follow/orbit, client-side prediction for visuals
    ///   • View-only logic that derives camera/viewport state from deterministic data.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameSyncGroupAttribute : GroupAttribute
    {
        public FrameSyncGroupAttribute()
            : base(SystemGroup.FrameSync)
        {
        }
    }

    /// <summary>
    /// Marks a system as part of the presentation/read-only stage.
    /// <para>
    /// Mapped to <see cref="SystemGroup.FrameView"/>.
    /// Used for interpolation, view binding, and UI updates that read from the
    /// deterministic core state without mutating it.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameViewGroupAttribute : GroupAttribute
    {
        public FrameViewGroupAttribute()
            : base(SystemGroup.FrameView)
        {
        }
    }
    
    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameUI"/> group.
    /// <para>
    /// Typical usage:
    ///   • HUD / UI / debug overlays
    ///   • HP bars, ammo counters, minimap, quest indicators, etc.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameUIGroupAttribute : GroupAttribute
    {
        public FrameUIGroupAttribute()
            : base(SystemGroup.FrameUI)
        {
        }
    }
}
