// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Context API
// File: IWorldContextApi.cs
// Purpose: Register per-entity view contexts consumed by binders/renderers.
// Key concepts:
//   • Registry per world: attach arbitrary resources to entities.
//   • Binder integration: binders resolve contexts to render/apply updates.
//   • Ownership: registry defines replacement/merge policy by type/key.
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    /// <summary>Register and manage view contexts for entities.</summary>
    public interface IWorldContextApi
    {
        /// <summary>Register a context object for an entity (policy controls overwrite).</summary>
        void RegisterContext(Entity e, IContext ctx);
    }
}