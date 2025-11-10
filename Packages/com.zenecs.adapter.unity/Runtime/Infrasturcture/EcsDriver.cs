#nullable enable
using System;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Abstractions.Diagnostics;

namespace ZenECS.Adapter.Unity
{
    class Logger : IEcsLogger
    {
        public void Info(string message)
        {
            Debug.Log(message);
        }
        public void Warn(string message)
        {
            Debug.LogWarning(message);
        }
        public void Error(string message)
        {
            Debug.LogError(message);
        }
    }
    
    /// <summary>
    /// Unity ↔ ZenECS 브릿지. 씬에 한 개 두고 커널을 부팅/드라이브/정리한다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class EcsDriver : MonoBehaviour
    {
        public IKernel? Kernel { get; private set; }

        private void Awake()
        {
            Kernel = new Kernel(new KernelOptions()
            {
                AutoSelectNewWorld = true
            }, new Logger());
        }

        private void Start()
        {
        }

        private void OnDestroy()
        {
            Kernel?.Dispose();
            Kernel = null;
        }

        private void Update()
        {
            Kernel?.BeginFrame(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            Kernel?.FixedStep(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            Kernel?.LateFrame();
        }
    }
}
