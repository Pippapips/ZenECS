#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Options for Kernel behavior and world creation/ticking policies.
    /// Keep this surface stable — adapters can safely rely on it.
    /// </summary>
    public sealed class KernelOptions
    {
        /// <summary>
        /// Factory used when generating a new <see cref="WorldId"/> for created worlds.
        /// Defaults to a Guid-based id.
        /// </summary>
        public Func<WorldId> NewWorldIdFactory { get; set; } = () => new WorldId(Guid.NewGuid());

        /// <summary>
        /// Prefix used when auto-naming worlds that omit an explicit name.
        /// </summary>
        public string AutoNamePrefix { get; set; } = "World-";

        /// <summary>
        /// If true, <see cref="Kernel.Tick(double)"/> will step only the current world
        /// when a current world is selected; otherwise, all worlds are stepped.
        /// </summary>
        public bool StepOnlyCurrentWhenSelected { get; set; } = false;

        /// <summary>
        /// If true, the Kernel may automatically set newly-created worlds as the current world
        /// when the caller doesn't specify otherwise. This is a global default; callers can still
        /// override per-call using the 'setAsCurrent' parameter of CreateWorld.
        /// </summary>
        public bool AutoSelectNewWorld { get; set; } = false;

        /// <summary>
        /// Generate a new world id using <see cref="NewWorldIdFactory"/>.
        /// </summary>
        public WorldId NewWorldId() => NewWorldIdFactory();
    }
}
