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
        Dictionary<Type, IComponentPool> Pools { get; }
        IComponentPool GetPool<T>() where T : struct;
        ComponentPool<T>? TryGetPool<T>() where T : struct;
        IComponentPool GetOrCreatePoolByType(Type t);
        Func<IComponentPool> GetOrBuildPoolFactory(Type compType);
        IComponentPool? GetPool(Type t);
        void RemoveEntity(Entity e);
    }
}