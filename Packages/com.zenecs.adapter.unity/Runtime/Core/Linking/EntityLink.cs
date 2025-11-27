using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    [DisallowMultipleComponent]
    public sealed class EntityLink : MonoBehaviour
    {
        public IWorld World { get; private set; }
        public Entity Entity { get; private set; }

        public bool IsAlive => World != null && Entity.Id >= 0 && World.IsAlive(Entity);

        public void Attach(IWorld w, in Entity e)
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this);

            World = w; Entity = e;

            if (World != null)
                EntityViewRegistry.For(World).Register(Entity, this);
        }

        public void Detach()
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this);

            World = null; Entity = default;
        }

        void OnDestroy()
        {
            Detach();
        }
    }

    public static class EntityLinkExtensions
    {
        public static EntityLink CreateEntityLink(this GameObject go, IWorld w, in Entity e)
        {
#if UNITY_EDITOR
            if (!go) return null;
            var link = go.GetComponent<EntityLink>() ?? go.AddComponent<EntityLink>();
            link.Attach(w, e);
            return link;
#else
            return null;
#endif
        }

        public static void DestroyEntityLink(this GameObject go)
        {
#if UNITY_EDITOR
            if (!go) return;
            var link = go.GetComponent<EntityLink>();
            if (link == null) return;
            link.Detach();
#endif
        }
    }
}