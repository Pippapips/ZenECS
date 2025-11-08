#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldContextApi
    {
        public void RegisterContext(Entity e, IContext ctx)
        {
            _contextRegistry.Register(this, e, ctx);
        }
    }
}