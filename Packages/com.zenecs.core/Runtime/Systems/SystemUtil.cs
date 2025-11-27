// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Systems
// File: SystemUtil.cs
// Purpose: Utility helpers for resolving system metadata such as execution groups.
// Key concepts:
//   • ResolveGroup: inspect attributes on a system type and map to SystemGroup.
//   • Attribute-first: explicit [GroupAttribute] (and derivatives) take priority.
//   • Fallback currently returns Unknown (marker-based inference can be added later).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Utility helpers for resolving system metadata such as execution groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The APIs here are intentionally small and side-effect free: they only
    /// inspect metadata on system types (attributes, marker interfaces, etc.)
    /// and do not touch world or runtime state.
    /// </para>
    /// </remarks>
    public static class SystemUtil
    {
        /// <summary>
        /// Resolves the execution <see cref="SystemGroup"/> for a system type.
        /// </summary>
        /// <param name="t">System type to inspect.</param>
        /// <returns>
        /// The resolved <see cref="SystemGroup"/> if an explicit
        /// <see cref="GroupAttribute"/> (or one of its concrete subclasses such as
        /// <see cref="FixedInputGroupAttribute"/>,
        /// <see cref="FixedDecisionGroupAttribute"/>,
        /// <see cref="FixedGroupAttribute"/>,
        /// <see cref="FixedPostGroupAttribute"/>,
        /// <see cref="FrameInputGroupAttribute"/>,
        /// <see cref="FrameSyncGroupAttribute"/>,
        /// <see cref="FrameViewGroupAttribute"/>,
        /// <see cref="FrameUIGroupAttribute"/>) is present on the type;
        /// otherwise <see cref="SystemGroup.Unknown"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Current behavior is attribute-only. If you want additional inference
        /// (for example, based on marker interfaces such as <c>IFixedRunSystem</c>
        /// or <c>IFrameRunSystem</c>), this is the place to extend.
        /// </para>
        /// </remarks>
        public static SystemGroup ResolveGroup(Type t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));

            // 1) If there is an explicit GroupAttribute (or any subclass) on the type,
            //    use it as the authoritative source for the system group.
            if (Attribute.GetCustomAttribute(t, typeof(GroupAttribute), inherit: false)
                is GroupAttribute sgAttr)
            {
                return sgAttr.Group;
            }

            // 2) No explicit group attribute was found; caller can treat this as
            //    "unassigned" and decide a default (e.g., FixedSimulation) elsewhere.
            return SystemGroup.Unknown;
        }
    }
}
