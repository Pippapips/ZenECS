using System.Collections.Generic;

namespace ZenECS.Core
{
    public interface IWorldEntityApi
    {
        int AliveCount { get; }
        bool IsAlive(Entity e);
        Entity SpawnEntity(int? fixedId = null);
        void DespawnEntity(Entity e);
        void DespawnAllEntities(bool fireEvents = false);
        List<Entity> GetAllEntities();
    }
}
