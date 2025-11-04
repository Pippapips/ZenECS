#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>Stable identifier for a World.</summary>
    public readonly struct WorldId : IEquatable<WorldId>
    {
        public Guid Value { get; }
        public WorldId(Guid value) => Value = value;
        public bool Equals(WorldId other) => Value.Equals(other.Value);
        public override bool Equals(object? obj) => obj is WorldId o && Equals(o);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(WorldId a, WorldId b) => a.Equals(b);
        public static bool operator !=(WorldId a, WorldId b) => !a.Equals(b);
    }
}