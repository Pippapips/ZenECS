using System;
using System.Collections.Generic;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.DI
{
    public sealed class SharedContextResolver : ISharedContextResolver
    {
#if ZENECS_ZENJECT
        private readonly DiContainer _container;

        public SharedContextResolver(DiContainer container)
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
        
        public void AddContext(IContext context)
        {
        }
        public void RemoveContext(IContext context)
        {
        }
        public void RemoveAllContexts()
        {
        }
#else        
        private readonly Dictionary<Type, IContext> _contexts = new();

        public IContext Resolve(SharedContextAsset marker)
        {
            return _contexts.ContainsKey(marker.GetType()) ? _contexts[marker.GetType()] : null;
        }

        public IContext Resolve<T>() where T : IContext
        {
            return _contexts.GetValueOrDefault(typeof(T));
        }

        public void AddContext(IContext context)
        {
            if (_contexts.ContainsKey(context.GetType()))
            {
                _contexts[context.GetType()] = context;
            }
        }

        public void RemoveContext(IContext context)
        {
            if (_contexts.ContainsKey(context.GetType()))
            {
                _contexts.Remove(context.GetType());
            }
        }

        public void RemoveAllContexts()
        {
            _contexts.Clear();
        }
#endif
    }
}