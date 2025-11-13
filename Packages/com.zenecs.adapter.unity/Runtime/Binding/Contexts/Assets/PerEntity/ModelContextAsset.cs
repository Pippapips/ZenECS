// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Adapter.Unity — Binding
// File: Runtime/Binding/Contexts/Assets/ModelContextAsset.cs
// Purpose: Per-entity model context asset.
// Key concepts:
//   • Holds prefab reference for character/enemy model.
//   • Creates a distinct ModelContext per entity, instantiating the prefab.
//   • Lifetime: tied to entity; destroy GameObject when context is disposed (by caller).
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;
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
        public override Type ContextType => typeof(ModelContext);
        
        [Header("Model Prefab")]
        public GameObject modelPrefab = null!;
        
        /// <inheritdoc />
        public override IContext CreateContextForEntity(IWorld world, Entity e)
        {
            var boot = new EntityLinkBootstrap(LinkBootstrapPolicy.PreferModel);
            var (ww, ee, main) = boot.Run(
                world,
                existingEntity: e,
                optionalViewPrefab: modelPrefab,
                optionalViewRoot: null,
                asMain: true);
            
            var instance = main.gameObject;
            var t = instance.transform;
            var animator = instance.GetComponent<Animator>();

            return new ModelContext
            {
                Instance = instance,
                Root = t,
                Animator = animator
            };
        }
    }
}