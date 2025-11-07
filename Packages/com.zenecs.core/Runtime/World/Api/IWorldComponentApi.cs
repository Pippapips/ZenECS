using System;

namespace ZenECS.Core
{
    public interface IWorldComponentApi
    {
        void AddComponent<T>(Entity e, in T value) where T : struct;
        bool HasComponent<T>(Entity e) where T : struct;
        ref T RefComponent<T>(Entity e) where T : struct;
        ref T RefComponentExisting<T>(Entity e) where T : struct;
        ref T ReadComponent<T>(Entity e) where T : struct;
        void ReplaceComponent<T>(Entity e, in T value) where T : struct;
    }
}
