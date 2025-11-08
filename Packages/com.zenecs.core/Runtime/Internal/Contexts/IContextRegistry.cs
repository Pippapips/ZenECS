#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal.Contexts
{
    internal interface IContextRegistry : IContextLookup
    {
        // Register / Remove (registry manages Initialize/Deinitialize & initialized flag)
        void Register(IWorld w, Entity e, IContext ctx);
        bool Remove(IWorld w, Entity e, IContext ctx);
        bool Remove<T>(IWorld w, Entity e) where T : class, IContext;

        // Reinitialize (fast path or Deinit→Init fallback)
        bool Reinitialize(IWorld w, Entity e, IContext ctx);
        bool Reinitialize<T>(IWorld w, Entity e) where T : class, IContext;

        // State / cleanup
        bool IsInitialized(IWorld w, Entity e, IContext ctx);
        bool IsInitialized<T>(IWorld w, Entity e) where T : class, IContext;
        void Clear(IWorld w, Entity e);
        void ClearAll();
    }
}