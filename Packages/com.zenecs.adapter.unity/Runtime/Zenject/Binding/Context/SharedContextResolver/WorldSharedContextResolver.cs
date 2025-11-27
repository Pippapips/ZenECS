using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.DI
{
    public sealed class WorldSharedContextResolver : ISharedContextResolver
    {
#if ZENECS_ZENJECT
        private readonly DiContainer _container;
        
        public WorldSharedContextResolver(DiContainer container)
        {
            _container = container;
        }
#endif
    
        public IContext Resolve(SharedContextAsset marker)
        {
#if ZENECS_ZENJECT
            var t = marker.ContextType;
            var resolved = _container.Resolve(t);
            return _container.Resolve(t) as IContext;
#else
            return null;
#endif
        }

        public IContext Resolve<T>() where T : IContext
        {
#if ZENECS_ZENJECT
            return  _container.Resolve<T>();
#else
            return null;
#endif
        }
    }
}