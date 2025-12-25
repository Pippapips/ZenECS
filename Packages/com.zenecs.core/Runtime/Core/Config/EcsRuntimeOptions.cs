// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Configuration & Diagnostics
// File: EcsRuntimeOptions.cs
// Purpose: Centralized process-wide runtime options and diagnostic hooks.
// Key concepts:
//   • Pluggable logger: bridge to your engine/framework logging.
//   • Write failure policy: Throw / Log / Ignore on denied structural writes.
//   • Error funnel: single place to report non-fatal exceptions (safe guards).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Config
{
    /// <summary>
    /// Centralized collection of runtime options and diagnostic hooks that influence core behavior.
    /// </summary>
    /// <remarks>
    /// Configure these options during application bootstrap (before systems start). They are static,
    /// process-wide settings and are intended to be read frequently but written infrequently.
    /// </remarks>
    public static class EcsRuntimeOptions
    {
        // ---- Logging ------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the current logger instance used by the core.
        /// Defaults to a no-op logger (<see cref="Internal.NullLogger"/>).
        /// </summary>
        /// <remarks>
        /// Replace this during startup to surface core diagnostics to your logging system.
        /// </remarks>
        public static IEcsLogger Log { get; set; } = new Internal.NullLogger();

        // ---- Structural write policy --------------------------------------------------

        /// <summary>
        /// Defines how the core reacts when a structural write (Add/Replace/Remove) is denied
        /// by permissions or validation policies.
        /// </summary>
        public enum WriteFailurePolicy
        {
            /// <summary>
            /// Throw an exception immediately. Best for development and strict correctness.
            /// </summary>
            Throw,

            /// <summary>
            /// Log a warning and ignore the operation (non-fatal).
            /// </summary>
            Log,

            /// <summary>
            /// Silently ignore the operation. Use with caution in production.
            /// </summary>
            Ignore
        }

        /// <summary>
        /// Gets or sets the global policy the core uses when a structural write is denied.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="WriteFailurePolicy.Throw"/> to surface issues early.
        /// Consider <see cref="WriteFailurePolicy.Log"/> for friendlier behavior in non-dev environments.
        /// </remarks>
        public static WriteFailurePolicy WritePolicy { get; set; } = WriteFailurePolicy.Throw;

        // ---- Misc / hooks -------------------------------------------------------------

        /// <summary>
        /// Optional global callback invoked whenever a critical runtime error is reported via <see cref="Report"/>.
        /// </summary>
        public static Action<Exception>? OnUnhandledError { get; set; }

        /// <summary>
        /// Reports a non-fatal exception through the configured logger and invokes
        /// <see cref="OnUnhandledError"/> (if set). Both paths are guarded with try/catch.
        /// </summary>
        /// <param name="ex">The exception to report.</param>
        /// <param name="context">Optional context string (system/operation name).</param>
        public static void Report(Exception ex, string context = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(context))
                    Log.Error($"[{context}] {ex}");
                else
                    Log.Error(ex.ToString());
            }
            catch
            {
                // Swallow logging failures to avoid cascading errors.
            }

            try
            {
                OnUnhandledError?.Invoke(ex);
            }
            catch
            {
                // Swallow callback failures to keep reporting safe.
            }
        }
    }
}
