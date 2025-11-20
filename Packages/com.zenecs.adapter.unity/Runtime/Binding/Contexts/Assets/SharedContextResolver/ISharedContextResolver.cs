using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    public interface ISharedContextResolver
    {
        /// <summary>
        /// SharedContextMarkerAsset에 대응하는 IContext 인스턴스를 리턴한다.
        /// 없으면 null.
        /// </summary>
        IContext Resolve(SharedContextAsset marker);
        IContext Resolve<T>() where T : IContext;
    }
}