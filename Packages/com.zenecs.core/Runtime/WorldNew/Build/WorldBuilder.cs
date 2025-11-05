using ZenECS.Core.DI;
using ZenECS.Core.World.Internal;

namespace ZenECS.Core.World.Build
{
    public static class WorldBuilder
    {
        public static IWorld BuildDefault()
        {
            // per-world scope
            var scope = new ServiceHost()
                // .RegisterSingleton<IEntityStore>(new InMemoryEntityStore(), takeOwnership: true)
                // .RegisterSingleton<IEntityGc>(new InMemoryEntityStore(), takeOwnership: false) // 같은 인스턴스 재사용시 이렇게 주입 가능
                .Freeze();

            return new World(new WorldInternal(scope));
        }
    }
}