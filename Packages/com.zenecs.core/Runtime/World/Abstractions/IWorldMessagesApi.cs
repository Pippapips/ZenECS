// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Messages API
// File: IWorldMessagesApi.cs
// Purpose: Lightweight, per-world pub/sub for struct messages.
// Key concepts:
//   • Isolation per world: no cross-world bleed.
//   • Value-type messages: minimal GC; Action<T> handlers.
//   • Disposable subscription tokens for deterministic unsubscription.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>Marker interface for bus messages.</summary>
    public interface IMessage { }

    /// <summary>Per-world pub/sub surface.</summary>
    public interface IWorldMessagesApi
    {
        /// <summary>Subscribe a handler to messages of type <typeparamref name="T"/>.</summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;

        /// <summary>Publish a message instance to all subscribers of <typeparamref name="T"/>.</summary>
        void Publish<T>(in T msg) where T : struct, IMessage;
    }
}