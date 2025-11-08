// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IContext.cs
// Purpose: Context contracts — per-entity resource containers consumed by binders.
// Key concepts:
//   • Lookup first: binders resolve contexts via IContextLookup during Bind/Apply.
//   • Optional lifecycle: Initialize/Deinitialize/Reinitialize hooks on registry events.
//   • Sharing: contexts may be shared references; ownership is defined by the registry policy.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Marker for resource containers (data/references) that binders use to apply view/state to external systems.
    /// </summary>
    public interface IContext { }

    /// <summary>
    /// Read-only lookup surface to discover contexts attached to an entity.
    /// </summary>
    public interface IContextLookup
    {
        /// <summary>
        /// Try get a context of type <typeparamref name="T"/> for <paramref name="e"/>.
        /// </summary>
        bool TryGet<T>(IWorld w, Entity e, out T ctx) where T : class, IContext;

        /// <summary>
        /// Get a context of type <typeparamref name="T"/> or throw if missing.
        /// </summary>
        T Get<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>
        /// Returns <c>true</c> if a context of type <typeparamref name="T"/> exists for <paramref name="e"/>.
        /// </summary>
        bool Has<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>
        /// Returns <c>true</c> if the specific <paramref name="ctx"/> instance is registered for <paramref name="e"/>.
        /// </summary>
        bool Has(IWorld w, Entity e, IContext ctx);
    }

    /// <summary>
    /// Optional lifecycle called by the registry when contexts are added/removed.
    /// </summary>
    public interface IContextInitialize
    {
        /// <summary>
        /// Called once when the context is first registered for the entity.
        /// Other contexts can be resolved from <paramref name="l"/>.
        /// </summary>
        void Initialize(IWorld w, Entity e, IContextLookup l);

        /// <summary>
        /// Called when the context is removed or the entity/world is being destroyed.
        /// </summary>
        void Deinitialize(IWorld w, Entity e);
    }

    /// <summary>
    /// Optional fast re-init path used when a context can rebind without full deinit/init.
    /// If not implemented, the registry falls back to Deinitialize → Initialize.
    /// </summary>
    public interface IContextReinitialize : IContextInitialize
    {
        /// <summary>Reinitialize the context when a quick rebind is possible.</summary>
        void Reinitialize(IWorld w, Entity e, IContextLookup l);
    }
}
