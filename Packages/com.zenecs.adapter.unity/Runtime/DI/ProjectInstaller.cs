using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Install;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel();
            ZenEcsUnityBridge.SharedContextResolver = new WorldSharedContextResolver(Container);
            ZenEcsUnityBridge.SystemPresetResolver = new SystemPresetResolver(Container);
            
            Container.BindInstance(ZenEcsUnityBridge.Kernel);
            Container.Bind<ISharedContextResolver>().FromInstance(ZenEcsUnityBridge.SharedContextResolver).AsSingle();
        }
    }
#else
    public sealed class ProjectInstaller : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            KernelLocator.CreateEcsDriverWithKernel();
        }
    }
#endif
}