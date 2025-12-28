// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization
// File: PostLoadMigrationRegistry.cs
// Purpose: Register and execute post-load migrations after snapshot load.
// Key concepts:
//   • Uniqueness: one instance per migration type.
//   • Deterministic: ordered by Order, then type name.
//   • Test-friendly: Clear() to reset state in unit tests/tools.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Central registry responsible for holding and executing post-load migrations.
    /// </summary>
    public static class PostLoadMigrationRegistry
    {
        private static readonly List<IPostLoadMigration> _migs = new();
        private static readonly HashSet<Type> _migTypes = new();

        /// <summary>
        /// Register a migration instance. Duplicate types are ignored.
        /// </summary>
        /// <param name="mig">Migration instance.</param>
        /// <returns><see langword="true"/> if registered; <see langword="false"/> if ignored.</returns>
        public static bool Register(IPostLoadMigration mig)
        {
            if (mig == null) return false;
            var t = mig.GetType();
            if (_migTypes.Contains(t)) return false;

            _migTypes.Add(t);
            _migs.Add(mig);
            return true;
        }

        /// <summary>
        /// Check whether a migration of type <typeparamref name="T"/> is already registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : IPostLoadMigration
            => _migTypes.Contains(typeof(T));

        /// <summary>
        /// Ensure a migration exists by registering a newly created instance if absent.
        /// </summary>
        public static bool EnsureRegistered<T>(Func<T> factory) where T : class, IPostLoadMigration
        {
            if (IsRegistered<T>()) return false;
            var instance = factory();
            return Register(instance);
        }

        /// <summary>
        /// Execute all registered migrations in deterministic order.
        /// </summary>
        /// <param name="world">Target world.</param>
        public static void RunAll(IWorld world)
        {
            if (_migs.Count == 0) return;

            foreach (var m in _migs
                     .OrderBy(m => m.Order)
                     .ThenBy(m => m.GetType().FullName, StringComparer.Ordinal))
            {
                m.Run(world);
            }
        }

        /// <summary>
        /// Remove all registered migrations and type keys. Intended for tests/resets.
        /// </summary>
        public static void Clear()
        {
            _migs.Clear();
            _migTypes.Clear();
        }
    }
}
