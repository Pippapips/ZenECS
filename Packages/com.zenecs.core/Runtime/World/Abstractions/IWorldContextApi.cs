// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Context API
// File: IWorldContextApi.cs
// Purpose: Register per-entity view contexts consumed by binders/renderers.
// Key concepts:
//   • Registry per world: attach arbitrary resources to entities.
//   • Binder integration: binders resolve contexts to render/apply updates.
//   • Ownership: registry defines replacement/merge policy by type/key.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    /// <summary>Register and manage view contexts for entities.</summary>
    public interface IWorldContextApi
    {
        /// <summary>Register a context object for an entity (policy controls overwrite).</summary>
        void RegisterContext(Entity e, IContext ctx);
        bool HasContext(Entity e, Type? contextType);
        (Type type, object boxed)[] GetAllContexts(Entity e);
        bool RemoveContext(Entity e, IContext ctx);
        bool ReinitializeContext(Entity e, IContext ctx);
    }
}