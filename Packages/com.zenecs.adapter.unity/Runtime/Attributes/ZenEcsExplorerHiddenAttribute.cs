// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity
// File: ZenEcsExplorerHiddenAttribute.cs
// Purpose: Attribute for marking fields and properties as hidden in ZenECS
//          Explorer and related editor tooling.
// Key concepts:
//   • Tooling hint only: does not affect runtime behavior.
//   • Can be applied to fields and properties.
//   • Supports inheritance but disallows multiple usage on the same member.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Marks a field or property as hidden from the ZenECS Explorer UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is used by editor tooling to selectively hide members
    /// from ZenECS Explorer and related inspectors. It does not affect runtime
    /// execution, serialization, or networking; it is purely a tooling hint.
    /// </para>
    /// <para>
    /// Apply this to any field or property that should not be visible or
    /// interactable in ZenECS editor views, for example internal caches,
    /// temporary state, or values that are managed exclusively by systems.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ZenEcsExplorerHiddenAttribute : Attribute
    {
    }
}