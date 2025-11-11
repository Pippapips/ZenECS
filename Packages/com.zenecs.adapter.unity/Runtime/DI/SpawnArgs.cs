using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.DI
{
    public readonly struct SpawnArgs
    {
        public readonly GameObject Prefab; public readonly Transform Parent;
        public readonly IWorld World; public readonly Entity Entity; public readonly ViewKey Key;
        public SpawnArgs(GameObject prefab, Transform parent, IWorld world, in Entity entity, ViewKey key)
        { Prefab=prefab; Parent=parent; World=world; Entity=entity; Key=key; }
    }
}