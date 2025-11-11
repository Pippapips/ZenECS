#nullable enable
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Abstractions.Diagnostics;

namespace ZenECS.Adapter.Unity
{
    sealed class Logger : IEcsLogger
    {
        public void Info(string m)  => Debug.Log(m);
        public void Warn(string m)  => Debug.LogWarning(m);
        public void Error(string m) => Debug.LogError(m);
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class EcsDriver : MonoBehaviour
    {
        public IKernel? Kernel { get; private set; }

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

            Kernel = new Kernel(new KernelOptions { AutoSelectNewWorld = true }, new Logger());
            KernelLocator.Attach(Kernel);
            DontDestroyOnLoad(gameObject);
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