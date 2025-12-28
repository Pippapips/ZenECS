// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: SystemOrderAttributes.cs
// Purpose: Specifies execution order constraints between systems.
// Key concepts:
//   • Declares that this system must run BEFORE or AFTER another target system.
//   • Multiple attributes can be used to define multiple dependencies.
//   • The planner is expected to interpret these constraints WITHIN the same
//     high-level SystemGroup (FixedInput, FixedDecision, FixedSimulation,
//     FixedPost, FrameInput, FrameView, FrameUI, Presentation, ...).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Declares that this system should execute <b>before</b> the specified target system type.
    /// <para>
    /// Semantics:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The planner should ensure that the declaring system appears earlier in the
    ///       execution order than <see cref="Target"/> <b>within the same
    ///       <see cref="SystemGroup"/></b>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Cross-group ordering is not enforced here; group ordering is defined by
    ///       the runner (e.g. FixedInput → FixedDecision → FixedSimulation → FixedPost).
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// Multiple <see cref="OrderBeforeAttribute"/> instances can be applied to a single
    /// system to declare multiple dependencies.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderBeforeAttribute : Attribute
    {
        /// <summary>
        /// The target system type that must execute after this one.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBeforeAttribute"/> class.
        /// </summary>
        /// <param name="target">The target system type to execute after this system.</param>
        public OrderBeforeAttribute(Type target)
        {
            Target = target;
        }
    }

    /// <summary>
    /// Declares that this system should execute <b>after</b> the specified target system type.
    /// <para>
    /// Semantics:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The planner should ensure that the declaring system appears later in the
    ///       execution order than <see cref="Target"/> <b>within the same
    ///       <see cref="SystemGroup"/></b>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Cross-group ordering is handled by the runner's fixed pipeline, not by this attribute.
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// Multiple <see cref="OrderAfterAttribute"/> instances can be applied to a single
    /// system to declare multiple dependencies.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderAfterAttribute : Attribute
    {
        /// <summary>
        /// The target system type that must execute before this one.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderAfterAttribute"/> class.
        /// </summary>
        /// <param name="target">The target system type to execute before this system.</param>
        public OrderAfterAttribute(Type target)
        {
            Target = target;
        }
    }
}
