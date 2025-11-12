// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: BindingRouter.cs
// Purpose: Manage binders per-entity, validate required contexts, dispatch
//          component deltas, and call Apply() once per frame.
// Key concepts:
//   • Per-entity lists: ordered by Priority, then stable attach-sequence.
//   • Strict vs relaxed attach: validate IRequireContext<T> at bind time.
//   • Delta fan-out: type-directed dispatch to IBinds<T> listeners.
//   • Frame barrier: ApplyAll() is called once before Presentation systems.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.Contexts;

namespace ZenECS.Core.Internal.Binding
{
    /// <summary>
    /// Manages binder attachment per entity, validates required contexts, dispatches
    /// component deltas, and invokes <see cref="IBinder.Apply"/> once per frame.
    /// </summary>
    internal sealed class BindingRouter : IBindingRouter
    {
        private readonly IContextRegistry _contextRegistry;
        private readonly Dictionary<Entity, List<IBinder>> _byEntity;
        private readonly int _initialEntityCapacity;
        private int _attachSeq = 0;

        /// <summary>
        /// Create a new router.
        /// </summary>
        /// <param name="contextRegistry">Lookup/registry used to resolve contexts.</param>
        /// <param name="binderBuckets">Initial dictionary buckets for entity→binders map.</param>
        /// <param name="binderEntityPerBuckets">Initial list capacity per entity.</param>
        public BindingRouter(IContextRegistry contextRegistry,
            int binderBuckets = 1024,
            int binderEntityPerBuckets = 4)
        {
            _contextRegistry = contextRegistry ?? throw new ArgumentNullException(nameof(contextRegistry));
            _byEntity = new Dictionary<Entity, List<IBinder>>(binderBuckets);
            _initialEntityCapacity = binderEntityPerBuckets;
        }

        /// <summary>
        /// Attach a <paramref name="binder"/> to the entity <paramref name="e"/> in world <paramref name="w"/>.
        /// </summary>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">Binder instance.</param>
        /// <param name="options">Attach validation mode (strict or relaxed).</param>
        /// <exception cref="ArgumentNullException">Binder is null.</exception>
        /// <exception cref="InvalidOperationException">Required contexts missing in strict mode.</exception>
        public void Attach(IWorld w, Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict)
        {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            ValidateRequiredContexts(binder, w, e, options);
            binder.Bind(w, e, _contextRegistry);
            if (!_byEntity.TryGetValue(e, out var list))
                _byEntity[e] = list = new List<IBinder>(_initialEntityCapacity);
            InsertOrdered(list, binder);
        }

        /// <summary>
        /// Detach the specified <paramref name="binder"/> from entity <paramref name="e"/>.
        /// </summary>
        public void Detach(Entity e, IBinder binder)
        {
            if (_byEntity.TryGetValue(e, out var list) && list.Remove(binder))
                binder.Unbind();
        }

        /// <summary>
        /// Detach the first binder of the given <paramref name="binderType"/> from <paramref name="e"/>.
        /// A binder matches when <c>binderType.IsAssignableFrom(b.GetType())</c>.
        /// Returns true if one was found and detached.
        /// </summary>
        /// <exception cref="ArgumentNullException">binderType is null.</exception>
        public bool Detach(Entity e, Type binderType)
        {
            if (binderType is null) throw new ArgumentNullException(nameof(binderType));
            if (!_byEntity.TryGetValue(e, out var list) || list is null || list.Count == 0)
                return false;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var b = list[i];
                if (binderType.IsAssignableFrom(b.GetType()))
                {
                    list.RemoveAt(i);
                    b.Unbind();
                    if (list.Count == 0) _byEntity.Remove(e);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Detach and unbind all binders currently attached to <paramref name="e"/>.
        /// </summary>
        public void DetachAll(Entity e)
        {
            if (_byEntity.TryGetValue(e, out var list))
            {
                foreach (var b in list) b.Unbind();
                list.Clear();
                _byEntity.Remove(e);
            }
        }

        /// <summary>
        /// World callback: invoked when <paramref name="e"/> is destroyed.
        /// </summary>
        public void OnEntityDestroyed(IWorld w, Entity e)
        {
            DetachAll(e);
        }

        /// <summary>
        /// Invoke <see cref="IBinder.Apply"/> for all binders of all tracked entities.
        /// Call this once per frame before presentation systems.
        /// </summary>
        public void ApplyAll()
        {
            foreach (var list in _byEntity.Values)
                for (int i = 0; i < list.Count; i++)
                    list[i].Apply();
        }

        /// <summary>
        /// Dispatch a component <typeparamref name="T"/> delta to binders attached to
        /// <see cref="ComponentDelta{T}.Entity"/> that implement <see cref="IBinds{T}"/>.
        /// </summary>
        public void Dispatch<T>(in ComponentDelta<T> d) where T : struct
        {
            if (!_byEntity.TryGetValue(d.Entity, out var list)) return;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                if (i >= list.Count) break; // safe-guard against mid-iteration detaches
                if (list[i] is IBinds<T> b) b.OnDelta(in d);
            }
        }

        /// <summary>
        /// Returns all binders currently attached to <paramref name="e"/> as tuples of
        /// (concrete runtime type, boxed instance). The array is a snapshot copy and
        /// safe for editor reflection/tools to enumerate without holding internal lists.
        /// </summary>
        public (Type type, object boxed)[] GetAllBinders(Entity e)
        {
            if (_byEntity.TryGetValue(e, out var list) && list != null && list.Count > 0)
            {
                var arr = new (Type, object)[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    var b = list[i];
                    // IBinder는 null이 아니어야 함(Attach 시점 보장)
                    arr[i] = (b.GetType(), (object)b);
                }
                return arr;
            }
            return Array.Empty<(Type, object)>();
        }
        
        // ---- helpers -------------------------------------------------------------

        private void InsertOrdered(List<IBinder> list, IBinder binder)
        {
            int attachOrder = ++_attachSeq;
            if (binder is IAttachOrderMarker m) m.AttachOrder = attachOrder;

            int idx = list.FindIndex(x =>
            {
                int byPriority = x.Priority.CompareTo(binder.Priority);
                if (byPriority != 0) return byPriority > 0;
                int a1 = (x is IAttachOrderMarker mm) ? mm.AttachOrder : int.MaxValue;
                return a1 > attachOrder;
            });

            if (idx < 0) list.Add(binder); else list.Insert(idx, binder);
        }

        private void ValidateRequiredContexts(IBinder binder, IWorld w, Entity e, AttachOptions options)
        {
            var need = binder.GetType().GetInterfaces()
                             .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequireContext<>))
                             .Select(t => t.GetGenericArguments()[0])
                             .Distinct().ToArray();

            foreach (var tCtx in need)
            {
                var has = _contextRegistry.Has(w, e, tCtx);
                if (!has)
                {
                    var msg = $"[BindingRouter] Missing required context {tCtx.Name} for binder {binder.GetType().Name} on {e}.";
                    if (options == AttachOptions.Strict)
                        throw new InvalidOperationException(msg);
                    // else: warn externally if desired
                }
            }
        }
    }
}
