#nullable enable
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

        protected override void OnBind(Entity e)
        {
            var world = this.World;
            if (world != null && Contexts.TryGet<ModelContext>(world, this.Entity, out _modelContext))
            {
                InitView(this.Entity, world);
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
        
        public override void Apply(IWorld w, Entity e)
        {
            if (World == null) return;
            
            if (_modelContext != null)
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

        private void InitView(Entity e, IWorld world)
        {
            if (_modelContext == null)
                return;
        }
        
        private void CleanupView()
        {
        }
    }
}