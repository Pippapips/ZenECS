// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Adapter.Unity — Binding
// File: Runtime/Binding/Contexts/PerEntity/ModelContext.cs
// Purpose: Per-entity model context holding instantiated GameObject/Transform.
// Key concepts:
//   • Owned by a single entity; not shared.
//   • Holds references to GameObject/Transform/Animator used by binders.
//   • Destroyed when entity (or owning system) disposes it.
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts
{
    /// <summary>
    /// Entity-owned model context wrapping a Unity GameObject instance.
    /// </summary>
    public sealed class ModelContext : IContext, IContextInitialize
    {
        /// <summary>The instantiated GameObject for this entity's model.</summary>
        public GameObject? Instance { get; set; } = null!;

        /// <summary>Cached root transform for fast access.</summary>
        public Transform? Root { get; set; } = null!;

        /// <summary>Optional Animator attached to the model.</summary>
        public Animator? Animator { get; set; }

        public void Initialize(IWorld w, Entity e, IContextLookup l)
        {
        }
        
        public void Deinitialize(IWorld w, Entity e)
        {
            Object.Destroy(Instance);
            Instance = null;
            Root = null;
        }
    }
}