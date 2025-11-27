// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Messages API
// File: IWorldMessagesApi.cs
// Purpose: Lightweight, per-world pub/sub for struct messages.
// Key concepts:
//   • Isolation per world: no cross-world bleed.
//   • Value-type messages: minimal GC; Action<T> handlers.
//   • Disposable subscription tokens for deterministic unsubscription.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Messaging;

namespace ZenECS.Core
{
    /// <summary>
    /// Per-world publish/subscribe surface for lightweight struct messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each world has its own isolated message bus; messages never leak between
    /// different worlds. This makes the API safe to use in multi-world setups
    /// (for example, gameplay, replay, and spectator worlds running in parallel).
    /// </para>
    /// <para>
    /// The message bus is intentionally minimal:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    /// Messages are value types implementing <see cref="IMessage"/>.
    ///   </description></item>
    ///   <item><description>
    /// Subscribers are <see cref="Action{T}"/> delegates invoked synchronously
    /// when a message is published.
    ///   </description></item>
    ///   <item><description>
    /// Subscriptions are represented by <see cref="IDisposable"/> tokens, making
    /// unsubscription explicit and deterministic.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public interface IWorldMessagesApi
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
        /// An <see cref="IDisposable"/> token that can be used to unsubscribe.
        /// Disposing the token should remove the handler from the bus.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Handlers are typically invoked synchronously in the context of the
        /// system that calls <see cref="Publish{T}(in T)"/>. Implementations may
        /// choose to buffer or defer invocation, but this is not guaranteed.
        /// </para>
        /// <para>
        /// Callers are responsible for holding on to the returned disposable
        /// and disposing it when the subscription is no longer needed
        /// (for example, when a system is removed or a view is destroyed).
        /// </para>
        /// </remarks>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;

        /// <summary>
        /// Publishes a message instance to all subscribers of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Message value type. Must be a <c>struct</c> that implements
        /// <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="msg">Message instance to publish.</param>
        /// <remarks>
        /// <para>
        /// All currently subscribed handlers for <typeparamref name="T"/> will be
        /// invoked (typically in subscription order). Implementations are free
        /// to guard against re-entrancy according to their internal policy.
        /// </para>
        /// <para>
        /// Message publishing is per-world: only subscribers attached to this
        /// world's bus will receive <paramref name="msg"/>.
        /// </para>
        /// </remarks>
        void Publish<T>(in T msg) where T : struct, IMessage;
    }
}
