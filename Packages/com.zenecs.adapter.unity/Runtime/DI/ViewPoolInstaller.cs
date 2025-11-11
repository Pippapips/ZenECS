using UnityEngine;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;

    public sealed class ViewPoolInstaller : MonoInstaller
    {
        [SerializeField] private EntityLink _prefab;
        [SerializeField] private int _initialSize = 16;

        public override void InstallBindings()
        {
            Container.BindMemoryPool<EntityLink, ViewLinkPool>()
                .WithInitialSize(_initialSize)
                .FromComponentInNewPrefab(_prefab.gameObject)
                .UnderTransformGroup("ViewPool");
        }
    }
#else
    public sealed class ViewPoolInstaller : MonoBehaviour
    {
        [SerializeField] private EntityLink _prefab;
        public ViewLinkPool RuntimePool { get; private set; }
        void Awake(){ if(_prefab) RuntimePool = new ViewLinkPool(_prefab.gameObject); }
    }
#endif
}