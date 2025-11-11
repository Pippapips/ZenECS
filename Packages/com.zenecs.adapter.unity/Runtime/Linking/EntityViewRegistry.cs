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
                public EntityLink Main;
                public readonly SortedList<int, HashSet<EntityLink>> Subs = new();
            }
            private readonly Dictionary<Entity, Bucket> _map = new();

            private Bucket GetOrCreate(Entity e) => _map.TryGetValue(e, out var b) ? b : (_map[e] = new Bucket());

            public void Register(Entity e, EntityLink link, ViewKey key)
            {
                var b = GetOrCreate(e);
                if (key.Kind == ViewKind.Main)
                {
                    if (b.Main && b.Main != link) PromoteToSubInternal(e, b.Main, 999);
                    b.Main = link;
                }
                else
                {
                    if (!b.Subs.TryGetValue(key.Index, out var set))
                        set = b.Subs[key.Index] = new HashSet<EntityLink>();
                    set.Add(link);
                }
            }

            public void Unregister(Entity e, EntityLink link, ViewKey key)
            {
                if (!_map.TryGetValue(e, out var b)) return;

                if (key.Kind == ViewKind.Main) { if (b.Main == link) b.Main = null; }
                else if (b.Subs.TryGetValue(key.Index, out var set))
                {
                    set.Remove(link);
                    if (set.Count == 0) b.Subs.Remove(key.Index);
                }

                if (b.Main == null && b.Subs.Count == 0) _map.Remove(e);
            }

            public bool TryGetMain(Entity e, out EntityLink link)
            {
                link = null;
                if (_map.TryGetValue(e, out var b) && b.Main && b.Main.IsAlive) { link = b.Main; return true; }
                return false;
            }

            public bool TryGetPrimary(Entity e, out EntityLink link)
            {
                if (TryGetMain(e, out link)) return true;
                if (_map.TryGetValue(e, out var b))
                    foreach (var kv in b.Subs)
                        foreach (var l in kv.Value)
                            if (l && l.IsAlive) { link = l; return true; }
                link = null; return false;
            }

            public void GetAllSubs(Entity e, List<EntityLink> outList)
            {
                outList.Clear();
                if (!_map.TryGetValue(e, out var b)) return;
                foreach (var kv in b.Subs) foreach (var l in kv.Value) if (l && l.IsAlive) outList.Add(l);
            }

            public void BroadcastAll(Entity e, System.Action<EntityLink> act)
            {
                if (!_map.TryGetValue(e, out var b)) return;
                if (b.Main && b.Main.IsAlive) act(b.Main);
                foreach (var kv in b.Subs) foreach (var l in kv.Value) if (l && l.IsAlive) act(l);
            }

            public void PromoteToMain(Entity e, EntityLink link)
            {
                var b = GetOrCreate(e);
                if (b.Main == link) return;
                if (b.Main) PromoteToSubInternal(e, b.Main, 999);

                if (TryFindSubKey(e, link, out var idx))
                {
                    var set = b.Subs[idx]; set.Remove(link);
                    if (set.Count == 0) b.Subs.Remove(idx);
                }
                b.Main = link; link.OverrideKey(ViewKey.Main());
            }

            public void PromoteToSub(Entity e, EntityLink link, int index)
            {
                var b = GetOrCreate(e);
                if (b.Main == link) b.Main = null;
                if (!b.Subs.TryGetValue(index, out var set))
                    set = b.Subs[index] = new HashSet<EntityLink>();
                set.Add(link); link.OverrideKey(ViewKey.Sub(index));
            }

            private void PromoteToSubInternal(Entity e, EntityLink link, int fallback) =>
                PromoteToSub(e, link, TryFindSubKey(e, link, out var idx) ? idx : fallback);

            private bool TryFindSubKey(Entity e, EntityLink link, out int index)
            {
                index = 0;
                if (!_map.TryGetValue(e, out var b)) return false;
                foreach (var kv in b.Subs) if (kv.Value.Contains(link)) { index = kv.Key; return true; }
                return false;
            }

            public bool HasAny(Entity e) => _map.TryGetValue(e, out var b) && (b.Main || b.Subs.Count > 0);
        }
    }
}
