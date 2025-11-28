// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity
// File: ZenReadOnlyInInspectorAttribute.cs
// Purpose: Attribute that marks components, fields, or properties as
//          read-only in Unity inspectors and ZenECS editor tooling.
// Key concepts:
//   • Read-only visualization: prevents editing but still shows the value.
//   • Can be applied to classes, fields, and properties.
//   • Intended for data driven by systems, not by manual authoring.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Marks a component, field, or property as read-only in the inspector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is used by Unity- and ZenECS-specific editor tooling to
    /// render a member as non-editable while still displaying its current
    /// value. It is typically applied to data that is entirely controlled by
    /// systems (for example, runtime stats or internal state) and should not
    /// be modified manually in the inspector.
    /// </para>
    /// <para>
    /// Applying this attribute does <b>not</b> change runtime behavior, nor
    /// does it affect serialization or networking; it is purely a UI hint for
    /// editor-time visualization.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class ZenReadOnlyInInspectorAttribute : Attribute
    {
    }
}