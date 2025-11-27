// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Diagnostics
// File: IEcsLogger.cs
// Purpose: Minimal logging surface to decouple core from specific loggers.
// Key concepts:
//   • Plug-in bridge: adapt to Unity console, Serilog, NLog, etc.
//   • Severity levels: Info, Warn, Error (string messages only).
//   • Lightweight: no formatting helpers to keep core dependency-free.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Config
{
    /// <summary>
    /// Minimal logging surface used by the core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to bridge ZenECS logging into your engine or
    /// framework logs (for example Unity console, Serilog, NLog, etc.).
    /// </para>
    /// <para>
    /// The interface is intentionally small: it only accepts fully formatted
    /// string messages and does not prescribe structured logging or formatting
    /// helpers, so the core remains free of external logging dependencies.
    /// </para>
    /// </remarks>
    public interface IEcsLogger
    {
        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="message">
        /// Human-readable message to record at informational level.
        /// </param>
        void Info(string message);

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="message">
        /// Human-readable message to record at warning level, typically used
        /// for unexpected but non-fatal conditions.
        /// </param>
        void Warn(string message);

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="message">
        /// Human-readable message to record at error level, typically used
        /// for failures and critical issues.
        /// </param>
        void Error(string message);
    }
}
