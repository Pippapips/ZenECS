#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Safe, serializable reference to a World.
    /// Holds only (Kernel, WorldId) and resolves to a live IWorld on demand.
    /// Prevents use-after-dispose and supports multi-world identity + serialization.
    /// </summary>
    public readonly struct WorldHandle : IEquatable<WorldHandle>
    {
        private readonly IKernel _kernel;
        public  WorldId Id { get; }

        public WorldHandle(IKernel kernel, WorldId id)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            Id      = id;
        }

        /// <summary>Try to resolve to a live world. Returns false if the world is not alive.</summary>
        public bool TryResolve(out IWorld world)
        {
            if (_kernel.TryGet(Id, out world!)) return true;
            world = default!;
            return false;
        }

        /// <summary>Resolve to a live world or throw if it is not alive.</summary>
        public IWorld ResolveOrThrow()
        {
            if (_kernel.TryGet(Id, out var w)) return w;
            throw new InvalidOperationException($"World {Id} is not alive (destroyed or never created).");
        }

        /// <summary>Whether the referenced world is currently alive in the kernel.</summary>
        public bool IsAlive() => _kernel.TryGet(Id, out _);

        public override string ToString() => $"WorldHandle({Id})";

        public bool Equals(WorldHandle other) => Equals(_kernel, other._kernel) && Id.Equals(other.Id);
        public override bool Equals(object? obj) => obj is WorldHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_kernel, Id);
        public static bool operator ==(WorldHandle a, WorldHandle b) => a.Equals(b);
        public static bool operator !=(WorldHandle a, WorldHandle b) => !a.Equals(b);
    }
}