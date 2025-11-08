// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Messages API)
// File: WorldMessages.cs
// Purpose: Lightweight pub/sub for struct messages scoped to a world.
// Key concepts:
//   • Per-world bus: isolation between worlds.
//   • Value-type messages: heap pressure kept low; handlers are Action<T>.
//   • IDisposable subscriptions: deterministic unsubscription & lifetime mgmt.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldMessagesApi"/> – per-world pub/sub bus.
    /// </summary>
    internal sealed partial class World : IWorldMessagesApi
    {
        /// <summary>
        /// Subscribe a handler to messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> token to unsubscribe.</returns>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage => _bus.Subscribe(handler);

        /// <summary>
        /// Publish a message instance to all subscribers of <typeparamref name="T"/>.
        /// </summary>
        public void Publish<T>(in T msg) where T : struct, IMessage => _bus.Publish(msg);
    }
}