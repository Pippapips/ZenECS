#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>Internal world instance (implementation hidden). External code should use <see cref="IWorldAPI"/>.</summary>
    public interface IWorld : IDisposable
    {
        WorldId Id { get; }
        string  Name { get; set; }
        IReadOnlyCollection<string> Tags { get; }
        bool IsPaused { get; set; }
    }
}