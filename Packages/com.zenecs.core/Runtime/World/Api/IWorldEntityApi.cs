using System;

namespace ZenECS.Core
{
    public interface IWorldEntityApi
    {
        bool IsAlive(Entity e);
        Entity SpawnEntity(int? fixedId = null);
        void DespawnEntity(Entity e);
        //void DespawnAllEntities(bool fireEvents = false);
    }
}
