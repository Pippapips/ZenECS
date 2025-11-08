// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Diagnostics
// File: IEcsLogger.cs
// Purpose: Minimal logging surface to decouple core from specific loggers.
// Key concepts:
//   • Plug-in bridge: adapt to Unity console, Serilog, NLog, etc.
//   • Severity levels: Info, Warn, Error (string messages only).
//   • Lightweight: no formatting helpers to keep core dependency-free.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────

#nullable enable
namespace ZenECS.Core.Abstractions.Diagnostics
{
    /// <summary>
    /// Minimal logging surface used by the core. Implement this interface to bridge
    /// ZenECS logging into your engine/framework logs (e.g., Unity, Serilog, NLog).
    /// </summary>
    public interface IEcsLogger
    {
        /// <summary>Write an informational message.</summary>
        void Info(string message);

        /// <summary>Write a warning message.</summary>
        void Warn(string message);

        /// <summary>Write an error message.</summary>
        void Error(string message);
    }
}