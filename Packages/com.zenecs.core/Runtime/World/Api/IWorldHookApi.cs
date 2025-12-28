// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Hook/Permission API
// File: IWorldHookApi.cs
// Purpose: Central read/write permission gates and value validators.
// Key concepts:
//   • Read/Write predicates: (entity, componentType) → allowed.
//   • Validators: object-level and typed validators before writes.
//   • Testability: clear/remove helpers for deterministic setups.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// World-level surface for configuring read/write permissions and value validators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Component APIs use this hook interface to decide whether a read or write
    /// operation is allowed, and whether a value passes validation, before
    /// mutating world state.
    /// </para>
    /// <para>
    /// Typical use cases include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Preventing structural changes during specific phases.</description></item>
    ///   <item><description>Enforcing server-authoritative write rules in clients.</description></item>
    ///   <item><description>Adding invariants or data-shape checks for components.</description></item>
    ///   <item><description>Temporarily locking specific entities or types.</description></item>
    /// </list>
    /// <para>
    /// All hooks are per-world and can be cleared/reset, which is especially
    /// useful in tests and editor tooling.
    /// </para>
    /// </remarks>
    public interface IWorldHookApi
    {
        /// <summary>
        /// Adds a write-permission predicate.
        /// </summary>
        /// <param name="hook">
        /// Predicate that receives the target <see cref="Entity"/> and component
        /// <see cref="Type"/> and returns <see langword="true"/> if the write
        /// is allowed, or <see langword="false"/> to deny it.
        /// </param>
        /// <remarks>
        /// <para>
        /// All registered predicates are evaluated when a write is attempted.
        /// Implementations typically combine them using logical AND
        /// (that is, all predicates must allow the write).
        /// </para>
        /// </remarks>
        void AddWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Removes a previously added write-permission predicate.
        /// </summary>
        /// <param name="hook">Previously registered predicate to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveWritePermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Clears all write-permission predicates for this world.
        /// </summary>
        /// <remarks>
        /// After calling this method, write-permission decisions fall back to
        /// the default behavior (for example, phase-based write policy only).
        /// </remarks>
        void ClearWritePermissions();

        /// <summary>
        /// Adds a read-permission predicate.
        /// </summary>
        /// <param name="hook">
        /// Predicate that receives the target <see cref="Entity"/> and component
        /// <see cref="Type"/> and returns <see langword="true"/> if the read
        /// is allowed, or <see langword="false"/> to deny it.
        /// </param>
        /// <remarks>
        /// <para>
        /// Read hooks are especially useful when implementing debug tooling or
        /// spectator modes that should not expose all entities or components.
        /// </para>
        /// </remarks>
        void AddReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Removes a previously added read-permission predicate.
        /// </summary>
        /// <param name="hook">Previously registered predicate to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the predicate was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveReadPermission(Func<Entity, Type, bool> hook);

        /// <summary>
        /// Clears all read-permission predicates for this world.
        /// </summary>
        /// <remarks>
        /// After calling this method, read-permission decisions fall back to
        /// the default behavior (for example, no additional read restrictions).
        /// </remarks>
        void ClearReadPermissions();

        /// <summary>
        /// Adds an object-level validator invoked on write attempts.
        /// </summary>
        /// <param name="hook">
            /// Validator delegate that receives a boxed component value and
        /// returns <see langword="true"/> if it is considered valid, or
        /// <see langword="false"/> to reject the write.
        /// </param>
        /// <remarks>
        /// <para>
        /// Object-level validators are type-agnostic and see the boxed value,
        /// which is useful for generic checks such as "no NaN in numeric
        /// fields" or "no nulls in nested collections".
        /// </para>
        /// <para>
        /// For type-specific rules, prefer <see cref="AddValidator{T}"/>.
        /// </para>
        /// </remarks>
        void AddValidator(Func<object, bool> hook);

        /// <summary>
        /// Removes a previously added object-level validator.
        /// </summary>
        /// <param name="hook">Previously registered validator to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveValidator(Func<object, bool> hook);

        /// <summary>
        /// Clears all object-level validators for this world.
        /// </summary>
        /// <remarks>
        /// After calling this method, only typed validators (see
        /// <see cref="AddValidator{T}"/>) contribute to validation.
        /// </remarks>
        void ClearValidators();

        /// <summary>
        /// Adds a typed validator invoked when writing components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">
        /// Predicate that receives the component value and returns
        /// <see langword="true"/> if it is valid, or <see langword="false"/> to
        /// reject the write.
        /// </param>
        /// <remarks>
        /// <para>
        /// Typed validators run before the value is written and can be combined
        /// with object-level validators. They are ideal for type-specific rules,
        /// such as clamping ranges or enforcing invariants.
        /// </para>
        /// </remarks>
        void AddValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>
        /// Removes a previously added typed validator for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">Previously registered validator to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the validator was found and removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct;

        /// <summary>
        /// Clears all typed validators for all component types in this world.
        /// </summary>
        /// <remarks>
        /// Type-specific validation is disabled until new validators are
        /// registered via <see cref="AddValidator{T}"/>.
        /// </remarks>
        void ClearTypedValidators();
    }
}
