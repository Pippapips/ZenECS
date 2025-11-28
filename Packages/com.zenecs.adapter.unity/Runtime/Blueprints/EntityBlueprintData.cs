// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Blueprints
// File: EntityBlueprintData.cs
// Purpose: Pure runtime data container that stores a list of component
//          snapshot entries for an entity blueprint.
// Key concepts:
//   • Serializable runtime DTO, usable outside ScriptableObject context.
//   • Each entry stores component type name and JSON snapshot.
//   • Provides ApplyTo(world, entity, cmd) to instantiate components.
//   • Type resolution helper that searches all loaded assemblies.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// Runtime blueprint data container for component snapshots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="EntityBlueprintData"/> is a pure data object that can be
    /// serialized and used both inside and outside Unity ScriptableObjects.
    /// It represents a list of component snapshots that can be applied to an
    /// entity in a given <see cref="IWorld"/>.
    /// </para>
    /// <para>
    /// Each snapshot entry stores:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="Entry.typeName"/>: the assembly-qualified component type name.
    /// </description></item>
    /// <item><description>
    /// <see cref="Entry.json"/>: JSON encoded by
    /// <see cref="EntityBlueprintComponentJson.Serialize"/>.
    /// </description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public sealed class EntityBlueprintData
    {
        /// <summary>
        /// Represents a single serialized component snapshot entry.
        /// </summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>
            /// Assembly-qualified component type name (recommended).
            /// </summary>
            public string? typeName;

            /// <summary>
            /// JSON snapshot string produced by
            /// <see cref="EntityBlueprintComponentJson.Serialize"/>.
            /// </summary>
            public string? json;
        }

        /// <summary>
        /// Collection of serialized component entries in this blueprint.
        /// </summary>
        public List<Entry> entries = new();

        /// <summary>
        /// Applies all stored component snapshots to an entity.
        /// </summary>
        /// <param name="world">
        /// Target <see cref="IWorld"/> that owns <paramref name="e"/>.
        /// </param>
        /// <param name="e">
        /// The entity to which components should be added.
        /// </param>
        /// <param name="cmd">
        /// Command buffer used to add components in a write-safe manner.
        /// </param>
        /// <remarks>
        /// <para>
        /// For each <see cref="Entry"/> in <see cref="entries"/>:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// Resolve the component type via <see cref="Resolve"/>.
        /// </description></item>
        /// <item><description>
        /// Deserialize the JSON snapshot using
        /// <see cref="EntityBlueprintComponentJson.Deserialize"/>.
        /// </description></item>
        /// <item><description>
        /// Add the boxed component to the entity via
        /// <see cref="ICommandBuffer.AddComponentBoxed"/>.
        /// </description></item>
        /// </list>
        /// <para>
        /// Entries with invalid <see cref="Entry.typeName"/> or unresolved
        /// types are skipped.
        /// </para>
        /// </remarks>
        public void ApplyTo(IWorld world, Entity e, ICommandBuffer cmd)
        {
            foreach (var it in entries)
            {
                if (string.IsNullOrEmpty(it?.typeName)) continue;
                var t = Resolve(it.typeName);
                if (t == null) continue;
                var boxed = EntityBlueprintComponentJson.Deserialize(it.json, t);
                cmd.AddComponentBoxed(e, boxed);
            }
        }

        /// <summary>
        /// Resolves a type from an assembly-qualified type name.
        /// </summary>
        /// <param name="typeName">
        /// The assembly-qualified type name to resolve.
        /// </param>
        /// <returns>
        /// The resolved <see cref="Type"/> if found; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method first calls <see cref="Type.GetType(string,bool)"/> with
        /// <c>throwOnError = false</c>. If that returns <c>null</c>, it scans
        /// all assemblies in <see cref="AppDomain.CurrentDomain"/> and returns
        /// the first type that matches <paramref name="typeName"/>.
        /// </para>
        /// </remarks>
        public static Type? Resolve(string typeName)
        {
            var t = Type.GetType(typeName, false);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetType(typeName, false) is { } tt) return tt;
            return null;
        }
    }
}
