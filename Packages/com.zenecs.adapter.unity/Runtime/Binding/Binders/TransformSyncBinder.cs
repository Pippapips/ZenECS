#nullable enable
using System.Collections.Generic;
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
            if (context is ModelContext modelContext)
            {
                _modelContext = modelContext;
            }
        }

        protected override void OnContextDetached(IContext context)
        {
            if (context is ModelContext modelContext)
            {
                _modelContext = null;
            }
        }

        protected override void OnUnbind()
        {
            _modelContext = null;
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
        }
    }
}