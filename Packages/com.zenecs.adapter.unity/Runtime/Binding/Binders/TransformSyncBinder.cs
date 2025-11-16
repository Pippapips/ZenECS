#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Binders.Implementations
{
    public class TransformSyncBinder :
        BaseBinder,
        IBind<Position>,
        IBind<Rotation>
    {
        private UnityTransformContext? _unityTransformContext;

        protected override void OnBind(Entity e, IReadOnlyList<IContext>? contexts)
        {
            var world = this.World;
            if (world == null) return;
            if (contexts == null) return;
            foreach (var context in contexts)
            {
                OnContextAttached(context);
            }
        }

        protected override void OnContextAttached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext modelContext:
                    _unityTransformContext = modelContext;
                    break;
                case SharedUIRootContext sharedUIRootContext:
                    break;
            }
        }

        protected override void OnContextDetached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext modelContext:
                    _unityTransformContext = null;
                    break;
                case SharedUIRootContext sharedUIRootContext:
                    break;
            }
        }

        protected override void OnUnbind()
        {
            _unityTransformContext = null;
        }

        public void OnDelta(in ComponentDelta<Position> delta)
        {
        }
        
        public void OnDelta(in ComponentDelta<Rotation> delta)
        {
        }
        
        protected override void OnApply(IWorld w, Entity e)
        {
            if (World == null) return;
            
            if (_unityTransformContext != null && _unityTransformContext.Root != null)
            {
                if (World.TryRead<Position>(e, out var position))
                {
                    _unityTransformContext.Root.position = position.Value;
                }
                
                if (World.TryRead<Rotation>(e, out var rotation))
                {
                    _unityTransformContext.Root.rotation = rotation.Value;
                }
                
                if (World.TryRead<Scale>(e, out var scale))
                {
                    _unityTransformContext.Root.localScale = scale.Value;
                }
            }
        }
    }
}