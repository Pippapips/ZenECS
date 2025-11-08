// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IContextRegistry.cs
// Purpose: Registry + lookup interface for per-entity contexts.
// Key concepts:
//   • Extends IContextLookup for read paths.
//   • Manages Initialize/Deinitialize/Reinitialize for registered contexts.
//   • World-scoped lifetime; Clear and ClearAll for teardown.
// License: MIT
// © 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal.Contexts
{
    /// <summary>
    /// Registry that stores contexts per entity and manages their lifecycle.
    /// </summary>
    internal interface IContextRegistry : IContextLookup
    {
        // Register / Remove (registry manages Initialize/Deinitialize & initialized flag)

        /// <summary>Register a context for an entity (calls Initialize when supported).</summary>
        void Register(IWorld w, Entity e, IContext ctx);

        /// <summary>Remove a specific context instance (calls Deinitialize when needed).</summary>
        bool Remove(IWorld w, Entity e, IContext ctx);

        /// <summary>Remove a context by type.</summary>
        bool Remove<T>(IWorld w, Entity e) where T : class, IContext;

        // Reinitialize (fast path or Deinit→Init fallback)

        /// <summary>Reinitialize a specific context instance if supported.</summary>
        bool Reinitialize(IWorld w, Entity e, IContext ctx);

        /// <summary>Reinitialize context of type <typeparamref name="T"/>.</summary>
        bool Reinitialize<T>(IWorld w, Entity e) where T : class, IContext;

        // State / cleanup

        /// <summary>Return whether the specific context instance is initialized.</summary>
        bool IsInitialized(IWorld w, Entity e, IContext ctx);

        /// <summary>Return whether the context of type <typeparamref name="T"/> is initialized.</summary>
        bool IsInitialized<T>(IWorld w, Entity e) where T : class, IContext;

        /// <summary>Remove all contexts for the entity (with proper deinitialization).</summary>
        void Clear(IWorld w, Entity e);

        /// <summary>Remove all contexts for all entities in the world.</summary>
        void ClearAll();
    }
}
