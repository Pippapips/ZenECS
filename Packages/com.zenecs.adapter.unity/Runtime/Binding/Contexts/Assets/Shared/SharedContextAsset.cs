// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: SharedContextAsset.cs
// Purpose: Base asset for shared (global) context instances used by ZenECS
//          binding and view/model integration.
// Key concepts:
//   • Shared lifetime: a single IContext instance reused by many entities.
//   • Typically used for UI roots, global services, or cross-cutting state.
//   • Exposes the runtime context type for tooling and introspection.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    /// <summary>
    /// Base asset for shared (global) context markers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="SharedContextAsset"/> represents a shared context that is
    /// typically instantiated once and reused across multiple entities, views,
    /// or systems. Examples include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A UI root context managing global UI state.</description></item>
    /// <item><description>Application-wide services or singletons.</description></item>
    /// <item><description>
    /// Cross-cutting contexts referenced by many bindings simultaneously.
    /// </description></item>
    /// </list>
    /// <para>
    /// The concrete asset implementation is responsible for creating and
    /// managing the underlying <c>IContext</c> instance; this base type only
    /// exposes metadata about the context type.
    /// </para>
    /// </remarks>
    public abstract class SharedContextAsset : ContextAsset
    {
        /// <summary>
        /// Gets the concrete shared context type represented by this asset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned value is typically something like
        /// <c>typeof(UIRootContext)</c>, and is used by binding pipelines or
        /// tooling to discover which shared context is being referenced without
        /// needing to instantiate it.
        /// </para>
        /// </remarks>
        public abstract Type ContextType { get; }
    }
}
