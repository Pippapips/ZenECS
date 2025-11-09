// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Hooks
// File: IPermissionHook.cs
// Purpose: Define world-scoped read/write permission hooks and value validators.
// Key concepts:
//   • Write/Read permission predicates by (Entity, ComponentType)
//   • Object-level and typed value validators (struct T)
//   • Deterministic fan-in evaluation; Clear… helpers for teardown
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Internal.Hooking
{
    /// <summary>
    /// World-scoped hook surface that governs structural read/write permissions
    /// and validates component values before they are written.
    /// </summary>
    internal interface IPermissionHook
    {
        // --- Write permissions ------------------------------------------------

        /// <summary>
        /// Adds a write-permission predicate evaluated for every structural write
        /// (Add/Replace/Remove of a component) in this world.
        /// </summary>
        /// <param name="hook">
        /// Predicate returning <see langword="true"/> to allow the write.
        /// Parameters: (<see cref="Entity"/> entity, <see cref="Type"/> componentType).
        /// </param>
        void AddWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>Removes a previously added write-permission predicate.</summary>
        bool RemoveWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>Clears all write-permission predicates.</summary>
        void ClearWritePermissions();

        // --- Read permissions -------------------------------------------------

        /// <summary>
        /// Adds a read-permission predicate evaluated for component read access
        /// (e.g., <c>Get&lt;T&gt; / RefExisting&lt;T&gt;</c>) in this world.
        /// </summary>
        void AddReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>Removes a previously added read-permission predicate.</summary>
        bool RemoveReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>Clears all read-permission predicates.</summary>
        void ClearReadPermissions();

        // --- Object-level validators -----------------------------------------

        /// <summary>
        /// Adds a type-agnostic validator invoked with a boxed component value.
        /// </summary>
        void AddValidator(Func<object, bool> hook);

        /// <summary>Removes a previously added type-agnostic validator.</summary>
        bool RemoveValidator(Func<object, bool> hook);

        /// <summary>Clears all type-agnostic validators.</summary>
        void ClearValidators();

        // --- Typed validators (no boxing) ------------------------------------

        /// <summary>
        /// Adds a type-specific validator invoked for values of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        void AddValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>Removes a previously added type-specific validator.</summary>
        bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>Clears all typed validators registered via <see cref="AddValidator{T}"/>.</summary>
        void ClearTypedValidators();

        // --- Evaluation API ---------------------------------------------------

        /// <summary>Evaluates write permission hooks for the given access.</summary>
        bool EvaluateWritePermission(Entity e, Type t);

        /// <summary>Evaluates read permission hooks for the given access.</summary>
        bool EvaluateReadPermission(Entity e, Type t);

        /// <summary>Evaluates all object-level validators for the boxed value.</summary>
        bool ValidateObject(object value);

        /// <summary>Evaluates typed validators for <typeparamref name="T"/> without boxing.</summary>
        bool ValidateTyped<T>(in T value) where T : struct;

        // --- Maintenance ------------------------------------------------------

        /// <summary>
        /// Clears all hook queues (read/write permissions and validators).
        /// </summary>
        void ClearAllHookQueues();
    }
}
