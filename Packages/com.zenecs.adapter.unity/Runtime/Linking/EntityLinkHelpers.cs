using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    public static class EntityLinkHelpers
    {
        public static EntityLink EnsureMain(this GameObject go, IWorld w, in Entity e)
            => Ensure(go, w, e, ViewKey.Main());

        public static EntityLink EnsureSub(this GameObject go, IWorld w, in Entity e, int index = 0)
            => Ensure(go, w, e, ViewKey.Sub(index));

        private static EntityLink Ensure(GameObject go, IWorld w, in Entity e, ViewKey key)
        {
            if (!go) return null;
            var link = go.GetComponent<EntityLink>() ?? go.AddComponent<EntityLink>();
            link.Attach(w, e, key);
            return link;
        }
    }
}