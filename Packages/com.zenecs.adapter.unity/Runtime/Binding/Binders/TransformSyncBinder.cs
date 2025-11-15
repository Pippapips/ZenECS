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
        private ModelContext? _modelContext;
        private SharedUIRootContext? _sharedUIRootContext;

        private float _sinceSharedContextAttached;

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
                case ModelContext modelContext:
                    _modelContext = modelContext;
                    break;
                case SharedUIRootContext sharedUIRootContext:
                    _sharedUIRootContext = sharedUIRootContext;
                    _sinceSharedContextAttached = 0;
                    break;
            }
        }

        protected override void OnContextDetached(IContext context)
        {
            switch (context)
            {
                case ModelContext modelContext:
                    _modelContext = null;
                    break;
                case SharedUIRootContext sharedUIRootContext:
                    if (_sharedUIRootContext)
                    {
                        _sharedUIRootContext.Text.text = $"Shared UIRoot Detached";
                    }
                    _sharedUIRootContext = null;
                    break;
            }
        }

        protected override void OnUnbind()
        {
            _modelContext = null;
            if (_sharedUIRootContext)
            {
                _sharedUIRootContext.Text.text = $"Shared UIRoot Unbound";
            }
            _sharedUIRootContext = null;
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
            
            if (_modelContext != null && _modelContext.Root != null)
            {
                if (World.TryRead<Position>(e, out var position))
                {
                    _modelContext.Root.position = position.Value;
                }
                
                if (World.TryRead<Rotation>(e, out var rotation))
                {
                    _modelContext.Root.rotation = rotation.Value;
                }
            }

            if (_sharedUIRootContext != null)
            {
                _sinceSharedContextAttached += Time.deltaTime;
                _sharedUIRootContext.Text.text = $"Since shared context attached in {_sinceSharedContextAttached:0} seconds";
            }
        }
    }
}