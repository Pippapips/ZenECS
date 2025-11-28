using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
#if ZENECS_ZENJECT 
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel();
            ZenEcsUnityBridge.SharedContextResolver = new SharedContextResolver(Container);
            ZenEcsUnityBridge.SystemPresetResolver = new SystemPresetResolver(Container);
            
            Container.BindInstance(ZenEcsUnityBridge.Kernel);
            Container.Bind<ISharedContextResolver>().FromInstance(ZenEcsUnityBridge.SharedContextResolver).AsSingle();
        }
    }
#else
    [DefaultExecutionOrder(-10000)]
    public sealed class ProjectInstaller : MonoBehaviour
    {
        private void Awake()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel();
            ZenEcsUnityBridge.SharedContextResolver = new SharedContextResolver();
            ZenEcsUnityBridge.SystemPresetResolver = new SystemPresetResolver();
        }
    }
#endif
}