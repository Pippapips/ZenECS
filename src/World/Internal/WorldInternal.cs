using ZenECS.Core.DI;

namespace ZenECS.Core.World.Internal
{
    internal sealed class WorldInternal : IWorldInternal
    {
        private readonly ServiceHost _host;

        public WorldInternal(ServiceHost host) => _host = host;

        public T GetRequired<T>() where T : class => _host.GetRequired<T>();
        public bool TryGet<T>(out T? service) where T : class => _host.TryGet(out service);
        public bool Supports<T>() where T : class => _host.Contains<T>();
    }
}