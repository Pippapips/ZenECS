using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;
    public interface IViewLinkFactory : IFactory<SpawnArgs, EntityLink> {}
    public sealed class ViewLinkFactory : IViewLinkFactory
    {
        readonly DiContainer _c; public ViewLinkFactory(DiContainer c){_c=c;}
        public EntityLink Create(SpawnArgs a)
        {
            var link = _c.InstantiatePrefabForComponent<EntityLink>(a.Prefab, a.Parent, null);
            link.Attach(a.World, a.Entity, a.Key.Kind==ViewKind.Main?ViewKey.Main():ViewKey.Sub(a.Key.Index));
            return link;
        }
    }
#else
    public interface IViewLinkFactory { EntityLink Create(SpawnArgs a); }
    public sealed class ViewLinkFactory : IViewLinkFactory
    {
        public EntityLink Create(SpawnArgs a)
        {
            var go = Object.Instantiate(a.Prefab, a.Parent, false);
            var link = go.GetComponent<EntityLink>() ?? go.AddComponent<EntityLink>();
            link.Attach(a.World, a.Entity, a.Key.Kind==ViewKind.Main?ViewKey.Main():ViewKey.Sub(a.Key.Index));
            return link;
        }
    }
#endif
}