namespace ZenECS.Core.World.Internal
{
    internal interface IWorldInternal
    {
        T GetRequired<T>() where T : class;
        bool TryGet<T>(out T? service) where T : class;
        bool Supports<T>() where T : class;
    }
}