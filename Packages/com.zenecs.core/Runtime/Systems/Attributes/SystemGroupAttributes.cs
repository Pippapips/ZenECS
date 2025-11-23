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
    ///     <see cref="FixedSimulationGroupAttribute"/>,
    ///     <see cref="FixedPostGroupAttribute"/>,
    ///     <see cref="FrameInputGroupAttribute"/>,
    ///     <see cref="FrameViewGroupAttribute"/>,
    ///     <see cref="FrameUIGroupAttribute"/>,
    ///     or <see cref="PresentationGroupAttribute"/>.
    ///   • This attribute is the generic, low-level form used by the planner.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SimulationGroupAttribute : Attribute
    {
        /// <summary>
        /// The group this system belongs to.
        /// </summary>
        public SystemGroup Group { get; }

        /// <summary>
        /// Creates a new group attribute with the specified <see cref="SystemGroup"/>.
        /// </summary>
        public SimulationGroupAttribute(SystemGroup group)
        {
            Group = group;
        }

        /// <summary>
        /// Backwards-compatible ctor for legacy usages without parameters.
        /// Historically mapped to "Simulation" group; now defaults to <see cref="SystemGroup.FixedSimulation"/>.
        /// </summary>
        [Obsolete("Use the ctor with SystemGroup argument or a specific *GroupAttribute (e.g. [FixedSimulationGroup]).")]
        public SimulationGroupAttribute()
            : this(SystemGroup.FixedSimulation)
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Legacy aliases (for compatibility with older code)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Legacy alias for a fixed pre-simulation group.
    /// <para>
    /// Historically used as "FrameSetup".
    /// Now mapped to <see cref="SystemGroup.FixedInput"/>.
    /// Prefer <see cref="FixedInputGroupAttribute"/> or <see cref="FixedDecisionGroupAttribute"/> for new code.
    /// </para>
    /// </summary>
    [Obsolete("Use [FixedInputGroup] or [FixedDecisionGroup] instead.")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SetupGroupAttribute : SimulationGroupAttribute
    {
        public SetupGroupAttribute()
            : base(SystemGroup.FixedInput)
        {
        }
    }

    // 기존의 PresentationGroupAttribute 이름은 유지하면서
    // 새 SystemGroup.Presentation에 매핑한다.
    /// <summary>
    /// Marks a system as part of the presentation/read-only stage.
    /// <para>
    /// Mapped to <see cref="SystemGroup.Presentation"/>.
    /// Used for interpolation, view binding, and UI updates that read from the
    /// deterministic core state without mutating it.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PresentationGroupAttribute : SimulationGroupAttribute
    {
        public PresentationGroupAttribute()
            : base(SystemGroup.Presentation)
        {
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
    public sealed class FixedInputGroupAttribute : SimulationGroupAttribute
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
    public sealed class FixedDecisionGroupAttribute : SimulationGroupAttribute
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
    public sealed class FixedSimulationGroupAttribute : SimulationGroupAttribute
    {
        public FixedSimulationGroupAttribute()
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
    public sealed class FixedPostGroupAttribute : SimulationGroupAttribute
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
    public sealed class FrameInputGroupAttribute : SimulationGroupAttribute
    {
        public FrameInputGroupAttribute()
            : base(SystemGroup.FrameInput)
        {
        }
    }

    /// <summary>
    /// Marks a system as belonging to the <see cref="SystemGroup.FrameView"/> group.
    /// <para>
    /// Typical usage:
    ///   • Camera follow/orbit, client-side prediction for visuals
    ///   • View-only logic that derives camera/viewport state from deterministic data.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameViewGroupAttribute : SimulationGroupAttribute
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
    public sealed class FrameUIGroupAttribute : SimulationGroupAttribute
    {
        public FrameUIGroupAttribute()
            : base(SystemGroup.FrameUI)
        {
        }
    }
}
