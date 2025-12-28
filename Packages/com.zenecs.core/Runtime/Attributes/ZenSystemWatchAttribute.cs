// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Attributes
// File: ZenSystemWatchAttribute.cs
// Purpose: Editor-only helper attribute to declare "watch queries" for systems.
// Key concepts:
//   • AllOf-only filter: collects entities that contain all specified components.
//   • Editor hint: used by tools (e.g., Explorer/Inspector) to build quick views.
//   • Debug-only: compiled only in UNITY_EDITOR builds via Conditional attribute.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Diagnostics;

namespace ZenECS.Core
{
    /// <summary>
    /// Declares a simple editor-only watch query for a system.
    /// Tools can use this information to collect entities that contain
    /// all of the specified component types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is only evaluated in <c>UNITY_EDITOR</c> builds
    /// (via <see cref="ConditionalAttribute"/>) and is intended for
    /// debugging and visualization purposes (e.g., in an Explorer window).
    /// </para>
    /// <para>
    /// Multiple attributes can be placed on the same system type to
    /// describe several different watch queries.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenSystemWatchAttribute : Attribute
    {
        /// <summary>
        /// Component types that must all be present on an entity in order
        /// for it to match this watch query.
        /// </summary>
        public readonly Type[] AllOf;

        /// <summary>
        /// Creates a new watch attribute that matches entities containing
        /// all of the specified component types.
        /// </summary>
        /// <param name="allOf">
        /// One or more component types that must all be present on an entity.
        /// </param>
        public ZenSystemWatchAttribute(params Type[] allOf)
        {
            AllOf = allOf;
        }
    }
}
