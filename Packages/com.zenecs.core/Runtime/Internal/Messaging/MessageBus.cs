// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Messaging
// File: MessageBus.cs
// Purpose: Thread-safe publish/subscribe dispatcher with per-type topics.
// Key concepts:
//   • Struct messages only (no boxing on Publish)
//   • Deterministic per-frame delivery via PumpAll()
//   • Lock-free queues; synchronized subscriber list snapshots
// License: MIT
// © 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Internal.Messaging
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IMessageBus"/> using per-type
    /// queues and subscriber lists.
    /// </summary>
    internal sealed class MessageBus : IMessageBus
    {
        private interface ITopic { int Pump(); }

        private sealed class Topic<T> : ITopic where T : struct, IMessage
        {
            private readonly ConcurrentQueue<T> _queue = new();
            private readonly List<Action<T>> _subscribers = new();

            /// <summary>Enqueue a message instance.</summary>
            public void Publish(in T message) => _queue.Enqueue(message);

            /// <summary>Register a subscriber for <typeparamref name="T"/>.</summary>
            public IDisposable Subscribe(Action<T> handler)
            {
                lock (_subscribers) _subscribers.Add(handler);
                return new Unsub(this, handler);
            }

            /// <summary>Deliver all queued messages to a snapshot of subscribers.</summary>
            public int Pump()
            {
                Action<T>[] handlers;
                lock (_subscribers) handlers = _subscribers.ToArray();

                int count = 0;
                while (_queue.TryDequeue(out var msg))
                {
                    for (int i = 0; i < handlers.Length; i++)
                        handlers[i](msg);
                    count++;
                }
                return count;
            }

            private sealed class Unsub : IDisposable
            {
                private readonly Topic<T> _owner;
                private readonly Action<T> _handler;
                public Unsub(Topic<T> owner, Action<T> handler)
                {
                    _owner = owner; _handler = handler;
                }
                public void Dispose()
                {
                    lock (_owner._subscribers) _owner._subscribers.Remove(_handler);
                }
            }
        }

        private readonly ConcurrentDictionary<Type, ITopic> _topics = new();

        /// <inheritdoc/>
        public void Publish<T>(in T msg) where T : struct, IMessage
            => ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Publish(in msg);

        /// <inheritdoc/>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage
            => ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Subscribe(handler);

        /// <inheritdoc/>
        public int PumpAll()
        {
            int processed = 0;
            foreach (var kv in _topics)
                processed += kv.Value.Pump();
            return processed;
        }

        /// <inheritdoc/>
        public void Clear() => _topics.Clear();
    }
}
