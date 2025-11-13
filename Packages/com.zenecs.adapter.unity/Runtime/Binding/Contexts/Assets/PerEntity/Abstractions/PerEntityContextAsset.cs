// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Adapter.Unity — Binding
// File: Runtime/Binding/Contexts/Assets/PerEntityContextAsset.cs
// Purpose: Per-entity context asset base.
// Key concepts:
//   • Factory per entity: creates a distinct IContext instance for each entity.
//   • Suitable for model/view holders, per-entity UI, etc.
//   • Lifetime tied to entity; disposed when entity is destroyed.
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    /// <summary>
    /// Base asset for per-entity contexts (entity-owned resources).
    /// </summary>
    public abstract class PerEntityContextAsset : ContextAsset
    {
        public abstract Type ContextType { get; }
        
        /// <summary>
        /// Create a new context instance for the given entity.
        /// Caller is responsible for registering it to the entity.
        /// </summary>
        public abstract IContext CreateContextForEntity(IWorld world, Entity e);
    }
}