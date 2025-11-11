using UnityEngine;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public sealed class ViewLinkPool : MonoMemoryPool<SpawnArgs, EntityLink>
    {
        protected override void OnSpawned(SpawnArgs a, EntityLink link)
        {
            base.OnSpawned(a, link);
            if (a.Parent) link.transform.SetParent(a.Parent, false);
            link.Attach(a.World, a.Entity, a.Key.Kind==ViewKind.Main?ViewKey.Main():ViewKey.Sub(a.Key.Index));
        }
        protected override void OnDespawned(EntityLink link)
        {
            if (link) link.Detach(); if (link) link.gameObject.SetActive(false);
            base.OnDespawned(link);
        }
    }
#else
    public sealed class ViewLinkPool
    {
        readonly GameObject _prefab;
        public ViewLinkPool(GameObject prefab){ _prefab=prefab; }
        public EntityLink Spawn(SpawnArgs a)
        {
            var p = a.Prefab ? a.Prefab : _prefab;
            var go = Object.Instantiate(p, a.Parent, false);
            var link = go.GetComponent<EntityLink>() ?? go.AddComponent<EntityLink>();
            link.Attach(a.World, a.Entity, a.Key.Kind==ViewKind.Main?ViewKey.Main():ViewKey.Sub(a.Key.Index));
            return link;
        }
        public void Despawn(EntityLink link){ if(!link) return; link.Detach(); Object.Destroy(link.gameObject); }
    }
#endif
}