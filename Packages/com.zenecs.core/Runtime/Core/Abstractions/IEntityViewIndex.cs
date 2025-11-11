namespace ZenECS.Core
{
    public interface IEntityViewIndex
    {
        bool HasAny(in Entity e);
        bool TryGet(in Entity e, string key, out object viewId);
    }
}