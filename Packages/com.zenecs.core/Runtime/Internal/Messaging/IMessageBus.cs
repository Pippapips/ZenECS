// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Messaging
// File: IMessageBus.cs
// Purpose: Minimal publish/subscribe API for struct-based message passing.
// Key concepts:
//   • Per-type topics: queue + subscriber list
//   • PumpAll() once per frame to deliver deterministically
//   • Thread-safe publish/subscribe; synchronous delivery during PumpAll
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Internal.Messaging
{
    /// <summary>
    /// Contract for the core message bus. Messages are value types that implement
    /// a marker interface (e.g., <c>IMessage</c>) and are delivered on the same thread
    /// that calls <see cref="PumpAll"/>.
    /// </summary>
    internal interface IMessageBus
    {
        /// <summary>
        /// Subscribes to messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Struct message type implementing <c>IMessage</c>.</typeparam>
        /// <param name="handler">Callback invoked for each delivered message.</param>
        /// <returns>An <see cref="IDisposable"/> token; dispose to unsubscribe.</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;

        /// <summary>
        /// Publishes a message instance (enqueues for next <see cref="PumpAll"/>).
        /// </summary>
        /// <typeparam name="T">Struct message type implementing <c>IMessage</c>.</typeparam>
        /// <param name="msg">Message instance (passed by <see langword="in"/> reference).</param>
        void Publish<T>(in T msg) where T : struct, IMessage;

        /// <summary>
        /// Flushes all topics and synchronously delivers messages to subscribers.
        /// </summary>
        /// <returns>Total number of processed messages across all topics.</returns>
        int PumpAll();

        /// <summary>
        /// Clears all topics, queues, and subscriber lists.
        /// </summary>
        void Clear();
    }
}
