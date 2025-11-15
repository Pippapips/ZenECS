#nullable enable
using System;
using ZenECS.Core.Binding;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    [CreateAssetMenu(
        menuName = "ZenECS/Context Marker/UI Root",
        fileName = "SharedUIRootContextMarker")]
    public sealed class SharedUIRootContextMarkerAsset : SharedContextMarkerAsset
    {
        public override Type ContextType => typeof(SharedUIRootContext);
    }
}