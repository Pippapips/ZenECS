using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    [DisallowMultipleComponent]
    public sealed class EntityLink : MonoBehaviour
    {
        public IWorld World { get; private set; }
        public Entity Entity { get; private set; }

        [SerializeField] private ViewKey _key = ViewKey.Sub(0);
        public ViewKey Key => _key;
        public bool IsAlive => World != null && Entity.Id >= 0 && World.IsAlive(Entity);

        public void Attach(IWorld w, in Entity e, ViewKey key)
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this, _key);

            World = w; Entity = e; _key = key;

            if (World != null)
                EntityViewRegistry.For(World).Register(Entity, this, _key);
        }

        public void Detach()
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this, _key);

            World = null; Entity = default; _key = ViewKey.Sub(0);
        }

        internal void OverrideKey(ViewKey k) => _key = k;

        void OnDestroy()
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this, _key);
        }
    }
}