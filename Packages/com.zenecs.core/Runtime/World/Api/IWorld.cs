#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>Internal world instance (implementation hidden). External code should use <see cref="IWorldAPI"/>.</summary>
    public interface IWorld : IDisposable,
        IWorldQueryApi,
        IWorldQueryToSpanApi,
        IWorldEntityApi,
        IWorldComponentApi,
        IWorldContextApi,
        IWorldBinderApi,
        IWorldSnapshot,
        IWorldMessages,
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