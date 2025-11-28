using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    public interface ISharedContextResolver
    {
        IContext Resolve(SharedContextAsset marker);
        IContext Resolve<T>() where T : IContext;
        void AddContext(IContext context);
        void RemoveContext(IContext context);
        void RemoveAllContexts();
    }
}