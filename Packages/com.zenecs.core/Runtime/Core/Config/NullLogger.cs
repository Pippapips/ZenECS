// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Diagnostics
// File: NullLogger.cs
// Purpose: Safe default logger that drops all messages (no-op).
// Key concepts:
//   • Always available: avoids null checks in core code paths.
//   • Replace at bootstrap with your real logger.
//   • Intentionally silent in all methods.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Config.Internal
{
    /// <summary>
    /// No-op logger used as a safe default when no logger is configured.
    /// </summary>
    internal sealed class NullLogger : IEcsLogger
    {
        /// <inheritdoc/>
        public void Info(string message)
        {
        }

        /// <inheritdoc/>
        public void Warn(string message)
        {
        }

        /// <inheritdoc/>
        public void Error(string message)
        {
        }
    }
}