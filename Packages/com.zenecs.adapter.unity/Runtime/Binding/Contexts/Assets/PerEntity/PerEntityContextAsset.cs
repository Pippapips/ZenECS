// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: PerEntityContextAsset.cs
// Purpose: Base ScriptableObject asset for creating per-entity context
//          instances used by ZenECS binding and view/model integration.
// Key concepts:
//   • Factory-per-entity: produces a distinct IContext per entity.
//   • Used for view-models, UI presenters, or entity-owned services.
//   • Lifetime is typically tied to the owning entity's lifetime.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    /// <summary>
    /// Base asset for per-entity contexts (entity-owned resources).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="PerEntityContextAsset"/> acts as a factory for creating a
    /// new <see cref="IContext"/> instance per entity. It is commonly used to
    /// construct view-models, presenters, or other entity-local services that
    /// participate in ZenECS binding.
    /// </para>
    /// <para>
    /// The typical lifecycle is:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// The asset is referenced by some binding configuration or blueprint.
    /// </description></item>
    /// <item><description>
    /// When an entity is created, the asset's <see cref="Create"/> method is
    /// called to allocate the concrete context instance.
    /// </description></item>
    /// <item><description>
    /// The caller registers the returned context with the entity or world and
    /// manages its disposal when the entity is destroyed.
    /// </description></item>
    /// </list>
    /// </remarks>
    public abstract class PerEntityContextAsset : ContextAsset
    {
        /// <summary>
        /// Gets the concrete <see cref="Type"/> of the context instances
        /// produced by this asset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementations should return the runtime type of the
        /// <see cref="IContext"/> created in <see cref="Create"/>.
        /// </para>
        /// <para>
        /// This metadata is used by tooling or binding code that needs to
        /// introspect the context type without instantiating it.
        /// </para>
        /// </remarks>
        public abstract Type ContextType { get; }

        /// <summary>
        /// Creates a new context instance for an entity.
        /// </summary>
        /// <returns>
        /// A newly created <see cref="IContext"/> instance that is intended to
        /// be owned by a single entity.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method does not automatically attach the context to any entity
        /// or world. The caller is responsible for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Associating the context with the target entity.</description></item>
        /// <item><description>Registering it with any relevant binders or systems.</description></item>
        /// <item><description>Disposing it when the owning entity is destroyed.</description></item>
        /// </list>
        /// </remarks>
        public abstract IContext Create();
    }
}
