using ZenECS.Core;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;

    public sealed class KernelInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            //Container.Bind<IKernel>().To<ZenECS.Core.Kernel>().AsSingle();
        }
    }
#else
    public sealed class KernelInstaller : UnityEngine.MonoBehaviour {}
#endif
}