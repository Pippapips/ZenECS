#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.Contexts;

namespace ZenECS.Core.Internal.Binding
{
    /// <summary>Manages binders per entity, dispatches deltas, calls Apply() once per frame.</summary>
    internal sealed class BindingRouter : IBindingRouter 
    {
        private readonly IContextRegistry _contextRegistry;
        private readonly Dictionary<Entity, List<IBinder>> _byEntity;
        private readonly int _initialEntityCapacity;
        private int _attachSeq = 0;

        public BindingRouter(IContextRegistry contextRegistry,
            int binderBuckets = 1024,
            int binderEntityPerBuckets = 4)
        {
            _contextRegistry = contextRegistry ?? throw new ArgumentNullException(nameof(contextRegistry));
            _byEntity = new Dictionary<Entity, List<IBinder>>(binderBuckets);
            _initialEntityCapacity = binderEntityPerBuckets;
        }

        public void Attach(IWorld w, Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict)
        {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            ValidateRequiredContexts(binder, w, e, options);
            binder.Bind(w, e, _contextRegistry);
            if (!_byEntity.TryGetValue(e, out var list))
                _byEntity[e] = list = new List<IBinder>(_initialEntityCapacity);
            InsertOrdered(list, binder);
        }

        public void Detach(Entity e, IBinder binder)
        {
            if (_byEntity.TryGetValue(e, out var list) && list.Remove(binder))
                binder.Unbind();
        }

        public void DetachAll(Entity e)
        {
            if (_byEntity.TryGetValue(e, out var list))
            {
                foreach (var b in list) b.Unbind();
                list.Clear();
                _byEntity.Remove(e);
            }
        }

        public void OnEntityDestroyed(IWorld w, Entity e)
        {
            DetachAll(e);
        }

        public void ApplyAll()
        {
            foreach (var list in _byEntity.Values)
                for (int i = 0; i < list.Count; i++)
                    list[i].Apply();
        }

        public void Dispatch<T>(in ComponentDelta<T> d) where T : struct
        {
            if (!_byEntity.TryGetValue(d.Entity, out var list)) return;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                if (i >= list.Count) break;
                if (list[i] is IBinds<T> b) b.OnDelta(in d);
            }
        }

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
                var has = HasContextDynamic(tCtx, w, e);
                if (!has)
                {
                    var msg = $"[BindingRouter] Missing required context {tCtx.Name} for binder {binder.GetType().Name} on {e}.";
                    if (options == AttachOptions.Strict)
                    {
                        throw new InvalidOperationException(msg);
                    }
                    else
                    {
                        // just warning
                    }
                }
            }
        }

        private bool HasContextDynamic(Type ctxType, IWorld w, Entity e)
        {
            var mHas = _contextRegistry.GetType().GetMethods()
                .FirstOrDefault(mi => mi.IsGenericMethodDefinition && mi.Name == "Has"
                                      && mi.GetParameters().Length == 2
                                      && mi.GetParameters()[0].ParameterType == typeof(IWorld)
                                      && mi.GetParameters()[1].ParameterType == typeof(Entity));
            if (mHas != null)
                return (bool)mHas.MakeGenericMethod(ctxType).Invoke(_contextRegistry, new object[] { w, e });

            return false;
        }
    }
}
