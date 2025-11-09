// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Hook/Permission API)
// File: WorldHookApi.cs
// Purpose: Configure read/write permission and validation hooks per world.
// Key concepts:
//   • Read/Write gates: centralize policy for component access.
//   • Typed & object validators: data-shape rules before writes.
//   • Clear/Remove helpers: reset policies for tests/tools quickly.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldHookApi"/> – permission/validation hook surface.
    /// </summary>
    internal sealed partial class World : IWorldHookApi
    {
        /// <summary>Add a write-permission predicate (entity, componentType) → allowed.</summary>
        public void AddWritePermission(Func<Entity, Type, bool> hook) => _permissionHook.AddWritePermission(hook);

        /// <summary>Remove a previously added write-permission predicate.</summary>
        public bool RemoveWritePermission(Func<Entity, Type, bool> hook) => _permissionHook.RemoveWritePermission(hook);

        /// <summary>Clear all write-permission predicates.</summary>
        public void ClearWritePermissions() => _permissionHook.ClearWritePermissions();

        /// <summary>Add a read-permission predicate (entity, componentType) → allowed.</summary>
        public void AddReadPermission(Func<Entity, Type, bool> hook) => _permissionHook.AddReadPermission(hook);

        /// <summary>Remove a previously added read-permission predicate.</summary>
        public bool RemoveReadPermission(Func<Entity, Type, bool> hook) => _permissionHook.RemoveReadPermission(hook);

        /// <summary>Clear all read-permission predicates.</summary>
        public void ClearReadPermissions() => _permissionHook.ClearReadPermissions();

        /// <summary>Add an object-level validator invoked on write attempts.</summary>
        public void AddValidator(Func<object, bool> hook) => _permissionHook.AddValidator(hook);

        /// <summary>Remove a previously added object-level validator.</summary>
        public bool RemoveValidator(Func<object, bool> hook) => _permissionHook.RemoveValidator(hook);

        /// <summary>Clear all object-level validators.</summary>
        public void ClearValidators() => _permissionHook.ClearValidators();

        /// <summary>Add a typed validator invoked on writes to <typeparamref name="T"/>.</summary>
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct => _permissionHook.AddValidator(predicate);

        /// <summary>Remove a previously added typed validator for <typeparamref name="T"/>.</summary>
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct => _permissionHook.RemoveValidator(predicate);

        /// <summary>Clear all typed validators.</summary>
        public void ClearTypedValidators() => _permissionHook.ClearTypedValidators();
    }
}
