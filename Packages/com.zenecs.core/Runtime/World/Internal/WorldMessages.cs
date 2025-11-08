#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldMessagesApi
    {
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage => _bus.Subscribe(handler);
        public void Publish<T>(in T msg) where T : struct, IMessage => _bus.Publish(msg);
    }
}