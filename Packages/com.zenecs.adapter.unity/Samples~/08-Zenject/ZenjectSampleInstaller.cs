using UnityEngine;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenEcsAdapterUnitySamples.ZenjectSamples
{
#if ZENECS_ZENJECT
    /// <summary>
    /// Optional Zenject MonoInstaller for the Zenject sample.
    /// When ZENECS_ZENJECT is defined, this can be used to configure dependency injection bindings.
    /// </summary>
    public class ZenjectSampleInstaller : MonoInstaller
    {
        /// <summary>
        /// Installs bindings into the Zenject container.
        /// </summary>
        public override void InstallBindings()
        {
        }
    }
#else
    /// <summary>
    /// Placeholder MonoBehaviour for ZenjectSampleInstaller when ZENECS_ZENJECT is not defined.
    /// </summary>
    public class ZenjectSampleInstaller : MonoBehaviour
    {
        /// <summary>
        /// Called when the MonoBehaviour is created.
        /// </summary>
        private void Awake()
        {
        }

        /// <summary>
        /// Called when the MonoBehaviour is destroyed.
        /// </summary>
        private void OnDestroy()
        {
        }
    }
#endif
}