// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — System Presets
// File: SystemTypeFilterAttribute.cs
// Purpose: Attribute that constrains selectable types in a property drawer
//          (e.g., for SystemTypeRef pickers in the inspector).
// Key concepts:
//   • Used by custom PropertyDrawer to filter candidate types.
//   • BaseType: only types assignable to this base/interface are allowed.
//   • AllowAbstract: controls whether abstract types can be selected as well.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;

namespace ZenECS.Adapter.Unity.SystemPresets
{
    /// <summary>
    /// Attribute used to filter selectable types in a custom inspector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SystemTypeFilterAttribute"/> is intended to be consumed by a
    /// custom <c>PropertyDrawer</c> that renders a type picker (for example,
    /// for <c>SystemTypeRef</c>). The drawer is expected to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Restrict the list of candidate types to those assignable to
    /// <see cref="BaseType"/>.
    /// </description></item>
    /// <item><description>
    /// Optionally exclude abstract types when <see cref="AllowAbstract"/> is
    /// <c>false</c>.
    /// </description></item>
    /// </list>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// [SystemTypeFilter(typeof(ISystem), allowAbstract: false)]
    /// public SystemTypeRef[] systems;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SystemTypeFilterAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets the base type or interface that all selectable types must implement.
        /// </summary>
        public Type BaseType { get; }

        /// <summary>
        /// Gets a value indicating whether abstract types are allowed
        /// in the filtered type list.
        /// </summary>
        public bool AllowAbstract { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTypeFilterAttribute"/> class.
        /// </summary>
        /// <param name="baseType">
        /// The base type or interface that candidate types must be assignable to.
        /// This value cannot be <c>null</c>.
        /// </param>
        /// <param name="allowAbstract">
        /// Whether abstract types are allowed to be selected. Defaults to <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="baseType"/> is <c>null</c>.
        /// </exception>
        public SystemTypeFilterAttribute(Type baseType, bool allowAbstract = false)
        {
            BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
            AllowAbstract = allowAbstract;
        }
    }
}
