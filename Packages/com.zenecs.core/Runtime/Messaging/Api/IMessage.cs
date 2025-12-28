// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Messaging
// File: IMessage.cs
// Purpose: Minimal publish/subscribe API for struct-based message passing.
// Key concepts:
//   • Per-type topics: queue + subscriber list
//   • PumpAll() once per frame to deliver deterministically
//   • Thread-safe publish/subscribe; synchronous delivery during PumpAll
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Messaging
{
    /// <summary>
    /// Marker interface for messages that can be published on the world message bus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All messages flowing through <see cref="IWorldMessagesApi"/> are required
    /// to be value types (<c>struct</c>) that also implement this interface.
    /// </para>
    /// <para>
    /// Using value types keeps GC pressure low and makes it cheap to broadcast
    /// small, frequently published messages such as events, commands, and
    /// notifications between systems.
    /// </para>
    /// </remarks>
    public interface IMessage
    {
    }
}