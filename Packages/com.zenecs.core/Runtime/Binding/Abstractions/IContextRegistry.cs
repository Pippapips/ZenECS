#nullable enable

namespace ZenECS.Core.Binding
{
    public interface IContextLookup
    {
        bool TryGet<T>(WorldOld w, Entity e, out T ctx) where T : class, IContext;
        T Get<T>(WorldOld w, Entity e) where T : class, IContext;
        bool Has<T>(WorldOld w, Entity e) where T : class, IContext;
        bool Has(WorldOld w, Entity e, IContext ctx);
    }

    public interface IContextRegistry : IContextLookup
    {
        // Register / Remove (registry manages Initialize/Deinitialize & initialized flag)
        void Register(WorldOld w, Entity e, IContext ctx);
        bool Remove(WorldOld w, Entity e, IContext ctx);
        bool Remove<T>(WorldOld w, Entity e) where T : class, IContext;

        // Reinitialize (fast path or Deinit→Init fallback)
        bool Reinitialize(WorldOld w, Entity e, IContext ctx);
        bool Reinitialize<T>(WorldOld w, Entity e) where T : class, IContext;

        // State / cleanup
        bool IsInitialized(WorldOld w, Entity e, IContext ctx);
        bool IsInitialized<T>(WorldOld w, Entity e) where T : class, IContext;
        void Clear(WorldOld w, Entity e);
        void ClearAll();
    }
}