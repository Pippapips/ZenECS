// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: BinderAsset.cs
// Purpose: Base ScriptableObject for Unity-side binder configuration assets
//          that participate in ZenECS binding.
// Key concepts:
//   • Editor configuration: stores references to binders, configuration, etc.
//   • Runtime factory base: specialized assets create concrete IBinder types.
//   • Extended by per-entity binder asset variants.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Binders.Assets
{
    /// <summary>
    /// Base asset for Unity-side binder configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BinderAsset"/> is the root ScriptableObject type used by
    /// ZenECS binding to describe how Unity-facing binders are created and
    /// wired. Concrete subclasses act as configuration containers and factory
    /// descriptors that create binder instances per entity.
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
    /// called to allocate the concrete binder instance.
    /// </description></item>
    /// <item><description>
    /// The caller attaches the returned binder to the entity or world and
    /// manages its disposal when the entity is destroyed.
    /// </description></item>
    /// </list>
    /// <para>
    /// At runtime, higher-level binding code or systems are responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Interpreting the configuration stored in the asset.</description></item>
    /// <item><description>Instantiating the appropriate binder objects.</description></item>
    /// <item><description>Managing their lifetime alongside ECS entities or worlds.</description></item>
    /// </list>
    /// </remarks>
    public abstract class BinderAsset : ScriptableObject
    {
        /// <summary>
        /// Gets the concrete <see cref="Type"/> of the binder instances
        /// produced by this asset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementations should return the runtime type of the
        /// <see cref="IBinder"/> created in <see cref="Create"/>.
        /// </para>
        /// <para>
        /// This metadata is used by tooling or binding code that needs to
        /// introspect the binder type without instantiating it.
        /// </para>
        /// </remarks>
        public abstract Type BinderType { get; }

        /// <summary>
        /// Creates a new binder instance for an entity.
        /// </summary>
        /// <returns>
        /// A newly created <see cref="IBinder"/> instance that is intended to
        /// be owned by a single entity.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method does not automatically attach the binder to any entity
        /// or world. The caller is responsible for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Associating the binder with the target entity.</description></item>
        /// <item><description>Registering it with any relevant systems or worlds.</description></item>
        /// <item><description>Disposing it when the owning entity is destroyed.</description></item>
        /// </list>
        /// </remarks>
        public abstract IBinder Create();
    }
}

