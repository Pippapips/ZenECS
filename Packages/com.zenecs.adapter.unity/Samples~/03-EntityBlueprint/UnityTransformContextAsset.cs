using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;
using Object = UnityEngine.Object;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    [CreateAssetMenu(
        menuName = "ZenECS Samples/Context/PerEntity/UnityTransformContext",
        fileName = "UnityTransformContext")]
    public sealed class UnityTransformContextAsset : PerEntityContextAsset
    {
        [SerializeField] private GameObject _modelPrefab;
        
        public override Type ContextType => typeof(UnityTransformContext);
        
        /// <inheritdoc />
        public override IContext Create()
        {
            return new UnityTransformContext(_modelPrefab);
        }
    }
}