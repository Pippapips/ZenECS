// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBinder.cs
// Purpose: Binder contracts to connect ECS data (contexts + deltas) to external
//          presentation layers (rendering, UI, audio, animation, etc.).
// Key concepts:
//   • Decoupled: binders read contexts and apply to view; they don't command Core.
//   • Lifecycle: Bind/Unbind are managed by the world/registry; Apply runs once per frame end.
//   • Ordering: Priority and AttachOrder coordinate multi-binder execution.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Implemented by binders that want to be notified when contexts are attached
    /// to or detached from the entity this binder is bound to.
    /// </summary>
    public interface IContextAwareBinder
    {
        /// <summary>
        /// Called when a context becomes available for the bound entity.
        /// Implementations typically cache or subscribe to the given context.
        /// </summary>
        /// <param name="context">The context that was attached.</param>
        void ContextAttached(IContext context);

        /// <summary>
        /// Called when a context is removed from the bound entity.
        /// Implementations should release or unsubscribe from the given context.
        /// </summary>
        /// <param name="context">The context that was detached.</param>
        void ContextDetached(IContext context);
    }

    /// <summary>
    /// Optional flag that allows enabling or disabling a binder without unbinding it.
    /// </summary>
    public interface IBinderEnabledFlag
    {
        /// <summary>
        /// Gets or sets whether this binder is currently enabled.
        /// When <c>false</c>, the binder will be skipped in the <see cref="IBinder.Apply"/> pass.
        /// </summary>
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Internal marker that lets the router/registry preserve "attach sequence"
    /// ordering independent from <see cref="IBinder.ApplyOrder"/>.
    /// </summary>
    public interface IAttachOrderMarker
    {
        /// <summary>
        /// Zero-based order assigned at attachment time.
        /// This is stable for the lifetime of the binder attachment.
        /// </summary>
        int AttachOrder { get; }

        /// <summary>
        /// Reset the stored attach order back to its default value.
        /// </summary>
        void ResetAttachOrder();

        /// <summary>
        /// Set the <see cref="AttachOrder"/> value explicitly.
        /// </summary>
        /// <param name="attachOrder">The new attach order index.</param>
        void SetAttachOrder(int attachOrder);
    }

    /// <summary>
    /// Connects an entity’s contexts and component deltas to an external presentation target.
    /// </summary>
    public interface IBinder : IAttachOrderMarker, IContextAwareBinder, IBinderEnabledFlag
    {
        /// <summary>
        /// The entity this binder is currently attached to.
        /// Contains <see cref="Entity.None"/> (default value) when detached.
        /// </summary>
        Entity Entity { get; }

        /// <summary>
        /// Execution priority among binders attached to the same entity.
        /// Lower values are applied earlier during the per-frame <see cref="Apply"/> pass.
        /// </summary>
        int ApplyOrder { get; }

        /// <summary>
        /// Set the <see cref="ApplyOrder"/> for this binder.
        /// </summary>
        /// <param name="order">New application order value.</param>
        void SetApplyOrder(int order);

        /// <summary>
        /// Set both <see cref="ApplyOrder"/> and <see cref="IAttachOrderMarker.AttachOrder"/>
        /// in a single call, keeping them in sync when needed.
        /// </summary>
        /// <param name="applyOrder">New <see cref="ApplyOrder"/>.</param>
        /// <param name="attachOrder">New <see cref="IAttachOrderMarker.AttachOrder"/>.</param>
        void SetApplyOrderAndAttachOrder(int applyOrder, int attachOrder);

        /// <summary>
        /// Reset the <see cref="ApplyOrder"/> back to its default value.
        /// </summary>
        void ResetApplyOrder();

        /// <summary>
        /// Reset both <see cref="IAttachOrderMarker.AttachOrder"/> and <see cref="ApplyOrder"/>
        /// back to their default values.
        /// </summary>
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
        /// Detach this binder from its entity. Called when the entity is destroyed
        /// or the binder is removed. Must be safe to call multiple times
        /// (no-op after the first detach).
        /// </summary>
        void Unbind();

        /// <summary>
        /// Per-frame application step invoked at the end of Presentation.
        /// Implementations should perform view updates based on captured
        /// deltas and contexts in this method.
        /// </summary>
        /// <param name="w">The world in which the entity exists.</param>
        /// <param name="e">The entity this binder is applied to.</param>
        void Apply(IWorld w, Entity e);
    }

    /// <summary>
    /// Convenience base class implementing the common binder lifecycle pattern.
    /// Derive from this for typical binders that:
    /// <list type="bullet">
    /// <item><description>Cache <see cref="World"/> and <see cref="Contexts"/> on bind.</description></item>
    /// <item><description>React to context attach/detach notifications.</description></item>
    /// <item><description>Perform updates in <see cref="OnApply"/>.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseBinder : IBinder
    {
        [NonSerialized] private bool _done;

        private int _applyOrder;
        private int _attachOrder;
        private bool _bound;

        /// <summary>
        /// Gets a value indicating whether this binder has finished its lifetime
        /// and should no longer receive <see cref="Apply"/> calls.
        /// </summary>
        public bool Done => _done;

        /// <summary>
        /// The world this binder is attached to. <c>null</c> when unbound.
        /// </summary>
        protected IWorld? World { get; private set; }

        /// <summary>
        /// Lookup service for resolving contexts. <c>null</c> when unbound.
        /// </summary>
        protected IContextLookup? Contexts { get; private set; }

        /// <inheritdoc/>
        public bool Enabled { get; set; } = true;

        /// <inheritdoc/>
        public Entity Entity { get; private set; }

        /// <inheritdoc/>
        public virtual int ApplyOrder => _applyOrder;

        /// <inheritdoc/>
        public virtual int AttachOrder => _attachOrder;

        /// <inheritdoc/>
        public void ResetAttachOrder()
        {
            _attachOrder = 0;
        }

        /// <inheritdoc/>
        public void ResetApplyOrder()
        {
            _applyOrder = 0;
        }

        /// <inheritdoc/>
        public void ResetApplyOrderAndAttachOrder()
        {
            ResetAttachOrder();
            ResetApplyOrder();
        }

        /// <inheritdoc/>
        public void SetAttachOrder(int attachOrder)
        {
            _attachOrder = attachOrder;
        }

        /// <inheritdoc/>
        public void SetApplyOrder(int order)
        {
            _applyOrder = order;
        }

        /// <inheritdoc/>
        public void SetApplyOrderAndAttachOrder(int applyOrder, int attachOrder)
        {
            SetApplyOrder(applyOrder);
            SetAttachOrder(attachOrder);
        }

        /// <summary>
        /// Mark this binder as finished. Once marked done, it will no longer
        /// receive <see cref="Apply"/> calls.
        /// </summary>
        public void MarkDone()
        {
            _done = true;
        }

        /// <inheritdoc/>
        public void Bind(IWorld world, Entity e, IContextLookup contextLookup)
        {
            if (_bound)
                throw new Exception("Binder is already bound.");

            World = world;
            Contexts = contextLookup;
            Entity = e;
            _bound = true;

            if (Contexts != null)
            {
                var contextList = Contexts.GetAllContextList(world, e);
                if (contextList != null)
                {
                    foreach (var context in contextList)
                    {
                        OnContextAttached(context);
                    }
                }
            }

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
                Contexts = null;
            }
        }

        /// <inheritdoc/>
        public void ContextAttached(IContext context)
        {
            OnContextAttached(context);
        }

        /// <inheritdoc/>
        public void ContextDetached(IContext context)
        {
            OnContextDetached(context);
        }

        /// <inheritdoc/>
        public void Apply(IWorld w, Entity e)
        {
            if (_done) return;
            if (!Enabled) return;

            OnApply(w, e);
        }

        /// <summary>
        /// Hook called once after a successful <see cref="Bind"/>. Use to cache
        /// context references or initialize state based on the bound entity.
        /// </summary>
        /// <param name="e">The entity this binder was bound to.</param>
        protected virtual void OnBind(Entity e) { }

        /// <summary>
        /// Hook called during <see cref="Unbind"/> for cleanup
        /// (unsubscribe, dispose, clear caches, etc.).
        /// </summary>
        protected virtual void OnUnbind() { }

        /// <summary>
        /// Hook called from <see cref="Apply"/> when the binder is enabled and
        /// not marked as done. Perform view updates for the given entity here.
        /// </summary>
        /// <param name="w">The world in which the entity exists.</param>
        /// <param name="e">The entity this binder is applied to.</param>
        protected virtual void OnApply(IWorld w, Entity e) { }

        /// <summary>
        /// Hook called whenever a context is attached to the bound entity.
        /// Override to cache and react to the given context instance.
        /// </summary>
        /// <param name="context">The context that was attached.</param>
        protected virtual void OnContextAttached(IContext context) { }

        /// <summary>
        /// Hook called whenever a context is detached from the bound entity.
        /// Override to release or unsubscribe from the given context instance.
        /// </summary>
        /// <param name="context">The context that was detached.</param>
        protected virtual void OnContextDetached(IContext context) { }
    }
}
