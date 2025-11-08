#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldBinderApi
    {
        public void AttachBinder(Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict)
        {
            _bindingRouter.Attach(this, e, binder, options);
        }

        public void DetachAllBinders(Entity e)
        {
            _bindingRouter.DetachAll(e);
        }

        public void DetachBinder(Entity e, IBinder binder)
        {
            _bindingRouter.Detach(e, binder);
        }
    }
}