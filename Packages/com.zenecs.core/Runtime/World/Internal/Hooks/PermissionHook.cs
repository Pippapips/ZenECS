// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Hooks
// File: PermissionHook.cs
// Purpose: World-scoped implementation of read/write permissions and validators.
// Key concepts:
//   • List-based predicates; all must pass (AND) to allow the operation
//   • Per-type validator cache to avoid boxing on hot paths
//   • Clear… helpers to reset during teardown or tests
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Hooking.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IPermissionHook"/> used by the world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All registered predicates and validators are combined using logical AND:
    /// if any predicate returns <see langword="false"/>, the corresponding
    /// operation (read, write, or validation) is rejected.
    /// </para>
    /// <para>
    /// This implementation is intentionally simple and allocation-friendly:
    /// it uses plain lists for hook storage and a small dictionary for typed
    /// validator caches.
    /// </para>
    /// </remarks>
    internal sealed class PermissionHook : IPermissionHook
    {
        // ---- Hook storage ----------------------------------------------------
        private readonly List<Func<Entity, Type, bool>> _writePerms = new(2);
        private readonly List<Func<Entity, Type, bool>> _readPerms  = new(1);
        private readonly List<Func<object, bool>>       _objValidators = new(2);

        // ---- Write permissions ----------------------------------------------

        /// <inheritdoc/>
        public void AddWritePermission(Func<Entity, Type, bool> hook)
        {
            if (hook != null) _writePerms.Add(hook);
        }

        /// <inheritdoc/>
        public bool RemoveWritePermission(Func<Entity, Type, bool> hook)
            => _writePerms.Remove(hook);

        /// <inheritdoc/>
        public void ClearWritePermissions() => _writePerms.Clear();

        // ---- Read permissions ------------------------------------------------

        /// <inheritdoc/>
        public void AddReadPermission(Func<Entity, Type, bool> hook)
        {
            if (hook != null) _readPerms.Add(hook);
        }

        /// <inheritdoc/>
        public bool RemoveReadPermission(Func<Entity, Type, bool> hook)
            => _readPerms.Remove(hook);

        /// <inheritdoc/>
        public void ClearReadPermissions() => _readPerms.Clear();

        // ---- Object-level validators ----------------------------------------

        /// <inheritdoc/>
        public void AddValidator(Func<object, bool> hook)
        {
            if (hook != null) _objValidators.Add(hook);
        }

        /// <inheritdoc/>
        public bool RemoveValidator(Func<object, bool> hook)
            => _objValidators.Remove(hook);

        /// <inheritdoc/>
        public void ClearValidators() => _objValidators.Clear();

        // ---- Typed validators cache -----------------------------------------

        /// <summary>
        /// Internal interface for invoking typed validators through a boxed path.
        /// </summary>
        /// <remarks>
        /// This interface allows the permission hook to store and invoke
        /// strongly-typed validators (<see cref="TypeValidator{T}"/>) through
        /// a common untyped interface, avoiding boxing on the hot path while
        /// maintaining a unified storage mechanism.
        /// </remarks>
        private interface IBoxedTypeValidator
        {
            /// <summary>
            /// Invokes the validator with a boxed value.
            /// </summary>
            /// <param name="value">Boxed value to validate.</param>
            /// <returns>
            /// <see langword="true"/> if all validators accept the value;
            /// otherwise <see langword="false"/>.
            /// </returns>
            bool InvokeBoxed(object value);
        }

        /// <summary>
        /// Typed validator bucket for a specific value type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <remarks>
        /// <para>
        /// This class maintains a list of strongly-typed predicates for component
        /// type <typeparamref name="T"/>. All predicates must return
        /// <see langword="true"/> for a value to be considered valid.
        /// </para>
        /// <para>
        /// The typed storage avoids boxing when validating values of type
        /// <typeparamref name="T"/>, improving performance on hot paths.
        /// </para>
        /// </remarks>
        private sealed class TypeValidator<T> : IBoxedTypeValidator where T : struct
        {
            private readonly List<Func<T, bool>> _preds = new(2);

            /// <summary>
            /// Adds a new predicate to this validator bucket.
            /// </summary>
            /// <param name="p">Predicate to add.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Func<T, bool> p) => _preds.Add(p);

            /// <summary>
            /// Removes a predicate from this validator bucket.
            /// </summary>
            /// <param name="p">Predicate to remove.</param>
            /// <returns>
            /// <see langword="true"/> if the predicate was found and removed;
            /// otherwise <see langword="false"/>.
            /// </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Remove(Func<T, bool> p) => _preds.Remove(p);

            /// <summary>
            /// Invokes all validators against the typed value.
            /// </summary>
            /// <param name="v">Value to validate.</param>
            /// <returns>
            /// <see langword="true"/> if all predicates accept the value;
            /// otherwise <see langword="false"/>.
            /// </returns>
            public bool Invoke(in T v)
            {
                for (int i = 0; i < _preds.Count; i++)
                    if (!_preds[i](v)) return false;
                return true;
            }

            /// <inheritdoc/>
            bool IBoxedTypeValidator.InvokeBoxed(object value)
                => value is T v && Invoke(in v);
        }

        /// <summary>
        /// Cache of per-type validator buckets keyed by value type.
        /// </summary>
        private readonly Dictionary<Type, object> _typedValidators = new(64);

        /// <summary>
        /// Ensures that a <see cref="TypeValidator{T}"/> exists for the given
        /// type <typeparamref name="T"/> and returns it.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TypeValidator<T> EnsureTypeValidator<T>() where T : struct
        {
            var t = typeof(T);
            if (!_typedValidators.TryGetValue(t, out var obj))
            {
                obj = new TypeValidator<T>();
                _typedValidators[t] = obj;
            }
            return (TypeValidator<T>)obj;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct
            => EnsureTypeValidator<T>().Add(predicate);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct
            => EnsureTypeValidator<T>().Remove(predicate);

        /// <inheritdoc/>
        public void ClearTypedValidators() => _typedValidators.Clear();

        // ---- Evaluators ------------------------------------------------------

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EvaluateWritePermission(Entity e, Type t)
        {
            for (int i = 0; i < _writePerms.Count; i++)
                if (!_writePerms[i](e, t)) return false;
            return true;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EvaluateReadPermission(Entity e, Type t)
        {
            for (int i = 0; i < _readPerms.Count; i++)
                if (!_readPerms[i](e, t)) return false;
            return true;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateObject(object value)
        {
            for (int i = 0; i < _objValidators.Count; i++)
                if (!_objValidators[i](value)) return false;
            return true;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateTyped<T>(in T value) where T : struct
        {
            if (_typedValidators.TryGetValue(typeof(T), out var obj))
                return ((TypeValidator<T>)obj).Invoke(in value);
            return true;
        }

        // ---- Maintenance -----------------------------------------------------

        /// <inheritdoc/>
        public void ClearAllHookQueues()
        {
            ClearWritePermissions();
            ClearReadPermissions();
            ClearValidators();
            ClearTypedValidators();
        }
    }
}