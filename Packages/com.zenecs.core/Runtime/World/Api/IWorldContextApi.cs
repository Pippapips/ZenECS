using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    public interface IWorldContextApi
    {
        void RegisterContext(Entity e, IContext ctx);
    }
}
