using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
#if ZENECS_ZENJECT
    using Zenject;

    public interface IViewLinkFactory : IFactory<SpawnArgs, EntityLink> { }

    public sealed class ViewLinkFactory : IViewLinkFactory
    {
        readonly DiContainer Container;

        public ViewLinkFactory(DiContainer container)
        {
            Container = container;
        }

        public EntityLink Create(SpawnArgs spawnArgs)
        {
            var entityLink = Container.InstantiatePrefabForComponent<EntityLink>(spawnArgs.Prefab, spawnArgs.Parent, null);
            entityLink.Attach(spawnArgs.World, spawnArgs.Entity, spawnArgs.Key.Kind == ViewKind.Main ? ViewKey.Main() : ViewKey.Sub(spawnArgs.Key.Index));
            return entityLink;
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