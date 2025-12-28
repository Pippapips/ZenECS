// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Messages API)
// File: WorldMessagesApi.cs
// Purpose: Lightweight pub/sub for struct messages scoped to a world.
// Key concepts:
//   • Per-world bus: isolation between worlds.
//   • Value-type messages: heap pressure kept low; handlers are Action<T>.
//   • IDisposable subscriptions: deterministic unsubscription & lifetime mgmt.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core.Messaging;
using ZenECS.Core.Messaging.Internal;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldMessagesApi"/> by delegating to the injected message bus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial <c>World</c> implementation exposes a thin façade over the
    /// internal <c>_bus</c> (<see cref="IMessageBus"/>). The bus itself is
    /// responsible for storing subscriptions and delivering messages, while the
    /// world ensures that each world instance receives its own dedicated bus.
    /// </para>
    /// <para>
    /// Keeping the bus implementation separate from the world makes it easier
    /// to test and to swap out the bus strategy (for example, for tracing,
    /// metrics, or alternative dispatch models).
    /// </para>
    /// </remarks>
    internal sealed partial class World : IWorldMessagesApi
    {
        /// <summary>
        /// Subscribes a handler to messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Message value type. Must be a <c>struct</c> that implements
        /// <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="handler">
        /// Delegate to invoke for each published message of type
        /// <typeparamref name="T"/>. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> token that can be disposed to unsubscribe
        /// the handler from the bus.
        /// </returns>
        /// <remarks>
        /// Internally this simply forwards to <c>_bus.Subscribe(handler)</c>.
        /// </remarks>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage
            => _bus.Subscribe(handler);

        /// <summary>
        /// Publishes a message instance to all subscribers of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Message value type. Must be a <c>struct</c> that implements
        /// <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="msg">Message instance to publish.</param>
        /// <remarks>
        /// This is a thin wrapper over <c>_bus.Publish(msg)</c>, ensuring that
        /// messages are dispatched on the world-scoped bus associated with this
        /// <c>World</c> instance.
        /// </remarks>
        public void Publish<T>(in T msg) where T : struct, IMessage
            => _bus.Publish(msg);
    }
}
