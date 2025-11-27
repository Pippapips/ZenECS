#nullable enable
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Config;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity
{
    sealed class Logger : IEcsLogger
    {
        public void Info(string m)  => Debug.Log(m);
        public void Warn(string m)  => Debug.LogWarning(m);
        public void Error(string m) => Debug.LogError(m);
    }

#if !ZENECS_ZENJECT
    [DefaultExecutionOrder(-10000)]
#endif
    public sealed class EcsDriver : MonoBehaviour
    {
        public IKernel? Kernel { get; private set; }

        public IKernel CreateKernel()
        {
            if (Kernel != null) return Kernel;
            Kernel ??= new ZenECS.Core.Kernel(new KernelOptions { AutoSelectNewWorld = false }, new Logger());
            KernelLocator.Attach(Kernel);
            return Kernel;
        }
        
        void Awake()
        {
#if UNITY_2022_2_OR_NEWER
            var first = FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
            var first = FindObjectOfType<EcsDriver>(true);
#endif
            if (first != null && first != this)
            {
                Debug.LogWarning("[EcsDriver] Duplicate found. Destroying the newer one.");
                DestroyImmediate(gameObject);
                return;
            }
            
            CreateKernel();
        }

        void OnDestroy()
        {
            if (Kernel != null)
            {
                KernelLocator.Detach(Kernel);
                Kernel.Dispose();
                Kernel = null;
            }
        }

        void Update()      => Kernel?.BeginFrame(Time.deltaTime);
        void FixedUpdate() => Kernel?.FixedStep(Time.fixedDeltaTime);
        void LateUpdate()  => Kernel?.LateFrame();
    }
}