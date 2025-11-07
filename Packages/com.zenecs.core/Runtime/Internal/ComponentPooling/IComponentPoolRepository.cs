#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Common interface for all component pools.
    /// Keeps the minimal set of APIs required for snapshot save/load and tooling reflection.
    /// </summary>
    internal interface IComponentPoolRepository
    {
        void Initialize(int poolSize);
        IComponentPool GetPool<T>() where T : struct;
        ComponentPool<T>? TryGetPool<T>() where T : struct;
        IComponentPool GetOrCreatePoolByType(Type t);
        IComponentPool? GetPool(Type t);
        void RemoveEntity(Entity e);
    }
}