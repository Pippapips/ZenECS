#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>Internal world instance (implementation hidden). External code should use <see cref="IWorldAPI"/>.</summary>
    /// <summary>
    /// Marker interface for messages used within the ECS message bus.
    /// </summary>
    public interface IMessage { }
    
    public interface IWorldMessagesApi
    {
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;
        void Publish<T>(in T msg) where T : struct, IMessage;
    }
}