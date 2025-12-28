// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Messaging
// File: MessageBus.cs
// Purpose: Thread-safe publish/subscribe dispatcher with per-type topics.
// Key concepts:
//   • Struct messages only (no boxing on Publish)
//   • Deterministic per-frame delivery via PumpAll()
//   • Lock-free queues; synchronized subscriber list snapshots
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Messaging.Internal
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IMessageBus"/> using per-type
    /// topics backed by lock-free queues and subscriber lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each message type gets its own <c>Topic&lt;T&gt;</c>
    /// instance containing a concurrent queue and a list of subscribers.
    /// </para>
    /// <para>
    /// <see cref="Publish{T}(in T)"/> enqueues messages without blocking subscribers.
    /// Actual delivery occurs when <see cref="PumpAll"/> is called, on the caller's thread.
    /// </para>
    /// </remarks>
    internal sealed class MessageBus : IMessageBus
    {
        /// <summary>
        /// Minimal topic abstraction for pumpable message queues.
        /// </summary>
        /// <remarks>
        /// Each message type has its own topic instance that manages a queue
        /// of pending messages and a list of subscribers. Topics are created
        /// lazily when the first message of a given type is published.
        /// </remarks>
        private interface ITopic
        {
            /// <summary>
            /// Pumps all queued messages for this topic.
            /// </summary>
            /// <returns>
            /// Number of messages processed during this pump.
            /// </returns>
            int Pump();
        }

        /// <summary>
        /// Topic implementation for messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Struct message type implementing <see cref="IMessage"/>.</typeparam>
        /// <remarks>
        /// <para>
        /// Each topic maintains a lock-free queue for message publishing and a
        /// synchronized list of subscribers. Messages are delivered to all
        /// subscribers when <see cref="Pump"/> is called.
        /// </para>
        /// <para>
        /// Subscribers are stored in a snapshot taken at pump time to allow
        /// safe iteration even if the subscriber list is modified during delivery.
        /// </para>
        /// </remarks>
        private sealed class Topic<T> : ITopic where T : struct, IMessage
        {
            private readonly ConcurrentQueue<T> _queue = new();
            private readonly List<Action<T>> _subscribers = new();

            /// <summary>
            /// Enqueues a message instance for later delivery.
            /// </summary>
            /// <param name="message">Message to enqueue.</param>
            public void Publish(in T message) => _queue.Enqueue(message);

            /// <summary>
            /// Registers a subscriber for message type <typeparamref name="T"/>.
            /// </summary>
            /// <param name="handler">Callback invoked for each delivered message.</param>
            /// <returns>
            /// An <see cref="IDisposable"/> token; dispose it to unsubscribe
            /// <paramref name="handler"/>.
            /// </returns>
            public IDisposable Subscribe(Action<T> handler)
            {
                lock (_subscribers) _subscribers.Add(handler);
                return new Unsub(this, handler);
            }

            /// <summary>
            /// Delivers all queued messages to a snapshot of subscribers.
            /// </summary>
            /// <returns>
            /// Number of messages processed during this pump.
            /// </returns>
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

            /// <summary>
            /// Disposable token that removes a handler from the parent topic.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This token is returned when subscribing to a topic and can be
            /// disposed to unsubscribe the handler. The unsubscribe operation
            /// is thread-safe and removes the handler from the topic's subscriber
            /// list.
            /// </para>
            /// <para>
            /// Disposing the token multiple times is safe and has no effect after
            /// the first disposal.
            /// </para>
            /// </remarks>
            private sealed class Unsub : IDisposable
            {
                private readonly Topic<T> _owner;
                private readonly Action<T> _handler;

                /// <summary>
                /// Creates a new unsubscribe token.
                /// </summary>
                /// <param name="owner">Owning topic.</param>
                /// <param name="handler">Handler to remove on dispose.</param>
                public Unsub(Topic<T> owner, Action<T> handler)
                {
                    _owner = owner;
                    _handler = handler;
                }

                /// <summary>
                /// Removes the handler from the topic's subscriber list.
                /// </summary>
                public void Dispose()
                {
                    lock (_owner._subscribers) _owner._subscribers.Remove(_handler);
                }
            }
        }

        /// <summary>
        /// Per-message-type topic map.
        /// </summary>
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
