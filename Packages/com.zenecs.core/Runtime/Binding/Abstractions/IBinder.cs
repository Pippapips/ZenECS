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
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    public interface IContextAwareBinder
    {
        void ContextAttached(IContext context);
        void ContextDetached(IContext context);
    }
    
    public interface IBinderEnabledFlag
    {
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Internal marker that lets the router/registry preserve "attach sequence" ordering
    /// independent from <see cref="IBinder.ApplyOrder"/>.
    /// </summary>
    public interface IAttachOrderMarker
    {
        /// <summary>Zero-based order assigned at attachment time.</summary>
        int AttachOrder { get; }

        void ResetAttachOrder();
        void SetAttachOrder(int attachOrder);
    }
    
    /// <summary>
    /// Connects an entity’s contexts and deltas to an external presentation target.
    /// </summary>
    public interface IBinder : IAttachOrderMarker, IContextAwareBinder, IBinderEnabledFlag
    {
        /// <summary>
        /// The entity this binder is currently attached to. Default(<see cref="Entity"/>) when detached.
        /// </summary>
        Entity Entity { get; }

        /// <summary>
        /// Execution priority among binders attached to the same entity.
        /// Lower values are applied earlier during the per-frame <see cref="Apply"/> pass.
        /// </summary>
        int ApplyOrder { get; }
        
        void SetApplyOrder(int order);
        void SetApplyOrderAndAttachOrder(int applyOrder, int attachOrder);
        void ResetApplyOrder();
        void ResetApplyOrderAndAttachOrder();

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
        void Apply(IWorld w, Entity e);
    }

    /// <summary>
    /// Convenience base class implementing the common binder lifecycle pattern.
    /// </summary>
    public abstract class BaseBinder : IBinder
    {
        /// <summary>The world this binder is attached to (null when unbound).</summary>
        protected IWorld? World { get; private set; }

        /// <summary>Lookup service for resolving contexts (null when unbound).</summary>
        protected IContextLookup? Contexts { get; private set; }

        public bool Enabled { get; set; } = true;

        /// <inheritdoc/>
        public Entity Entity { get; private set; }

        /// <inheritdoc/>
        public virtual int ApplyOrder => _applyOrder;
        private int _applyOrder;
        
        public virtual int AttachOrder => _attachOrder;
        private int _attachOrder;

        public void ResetAttachOrder()
        {
            this._attachOrder = 0;
        }

        public void ResetApplyOrder()
        {
            this._applyOrder = 0;
        }

        public void ResetApplyOrderAndAttachOrder()
        {
            ResetAttachOrder();
            ResetApplyOrder();
        }

        public void SetAttachOrder(int attachOrder)
        {
            this._attachOrder = attachOrder;
        }

        public void SetApplyOrder(int order)
        {
            this._applyOrder = order;
        }

        public void SetApplyOrderAndAttachOrder(int applyOrder, int attachOrder)
        {
            SetApplyOrder(applyOrder);
            SetAttachOrder(attachOrder);
        }

        private bool _bound;

        /// <inheritdoc/>
        public void Bind(IWorld world, Entity e, IContextLookup contextLookup)
        {
            if (_bound) throw new Exception("Binder is already bound.");
            World = world;
            Contexts = contextLookup;
            Entity = e;
            _bound = true;
            OnBind(e, contextLookup.GetAllContextList(world, e));
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
                Contexts = null;
            }
        }

        public void ContextAttached(IContext context)
        {
            OnContextAttached(context);
        }

        public void ContextDetached(IContext context)
        {
            OnContextDetached(context);
        }

        /// <inheritdoc/>
        public void Apply(IWorld w, Entity e)
        {
            if (!Enabled) return;
            OnApply(w, e);
        }

        /// <summary>
        /// Hook called once after a successful <see cref="Bind"/>. Use to cache context references.
        /// </summary>
        protected virtual void OnBind(Entity e, IReadOnlyList<IContext>? contexts) { }

        /// <summary>
        /// Hook called during <see cref="Unbind"/> for cleanup (unsubscribe, dispose, etc.).
        /// </summary>
        protected virtual void OnUnbind() { }
        
        protected virtual void OnApply(IWorld w, Entity e) { }
        
        protected virtual void OnContextAttached(IContext context) { }
        
        protected virtual void OnContextDetached(IContext context) { }
    }
}
