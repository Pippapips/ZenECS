// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Hook/Permission API)
// File: WorldHookApi.cs
// Purpose: Configure read/write permission and validation hooks per world.
// Key concepts:
//   • Read/Write gates: centralize policy for component access.
//   • Typed & object validators: data-shape rules before writes.
//   • Clear/Remove helpers: reset policies for tests/tools quickly.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldHookApi"/> by forwarding to the internal permission hook.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial <c>World</c> implementation exposes a stable surface for
    /// configuring read/write permissions and validators while delegating the
    /// actual storage and evaluation to the injected <c>_permissionHook</c>.
    /// </para>
    /// <para>
    /// Keeping the hook implementation separate from the world allows tests,
    /// tools, and adapters to swap in custom permission strategies if needed.
    /// </para>
    /// </remarks>
    internal sealed partial class World : IWorldHookApi
    {
        /// <summary>
        /// Adds a write-permission predicate of the form
        /// <c>(entity, componentType) → allowed</c>.
        /// </summary>
        /// <param name="hook">Predicate that decides whether a write is allowed.</param>
        public void AddWritePermission(Func<Entity, Type, bool> hook)
            => _permissionHook.AddWritePermission(hook);

        /// <summary>
        /// Removes a previously added write-permission predicate.
        /// </summary>
        /// <param name="hook">Previously registered predicate to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool RemoveWritePermission(Func<Entity, Type, bool> hook)
            => _permissionHook.RemoveWritePermission(hook);

        /// <summary>
        /// Clears all write-permission predicates for this world.
        /// </summary>
        public void ClearWritePermissions()
            => _permissionHook.ClearWritePermissions();

        /// <summary>
        /// Adds a read-permission predicate of the form
        /// <c>(entity, componentType) → allowed</c>.
        /// </summary>
        /// <param name="hook">Predicate that decides whether a read is allowed.</param>
        public void AddReadPermission(Func<Entity, Type, bool> hook)
            => _permissionHook.AddReadPermission(hook);

        /// <summary>
        /// Removes a previously added read-permission predicate.
        /// </summary>
        /// <param name="hook">Previously registered predicate to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool RemoveReadPermission(Func<Entity, Type, bool> hook)
            => _permissionHook.RemoveReadPermission(hook);

        /// <summary>
        /// Clears all read-permission predicates for this world.
        /// </summary>
        public void ClearReadPermissions()
            => _permissionHook.ClearReadPermissions();

        /// <summary>
        /// Adds an object-level validator invoked on write attempts.
        /// </summary>
        /// <param name="hook">
        /// Validator delegate that receives the boxed value being written and
        /// returns <see langword="true"/> if it is valid, or
        /// <see langword="false"/> to reject the write.
        /// </param>
        public void AddValidator(Func<object, bool> hook)
            => _permissionHook.AddValidator(hook);

        /// <summary>
        /// Removes a previously added object-level validator.
        /// </summary>
        /// <param name="hook">Previously registered validator to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool RemoveValidator(Func<object, bool> hook)
            => _permissionHook.RemoveValidator(hook);

        /// <summary>
        /// Clears all object-level validators for this world.
        /// </summary>
        public void ClearValidators()
            => _permissionHook.ClearValidators();

        /// <summary>
        /// Adds a typed validator invoked when writing components of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">
        /// Predicate that receives the component value and returns
        /// <see langword="true"/> if it is valid, or <see langword="false"/> to
        /// reject the write.
        /// </param>
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct
            => _permissionHook.AddValidator(predicate);

        /// <summary>
        /// Removes a previously added typed validator for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">Previously registered validator to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct
            => _permissionHook.RemoveValidator(predicate);

        /// <summary>
        /// Clears all typed validators for this world.
        /// </summary>
        public void ClearTypedValidators()
            => _permissionHook.ClearTypedValidators();
    }
}
