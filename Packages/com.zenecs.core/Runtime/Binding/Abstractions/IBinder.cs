// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBinder.cs
// Purpose: Binder contracts to connect ECS data (contexts + deltas) to external
//          presentation layers (rendering, UI, audio, animation, etc.).
// Key concepts:
//   • Decoupled: binders read contexts and apply to view; they don't command Core.
//   • Lifecycle: Bind/Unbind are managed by the world/registry; Apply runs once per frame end.
//   • Ordering: Priority and AttachOrder coordinate multi-binder execution.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Connects an entity’s contexts and deltas to an external presentation target.
    /// </summary>
    public interface IBinder
    {
        /// <summary>
        /// The entity this binder is currently attached to. Default(<see cref="Entity"/>) when detached.
        /// </summary>
        Entity Entity { get; }

        /// <summary>
        /// Execution priority among binders attached to the same entity.
        /// Lower values are applied earlier during the per-frame <see cref="Apply"/> pass.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Attach this binder to <paramref name="e"/> and cache lookup services.
        /// Called exactly once per attachment by the world/router.
        /// </summary>
        /// <param name="world">Owning world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="contextLookup">Context lookup for resolving required resources.</param>
        void Bind(IWorld world, Entity e, IContextLookup contextLookup);

        /// <summary>
        /// Detach this binder from its entity. Called when the entity despawns or the binder is removed.
        /// Must be safe to call multiple times (no-op after first detach).
        /// </summary>
        void Unbind();

        /// <summary>
        /// Per-frame application step invoked at the end of Presentation.
        /// Perform view updates based on captured deltas/contexts here.
        /// </summary>
        void Apply();
    }

    /// <summary>
    /// Internal marker that lets the router/registry preserve "attach sequence" ordering
    /// independent from <see cref="IBinder.Priority"/>.
    /// </summary>
    public interface IAttachOrderMarker
    {
        /// <summary>Zero-based order assigned at attachment time.</summary>
        int AttachOrder { get; set; }
    }

    /// <summary>
    /// Convenience base class implementing the common binder lifecycle pattern.
    /// </summary>
    public abstract class BaseBinder : IBinder, IAttachOrderMarker
    {
        /// <summary>The world this binder is attached to (null when unbound).</summary>
        protected IWorld? World { get; private set; }

        /// <summary>Lookup service for resolving contexts (null when unbound).</summary>
        protected IContextLookup? ContextLookup { get; private set; }

        /// <inheritdoc/>
        public Entity Entity { get; private set; }

        /// <inheritdoc/>
        public virtual int Priority { get; set; }

        int IAttachOrderMarker.AttachOrder { get; set; }

        private bool _bound;

        /// <inheritdoc/>
        public void Bind(IWorld world, Entity e, IContextLookup contextLookup)
        {
            if (_bound) throw new Exception("Binder is already bound.");
            World = world;
            ContextLookup = contextLookup;
            Entity = e;
            _bound = true;
            OnBind(e);
        }

        /// <inheritdoc/>
        public void Unbind()
        {
            if (!_bound) return;
            try
            {
                OnUnbind();
            }
            finally
            {
                _bound = false;
                World = null;
                Entity = default;
                ContextLookup = null;
            }
        }

        /// <inheritdoc/>
        public virtual void Apply() { }

        /// <summary>
        /// Hook called once after a successful <see cref="Bind"/>. Use to cache context references.
        /// </summary>
        protected virtual void OnBind(Entity e) { }

        /// <summary>
        /// Hook called during <see cref="Unbind"/> for cleanup (unsubscribe, dispose, etc.).
        /// </summary>
        protected virtual void OnUnbind() { }
    }
}
