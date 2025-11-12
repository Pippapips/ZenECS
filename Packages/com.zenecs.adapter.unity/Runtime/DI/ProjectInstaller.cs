using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            var ecsDriver = KernelLocator.CreateEcsDriver();
            ecsDriver.CreateKernel();
            Container.BindInstance(ecsDriver.Kernel);
            Container.Bind<IViewLinkFactory>().To<ViewLinkFactory>().AsSingle();
        }
    }
#else
    public sealed class ProjectInstaller : UnityEngine.MonoBehaviour {}
#endif
}