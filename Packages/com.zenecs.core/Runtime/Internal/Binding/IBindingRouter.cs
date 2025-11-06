using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal.Binding
{
    public enum AttachOptions
    {
        Default = 0,
        Strict  = 1, // throw if required context is missing
        WarnOnly = 2 // log warning and skip attach
    }
    
    internal interface IBindingRouter
    {
        void Attach(IWorld w, Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict);
        void Detach(Entity e, IBinder binder);
        void DetachAll(Entity e);
        void OnEntityDestroyed(IWorld w, Entity e);
        void ApplyAll();
        void Dispatch<T>(in ComponentDelta<T> d) where T : struct;
    }
}