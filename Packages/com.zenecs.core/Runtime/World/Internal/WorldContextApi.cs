// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Context API)
// File: WorldContextApi.cs
// Purpose: Register per-entity view contexts used by binders/renderers.
// Key concepts:
//   • Context registry per world: attach arbitrary resources to entities.
//   • Binder access: binders resolve contexts to render/apply updates.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldContextApi"/> – context registration surface.
    /// </summary>
    internal sealed partial class World : IWorldContextApi
    {
        /// <summary>
        /// Register a context object for the given entity (overwrites same-type entries as defined by registry policy).
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="ctx">Context instance.</param>
        public void RegisterContext(Entity e, IContext ctx) => _contextRegistry.Register(this, e, ctx);
        public bool HasContext(Entity e, Type? contextType) => _contextRegistry.Has(this, e, contextType);
        public (Type type, object boxed)[] GetAllContexts(Entity e) => _contextRegistry.GetAllContexts(this, e);
        public bool RemoveContext(Entity e, IContext ctx) => _contextRegistry.Remove(this, e, ctx);
        public bool ReinitializeContext(Entity e, IContext ctx) => _contextRegistry.Reinitialize(this, e, ctx);
    }
}