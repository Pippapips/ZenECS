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
    /// <summary>
    /// Marker interface for messages used within the ECS message bus.
    /// </summary>
    public interface IMessage { }
    
    public interface IWorldMessages
    {
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;
        void Publish<T>(in T msg) where T : struct, IMessage;
    }
}