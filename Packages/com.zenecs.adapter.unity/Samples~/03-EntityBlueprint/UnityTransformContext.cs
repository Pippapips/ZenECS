#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    /// <summary>
    /// Entity-owned model context wrapping a Unity GameObject instance.
    /// </summary>
    public sealed class UnityTransformContext : IContext, IContextReinitialize
    {
        /// <summary>The instantiated GameObject for this entity's model.</summary>
        public GameObject? Instance { get; set; } = null!;

        /// <summary>Cached root transform for fast access.</summary>
        public Transform? Root { get; set; } = null!;

        private GameObject _prefab;

        public UnityTransformContext(GameObject prefab)
        {
            _prefab = prefab;
        }
        
        public void Initialize(IWorld w, Entity e, IContextLookup l)
        {
            Instance = Object.Instantiate(_prefab);
            Root = Instance.transform;

            Instance.CreateEntityLink(w, e);
        }
        
        public void Deinitialize(IWorld w, Entity e)
        {
            Instance?.DestroyEntityLink();
            
            Object.Destroy(Instance);
            Instance = null;
            Root = null;
        }
        
        public void Reinitialize(IWorld w, Entity e, IContextLookup l)
        {
            Deinitialize(w, e);
            Initialize(w, e, l);
        }
    }
}