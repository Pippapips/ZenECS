// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Hooks
// File: IPermissionHook.cs
// Purpose: Define world-scoped read/write permission hooks and value validators.
// Key concepts:
//   • Write/Read permission predicates by (Entity, ComponentType)
//   • Object-level and typed value validators (struct T)
//   • Deterministic fan-in evaluation; Clear… helpers for teardown
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Hooking.Internal
{
    /// <summary>
    /// World-scoped hook surface that governs structural read/write permissions
    /// and validates component values before they are written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An implementation of this interface is typically owned by a single world
    /// instance and consulted by the core whenever a structural operation
    /// (Add/Replace/Remove) or component read occurs.
    /// </para>
    /// <para>
    /// All predicates and validators are combined with logical AND semantics:
    /// if any predicate returns <see langword="false"/>, the operation is denied.
    /// </para>
    /// </remarks>
    internal interface IPermissionHook
    {
        // --- Write permissions ------------------------------------------------

        /// <summary>
        /// Adds a write-permission predicate evaluated for every structural write
        /// (Add/Replace/Remove of a component) in this world.
        /// </summary>
        /// <param name="hook">
        /// Predicate returning <see langword="true"/> to allow the write, or
        /// <see langword="false"/> to deny it.
        /// Parameters:
        /// <list type="bullet">
        /// <item><description><see cref="Entity"/> — target entity.</description></item>
        /// <item><description><see cref="Type"/> — component type being written.</description></item>
        /// </list>
        /// </param>
        void AddWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Removes a previously added write-permission predicate.
        /// </summary>
        /// <param name="hook">The predicate instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Clears all registered write-permission predicates.
        /// </summary>
        void ClearWritePermissions();

        // --- Read permissions -------------------------------------------------

        /// <summary>
        /// Adds a read-permission predicate evaluated for component read access
        /// (for example <c>Get&lt;T&gt;</c> or <c>RefExisting&lt;T&gt;</c>) in this world.
        /// </summary>
        /// <param name="hook">
        /// Predicate returning <see langword="true"/> to allow the read, or
        /// <see langword="false"/> to deny it.
        /// </param>
        void AddReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Removes a previously added read-permission predicate.
        /// </summary>
        /// <param name="hook">The predicate instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Clears all registered read-permission predicates.
        /// </summary>
        void ClearReadPermissions();

        // --- Object-level validators -----------------------------------------

        /// <summary>
        /// Adds a type-agnostic validator invoked with a boxed component value
        /// before it is written.
        /// </summary>
        /// <param name="hook">
        /// Predicate that receives the boxed value and returns
        /// <see langword="true"/> if it is considered valid.
        /// </param>
        void AddValidator(Func<object, bool> hook);

        /// <summary>
        /// Removes a previously added type-agnostic validator.
        /// </summary>
        /// <param name="hook">The validator instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveValidator(Func<object, bool> hook);

        /// <summary>
        /// Clears all registered type-agnostic validators.
        /// </summary>
        void ClearValidators();

        // --- Typed validators (no boxing) ------------------------------------

        /// <summary>
        /// Adds a type-specific validator invoked for values of
        /// <typeparamref name="T"/> before they are written.
        /// </summary>
        /// <typeparam name="T">Component value type to validate.</typeparam>
        /// <param name="predicate">
        /// Predicate that receives the value and returns
        /// <see langword="true"/> if it is considered valid.
        /// </param>
        void AddValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>
        /// Removes a previously added type-specific validator.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">Validator instance to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>
        /// Clears all typed validators registered via <see cref="AddValidator{T}"/>.
        /// </summary>
        void ClearTypedValidators();

        // --- Evaluation API ---------------------------------------------------

        /// <summary>
        /// Evaluates all write-permission predicates for the given access.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="t">Component type being written.</param>
        /// <returns>
        /// <see langword="true"/> if all predicates allow the write;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool EvaluateWritePermission(Entity e, Type t);

        /// <summary>
        /// Evaluates all read-permission predicates for the given access.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="t">Component type being read.</param>
        /// <returns>
        /// <see langword="true"/> if all predicates allow the read;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool EvaluateReadPermission(Entity e, Type t);

        /// <summary>
        /// Evaluates all object-level validators for the boxed value.
        /// </summary>
        /// <param name="value">Boxed component value to validate.</param>
        /// <returns>
        /// <see langword="true"/> if all validators accept the value;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool ValidateObject(object value);

        /// <summary>
        /// Evaluates typed validators for <typeparamref name="T"/> without boxing.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <returns>
        /// <see langword="true"/> if all typed validators accept the value;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool ValidateTyped<T>(in T value) where T : struct;

        // --- Maintenance ------------------------------------------------------

        /// <summary>
        /// Clears all hook queues: write permissions, read permissions, and
        /// both typed and untyped validators.
        /// </summary>
        void ClearAllHookQueues();
    }
}