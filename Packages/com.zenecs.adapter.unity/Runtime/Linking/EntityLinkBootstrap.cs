using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    public enum LinkBootstrapPolicy { PreferModel, PreferView, ViewIfPresent }

    public sealed class EntityLinkBootstrap
    {
        private readonly LinkBootstrapPolicy _policy;
        public EntityLinkBootstrap(LinkBootstrapPolicy policy = LinkBootstrapPolicy.ViewIfPresent) { _policy = policy; }

        public (IWorld w, Entity e, EntityLink main)
            Run(IWorld world, Entity? existingEntity, GameObject optionalViewPrefab, Transform optionalViewRoot, bool asMain = true, int subIndex = 0)
        {
            var e = existingEntity ?? world.SpawnEntity();

            EntityLink found = null;
            if (_policy is LinkBootstrapPolicy.PreferView or LinkBootstrapPolicy.ViewIfPresent)
                found = FindFirstLink(optionalViewRoot, world, e);

            if (found)
            {
                found.Attach(world, e, asMain ? ViewKey.Main() : ViewKey.Sub(subIndex));
                return (world, e, ResolveMain(world, e));
            }

            if (_policy != LinkBootstrapPolicy.PreferView && optionalViewPrefab)
            {
                var go = Object.Instantiate(optionalViewPrefab, optionalViewRoot, false);
                if (asMain)
                {
                    go.EnsureMain(world, e);
                }
                else
                {
                    go.EnsureSub(world, e, subIndex);
                }
            }

            return (world, e, ResolveMain(world, e));
        }

        private static EntityLink ResolveMain(IWorld w, in Entity e)
        {
            var reg = EntityViewRegistry.For(w);
            if (reg.TryGetMain(e, out var m)) return m;
            if (reg.TryGetPrimary(e, out var p)) return p;
            return null;
        }

        private static EntityLink FindFirstLink(Transform root, IWorld w, in Entity e)
        {
#if UNITY_2022_2_OR_NEWER
            if (!root)
            {
                foreach (var l in Object.FindObjectsByType<EntityLink>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (l && (l.World == null || l.Entity.Id < 0 || (l.World == w && l.Entity.Equals(e))))
                        return l;
                return null;
            }
            foreach (var l in root.GetComponentsInChildren<EntityLink>(true))
                if (l && (l.World == null || l.Entity.Id < 0 || (l.World == w && l.Entity.Equals(e))))
                    return l;
            return null;
#else
            var links = root ? root.GetComponentsInChildren<EntityLink>(true)
                             : Object.FindObjectsOfType<EntityLink>(true);
            foreach (var l in links)
                if (l && (l.World == null || l.Entity.Id < 0 || (l.World == w && l.Entity.Equals(e))))
                    return l;
            return null;
#endif
        }
    }
}
