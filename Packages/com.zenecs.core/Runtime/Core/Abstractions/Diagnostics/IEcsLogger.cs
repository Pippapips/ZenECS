namespace ZenECS.Core.Abstractions.Diagnostics
{
    /// <summary>
    /// Minimal logging surface used by the core. Implement this interface to bridge
    /// ZenECS logging into your engine/framework logs (e.g., Unity, Serilog, NLog).
    /// </summary>
    public interface IEcsLogger
    {
        /// <summary>Writes an informational message.</summary>
        /// <param name="message">The message to log.</param>
        void Info(string message);

        /// <summary>Writes a warning message.</summary>
        /// <param name="message">The message to log.</param>
        void Warn(string message);

        /// <summary>Writes an error message.</summary>
        /// <param name="message">The message to log.</param>
        void Error(string message);
    }
}