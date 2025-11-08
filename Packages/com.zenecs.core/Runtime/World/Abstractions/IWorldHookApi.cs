// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Hook/Permission API
// File: IWorldHookApi.cs
// Purpose: Central read/write permission gates and value validators.
// Key concepts:
//   • Read/Write predicates: (entity, componentType) → allowed.
//   • Validators: object-level and typed validators before writes.
//   • Testability: clear/remove helpers for deterministic setups.
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>Permission/validation hook surface used by component APIs.</summary>
    public interface IWorldHookApi
    {
        /// <summary>Add a write-permission predicate.</summary>
        void AddWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>Remove a previously added write-permission predicate.</summary>
        bool RemoveWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>Clear all write-permission predicates.</summary>
        void ClearWritePermissions();

        /// <summary>Add a read-permission predicate.</summary>
        void AddReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>Remove a previously added read-permission predicate.</summary>
        bool RemoveReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>Clear all read-permission predicates.</summary>
        void ClearReadPermissions();

        /// <summary>Add an object-level validator invoked on write attempts.</summary>
        void AddValidator(Func<object, bool> hook);

        /// <summary>Remove a previously added object-level validator.</summary>
        bool RemoveValidator(Func<object, bool> hook);

        /// <summary>Clear all object-level validators.</summary>
        void ClearValidators();

        /// <summary>Add a typed validator invoked when writing <typeparamref name="T"/>.</summary>
        void AddValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>Remove a previously added typed validator for <typeparamref name="T"/>.</summary>
        bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>Clear all typed validators.</summary>
        void ClearTypedValidators();
    }
}
