using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public sealed class WorldInstaller : Installer<IWorld, WorldInstaller>
    {
        readonly IWorld _w; public WorldInstaller(IWorld w){ _w=w; }
        public override void InstallBindings()
        {
            Container.Bind<IWorld>().FromInstance(_w).AsSingle();
            Container.Bind<EntityViewRegistry.Registry>().FromMethod(_ => EntityViewRegistry.For(_w)).AsSingle();
        }
    }
#else
    public sealed class WorldInstaller {}
#endif
}