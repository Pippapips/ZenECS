// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Adapter.Unity — Binding
// File: Runtime/Binding/Contexts/Assets/ModelContextAsset.cs
// Purpose: Per-entity model context asset.
// Key concepts:
//   • Holds prefab reference for character/enemy model.
//   • Creates a distinct ModelContext per entity, instantiating the prefab.
//   • Lifetime: tied to entity; destroy GameObject when context is disposed (by caller).
// ──────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;
using Object = UnityEngine.Object;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    [CreateAssetMenu(
        menuName = "ZenECS/Context/Model",
        fileName = "ModelContext")]
    public sealed class ModelContextAsset : PerEntityContextAsset
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