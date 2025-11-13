#nullable enable
using System;
using ZenECS.Core.Binding;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    [CreateAssetMenu(
        menuName = "ZenECS/Context Marker/UI Root",
        fileName = "UIRootContextMarker")]
    public sealed class UIRootContextMarkerAsset : SharedContextMarkerAsset
    {
        public override Type ContextType => typeof(UIRootContext);
    }
    
    
    // using ZenECS.Core.Binding;
    //
    // public interface ISharedContextResolver
    // {
    //     /// <summary>
    //     /// SharedContextMarkerAsset에 대응하는 IContext 인스턴스를 리턴한다.
    //     /// 없으면 null.
    //     /// </summary>
    //     IContext Resolve(SharedContextMarkerAsset marker);
    // }
    //
    // public sealed class WorldSharedContextResolver : ISharedContextResolver
    // {
    //     readonly IWorldServices _services; // world.Services 같은 거
    //
    //     public WorldSharedContextResolver(IWorldServices services)
    //     {
    //         _services = services;
    //     }
    //
    //     public IContext Resolve(SharedContextMarkerAsset marker)
    //     {
    //         var t = marker.ContextType;
    //         // world.Services.Get(UIRootContext) 같이 꺼내는 느낌
    //         return _services.Get(t) as IContext;
    //     }
    // }
}