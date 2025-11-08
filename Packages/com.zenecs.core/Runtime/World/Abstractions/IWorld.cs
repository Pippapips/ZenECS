#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    public interface IWorld : IDisposable,
        IWorldQueryApi,
        IWorldQueryToSpanApi,
        IWorldEntityApi,
        IWorldComponentApi,
        IWorldContextApi,
        IWorldBinderApi,
        IWorldSnapshotApi,
        IWorldMessagesApi,
        IWorldHookApi,
        IWorldCommandBufferApi,
        IWorldWorkerApi
    {
        WorldId Id { get; }
        string  Name { get; set; }
        IReadOnlyCollection<string> Tags { get; }
        bool IsPaused { get; }

        void Pause();
        void Resume();
        void Reset(bool keepCapacity);
        
        int GenerationOf(int id);
        
        void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null);
    }
}