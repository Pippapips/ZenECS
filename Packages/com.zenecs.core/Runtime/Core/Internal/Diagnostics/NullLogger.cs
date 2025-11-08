using ZenECS.Core.Abstractions.Diagnostics;

namespace ZenECS.Core.Internal.Diagnostics
{
    /// <summary>
    /// No-op logger used as a safe default when no logger is configured.
    /// </summary>
    internal sealed class NullLogger : IEcsLogger
    {
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Error(string message) { }
    }
}