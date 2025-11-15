using ZenECS.Core.Binding;
using Zenject;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
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
    }
}