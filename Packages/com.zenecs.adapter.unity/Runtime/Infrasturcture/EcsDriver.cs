#nullable enable
using System;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Abstractions.Diagnostics;

namespace ZenECS.Adapter.Unity
{
    class Logger : IEcsLogger
    {
        public void Info(string message)  => Debug.Log(message);
        public void Warn(string message)  => Debug.LogWarning(message);
        public void Error(string message) => Debug.LogError(message);
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class EcsDriver : MonoBehaviour
    {
        public IKernel? Kernel { get; private set; }

        private void Awake()
        {
            // 중복 드라이버 방지(최초만 유지)
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

            // 생성
            Kernel = new Kernel(new KernelOptions() { AutoSelectNewWorld = true }, new Logger());

            // 전역 등록 ★
            KernelLocator.Attach(Kernel);

            // 싱글턴 수명 유지
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Kernel != null)
            {
                // 전역 제거 ★
                KernelLocator.Detach(Kernel);

                Kernel.Dispose();
                Kernel = null;
            }
        }

        private void Update()      => Kernel?.BeginFrame(Time.deltaTime);
        private void FixedUpdate() => Kernel?.FixedStep(Time.fixedDeltaTime);
        private void LateUpdate()  => Kernel?.LateFrame();
    }
}