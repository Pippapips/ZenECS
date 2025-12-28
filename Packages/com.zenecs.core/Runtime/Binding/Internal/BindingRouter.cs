// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: BindingRouter.cs
// Purpose: Manage binders per-entity, validate required contexts, dispatch
//          component deltas, and call Apply() once per frame.
// Key concepts:
//   • Per-entity lists: ordered by Priority, then stable attach-sequence.
//   • Delta fan-out: type-directed dispatch to IBind<T> listeners.
//   • Frame barrier: ApplyAll() is called once before Presentation systems.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding.Internal
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
        public void Attach(IWorld w, Entity e, IBinder? binder)
        {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            binder.Bind(w, e, _contextRegistry);
            if (!_byEntity.TryGetValue(e, out var list))
                _byEntity[e] = list = new List<IBinder>(_initialEntityCapacity);
            InsertOrdered(list, binder);

            var components = w.GetAllComponents(e);
            foreach (var (type, boxed) in components)
            {
                w.SnapshotComponentTyped(e, type);
            }
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
        public void ApplyAll(IWorld w)
        {
            foreach (var kv in _byEntity)
            {
                var e = kv.Key;
                var list = kv.Value;
                foreach (var binder in list)
                    binder.Apply(w, e);
            }
        }

        /// <summary>
        /// Dispatch a component <typeparamref name="T"/> delta to binders attached to
        /// <see cref="ComponentDelta{T}.Entity"/> that implement <see cref="IBind{T}"/>.
        /// </summary>
        public void Dispatch<T>(in ComponentDelta<T> d) where T : struct
        {
            if (!_byEntity.TryGetValue(d.Entity, out var list)) return;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                if (i >= list.Count) break; // safe-guard against mid-iteration detaches
                if (list[i] is IBind<T> b) b.OnDelta(in d);
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
                    // IBinder must be non-null (guaranteed at Attach time).
                    arr[i] = (b.GetType(), (object)b);
                }
                return arr;
            }
            return Array.Empty<(Type, object)>();
        }
        
        public IReadOnlyList<IBinder>? GetAllBinderList(Entity e)
        {
            if (_byEntity.TryGetValue(e, out var list) && list != null && list.Count > 0)
            {
                return list;
            }
            return null;
        }
        
        /// <summary>
        /// Returns true if entity <paramref name="e"/> has at least one binder
        /// assignable to <typeparamref name="T"/>.
        /// A binder matches when <c>typeof(T).IsAssignableFrom(binder.GetType())</c>.
        /// </summary>
        public bool Has<T>(Entity e) where T : class, IBinder
        {
            if (!_byEntity.TryGetValue(e, out var list) || list is null || list.Count == 0)
                return false;

            var targetType = typeof(T);
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null) continue;

                if (targetType.IsAssignableFrom(b.GetType()))
                    return true;
            }

            return false;
        }
        
        // ---- helpers -------------------------------------------------------------

        private void InsertOrdered(List<IBinder> list, IBinder binder)
        {
            int attachOrder = ++_attachSeq;
            if (binder is IAttachOrderMarker m) m.SetAttachOrder(attachOrder);

            int idx = list.FindIndex(x =>
            {
                int byPriority = x.ApplyOrder.CompareTo(binder.ApplyOrder);
                if (byPriority != 0) return byPriority > 0;
                int a1 = (x is IAttachOrderMarker mm) ? mm.AttachOrder : int.MaxValue;
                return a1 > attachOrder;
            });

            if (idx < 0) list.Add(binder); else list.Insert(idx, binder);
        }
    }
}
