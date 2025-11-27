// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Messaging
// File: IMessageBus.cs
// Purpose: Minimal publish/subscribe API for struct-based message passing.
// Key concepts:
//   • Per-type topics: queue + subscriber list
//   • PumpAll() once per frame to deliver deterministically
//   • Thread-safe publish/subscribe; synchronous delivery during PumpAll
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Messaging.Internal
{
    /// <summary>
    /// Contract for the core message bus used for lightweight struct-based messaging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages are value types that implement <see cref="IMessage"/> and are delivered
    /// on the same thread that calls <see cref="PumpAll"/>. Calling
    /// <see cref="Publish{T}(in T)"/> only enqueues the message; it is not delivered
    /// until the next pump.
    /// </para>
    /// <para>
    /// The bus maintains per-type topics, each with its own queue and subscriber list.
    /// Subscribers always observe messages in the order they were published within a
    /// topic during a single pump.
    /// </para>
    /// </remarks>
    internal interface IMessageBus
    {
        /// <summary>
        /// Subscribes to messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Struct message type implementing <see cref="IMessage"/>.</typeparam>
        /// <param name="handler">
        /// Callback invoked for each delivered message of type <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> token; disposing it unsubscribes
        /// <paramref name="handler"/> from further messages.
        /// </returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;

        /// <summary>
        /// Publishes a message instance by enqueuing it for the next <see cref="PumpAll"/> call.
        /// </summary>
        /// <typeparam name="T">Struct message type implementing <see cref="IMessage"/>.</typeparam>
        /// <param name="msg">
        /// Message instance to publish. It is copied into the internal queue.
        /// </param>
        /// <remarks>
        /// The message is delivered synchronously to subscribers when
        /// <see cref="PumpAll"/> is executed, not at the time of this call.
        /// </remarks>
        void Publish<T>(in T msg) where T : struct, IMessage;

        /// <summary>
        /// Flushes all topics and synchronously delivers all queued messages
        /// to their subscribers.
        /// </summary>
        /// <returns>
        /// Total number of processed messages across all topics during this pump.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Delivery order is:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Topics are pumped in an unspecified but stable dictionary-iteration order.
        /// </description></item>
        /// <item><description>
        /// Within a topic, messages are delivered in FIFO order to a snapshot of
        /// current subscribers.
        /// </description></item>
        /// </list>
        /// </remarks>
        int PumpAll();

        /// <summary>
        /// Clears all topics, message queues, and subscriber lists.
        /// </summary>
        /// <remarks>
        /// This is typically used during world teardown, tests, or when resetting
        /// the bus. After calling this, all subscriptions are removed.
        /// </remarks>
        void Clear();
    }
}
