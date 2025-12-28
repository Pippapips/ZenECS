// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: WorldHandle.cs
// Purpose: Safe, serializable handle that stores (Kernel, WorldId) and resolves
//          to a live IWorld on demand.
// Key concepts:
//   • Decoupling: keeps identity only; no direct world reference retained.
//   • Safety: avoids use-after-dispose by resolving through IKernel each time.
//   • Ergonomics: TryResolve / ResolveOrThrow / IsAlive convenience APIs.
//   • Value semantics: equality compares (kernel instance, world id).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// A safe, serializable reference to a world.
    /// The handle keeps only the <c>(IKernel, WorldId)</c> pair and resolves the
    /// current live <see cref="IWorld"/> via the kernel when needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a handle?</b> Storing direct <see cref="IWorld"/> references is unsafe
    /// across destruction or re-creation. <see cref="WorldHandle"/> guarantees that
    /// callers explicitly resolve the world through <see cref="IKernel"/>, which
    /// prevents use-after-dispose and supports multi-world identity.
    /// </para>
    /// <para>
    /// <b>Typical usage</b>
    /// <code language="csharp"><![CDATA[
    /// // Store identity only (safe to serialize).
    /// var handle = new WorldHandle(kernel, world.Id);
    ///
    /// // Later in a system or adapter:
    /// if (handle.TryResolve(out var w))
    /// {
    ///     // Use the world
    /// }
    /// // or
    /// var w2 = handle.ResolveOrThrow(); // throws if not alive
    /// ]]></code>
    /// </para>
    /// </remarks>
    public readonly struct WorldHandle : IEquatable<WorldHandle>
    {
        /// <summary>
        /// Kernel used to resolve the live <see cref="IWorld"/> for <see cref="Id"/>.
        /// </summary>
        private readonly IKernel _kernel;

        /// <summary>
        /// Immutable identifier of the target world.
        /// </summary>
        public WorldId Id { get; }

        /// <summary>
        /// Create a new world handle bound to a kernel and a world id.
        /// </summary>
        /// <param name="kernel">Kernel that owns and can resolve the world.</param>
        /// <param name="id">Identifier of the world to reference.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="kernel"/> is null.</exception>
        public WorldHandle(IKernel kernel, WorldId id)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            Id      = id;
        }

        /// <summary>
        /// Attempt to resolve this handle to a live <see cref="IWorld"/>.
        /// </summary>
        /// <param name="world">
        /// When this method returns, contains the live world if resolution succeeded;
        /// otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the world is currently alive in the kernel; otherwise <c>false</c>.
        /// </returns>
        public bool TryResolve(out IWorld? world)
        {
            if (_kernel.TryGet(Id, out var w))
            {
                world = w;
                return true;
            }
            world = null;
            return false;
        }

        /// <summary>
        /// Resolve this handle to a live <see cref="IWorld"/> or throw if the world is not alive.
        /// </summary>
        /// <returns>The resolved live world instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the referenced world was destroyed or has never been created.
        /// </exception>
        public IWorld? ResolveOrThrow()
        {
            if (_kernel.TryGet(Id, out var w)) return w;
            throw new InvalidOperationException($"World {Id} is not alive (destroyed or never created).");
        }

        /// <summary>
        /// Check whether the referenced world is currently alive (registered in the kernel).
        /// </summary>
        /// <returns><c>true</c> if the world is alive; otherwise <c>false</c>.</returns>
        public bool IsAlive() => _kernel.TryGet(Id, out _);

        /// <summary>
        /// Returns a human-readable representation that includes the world id.
        /// </summary>
        public override string ToString() => $"WorldHandle({Id})";

        /// <summary>
        /// Value equality: two handles are equal if they reference the same kernel instance
        /// and the same <see cref="WorldId"/>.
        /// </summary>
        public bool Equals(WorldHandle other) => Equals(_kernel, other._kernel) && Id.Equals(other.Id);

        /// <summary>
        /// Value equality override (see <see cref="Equals(WorldHandle)"/>).
        /// </summary>
        public override bool Equals(object? obj) => obj is WorldHandle other && Equals(other);

        /// <summary>
        /// Hash code composed from the kernel instance and the world id.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(_kernel, Id);

        /// <summary>
        /// Equality operator (see <see cref="Equals(WorldHandle)"/>).
        /// </summary>
        public static bool operator ==(WorldHandle a, WorldHandle b) => a.Equals(b);

        /// <summary>
        /// Inequality operator (see <see cref="Equals(WorldHandle)"/>).
        /// </summary>
        public static bool operator !=(WorldHandle a, WorldHandle b) => !a.Equals(b);
    }
}
