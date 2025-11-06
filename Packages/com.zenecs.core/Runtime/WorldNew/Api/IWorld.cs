#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>Internal world instance (implementation hidden). External code should use <see cref="IWorldAPI"/>.</summary>
    public interface IWorld : IDisposable
    {
        WorldId Id { get; }
        string  Name { get; set; }
        IReadOnlyCollection<string> Tags { get; }
        bool IsPaused { get; }

        void Pause();
        void Resume();
        
        void Initialize(IEnumerable<ISystem>? systems = null, Action<string>? warn = null);
        void Shutdown();

        void AddComponent<T>(Entity e, in T value) where T : struct;
        bool HasComponent<T>(Entity e) where T : struct;
    }
}