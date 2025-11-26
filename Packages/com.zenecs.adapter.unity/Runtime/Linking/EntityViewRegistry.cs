using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    public static class EntityViewRegistry
    {
        private static readonly ConditionalWeakTable<IWorld, Registry> _byWorld = new();
        public static Registry For(IWorld w) => _byWorld.GetValue(w, _ => new Registry());

        public sealed class Registry
        {
            private sealed class Bucket
            {
                public EntityLink Link;
            }
            private readonly Dictionary<Entity, Bucket> _map = new();

            private Bucket GetOrCreate(Entity e) => _map.TryGetValue(e, out var b) ? b : (_map[e] = new Bucket());

            public void Register(Entity e, EntityLink link)
            {
                var b = GetOrCreate(e);
                b.Link = link;
            }

            public void Unregister(Entity e, EntityLink link)
            {
                if (!_map.TryGetValue(e, out var b)) return;
                if (b.Link == link) b.Link = null;
                if (b.Link == null) _map.Remove(e);
            }

            public bool TryGet(Entity e, out EntityLink link)
            {
                link = null;
                if (_map.TryGetValue(e, out var b) && b.Link && b.Link.IsAlive) { link = b.Link; return true; }
                return false;
            }

            public void Callback(Entity e, System.Action<EntityLink> act)
            {
                if (!_map.TryGetValue(e, out var b)) return;
                if (b.Link && b.Link.IsAlive) act(b.Link);
            }

            public bool HasLink(Entity e) => _map.TryGetValue(e, out var b) && (b.Link);
        }
    }
}