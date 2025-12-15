using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Binders.Assets;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;
using Object = UnityEngine.Object;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    [CreateAssetMenu(
        menuName = "ZenECS Samples/Binder/UnityTransformSyncBinder",
        fileName = "UnityTransformSyncBinder")]
    public sealed class UnityTransformSyncBinderAsset : BinderAsset
    {
        public override Type BinderType => typeof(UnityTransformSyncBinder);
        
        public override IBinder Create()
        {
            return new UnityTransformSyncBinder();
        }
    }
}