#nullable enable
using System;
using ZenECS.Core.Abstractions.Diagnostics;
using ZenECS.Core.Internal.Diagnostics;

namespace ZenECS.Core.Abstractions.Config
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
        /// </summary>
        /// <value>
        /// Defaults to a no-op logger (<see cref="NullLogger"/>). Replace this with your own
        /// implementation during startup to surface core diagnostics.
        /// </value>
        public static IEcsLogger Log { get; set; } = new NullLogger();

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
            /// Log a warning and ignore the operation. Useful when you want visibility without halting.
            /// </summary>
            Log,

            /// <summary>
            /// Silently ignore the operation. Use with caution in production when performance
            /// and resilience outweigh strict feedback.
            /// </summary>
            Ignore
        }

        /// <summary>
        /// Gets or sets the global policy the core uses when a write is denied.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="WriteFailurePolicy.Throw"/> to surface issues early.
        /// Consider <see cref="WriteFailurePolicy.Log"/> for friendlier behavior in non-dev environments.
        /// </remarks>
        public static WriteFailurePolicy WritePolicy { get; set; } = WriteFailurePolicy.Throw;

        // ---- Misc / hooks -------------------------------------------------------------

        /// <summary>
        /// Optional global callback invoked whenever a critical runtime error is reported via <see cref="Report"/>.
        /// </summary>
        /// <remarks>
        /// Use this to route exceptions to crash reporters or UI toasts. The callback is wrapped in a try/catch
        /// to avoid cascading failures; any exception thrown here is swallowed.
        /// </remarks>
        public static Action<Exception>? OnUnhandledError { get; set; }

        /// <summary>
        /// Reports a non-fatal exception through the configured logger and invokes
        /// <see cref="OnUnhandledError"/> if it is set.
        /// </summary>
        /// <param name="ex">The exception to report.</param>
        /// <param name="context">
        /// Optional context string appended to the log (e.g., system name, operation).
        /// </param>
        /// <remarks>
        /// Both logging and callback invocations are individually guarded with try/catch to ensure
        /// reporting never throws.
        /// </remarks>
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
                // Swallow logging failures to keep reporting non-throwing.
            }

            try
            {
                OnUnhandledError?.Invoke(ex);
            }
            catch
            {
                // Swallow callback failures for the same reason.
            }
        }
    }
}