namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // SignalBusInstaller.Install(Container); // 이벤트 버스 별도 사용 시
            Container.Bind<IViewLinkFactory>().To<ViewLinkFactory>().AsSingle();
        }
    }
#else
    public sealed class ProjectInstaller : UnityEngine.MonoBehaviour {}
#endif
}