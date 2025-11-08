using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    public enum AttachOptions
    {
        Default = 0,
        Strict  = 1, // throw if required context is missing
        WarnOnly = 2 // log warning and skip attach
    }
    
    public interface IWorldBinderApi
    {
        void AttachBinder(Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict);
        void DetachAllBinders(Entity e);
        void DetachBinder(Entity e, IBinder binder);
    }
}
