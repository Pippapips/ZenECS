using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;
using Zenject;

namespace ZenECS.Adapter.Unity.DI
{
    public sealed class WorldSharedContextResolver : ISharedContextResolver
    {
        private readonly DiContainer _container;
        
        public WorldSharedContextResolver(DiContainer container)
        {
            _container = container;
        }
    
        public IContext Resolve(SharedContextAsset marker)
        {
            var t = marker.ContextType;
            var resolved = _container.Resolve(t);
            return _container.Resolve(t) as IContext;
        }

        public IContext Resolve<T>() where T : IContext
        {
            return  _container.Resolve<T>();
        }
    }
}