// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — System Presets
// File: SystemsPreset.cs
// Purpose: ScriptableObject asset that stores a curated list of ISystem types
//          which can be instantiated and registered into a world at runtime.
// Key concepts:
//   • Authoring-time list of ISystem implementations.
//   • Safe runtime resolution via SystemTypeRef.Resolve().
//   • Validation & deduplication in OnValidate for clean inspector state.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.SystemPresets
{
    /// <summary>
    /// ScriptableObject preset that holds a list of <see cref="ISystem"/> types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="SystemsPreset"/> is typically referenced by world
    /// installers (for example, <c>WorldSystemInstaller</c>) to contribute a
    /// set of systems that should be created and added to a world.
    /// </para>
    /// <para>
    /// Each entry in <see cref="systemTypes"/> is a <see cref="SystemTypeRef"/>
    /// that resolves to a concrete <see cref="ISystem"/> implementation type.
    /// The preset provides <see cref="GetValidTypes"/> to enumerate only those
    /// types that are resolvable, non-abstract, and assignable to
    /// <see cref="ISystem"/>.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "SystemsPreset", menuName = "ZenECS/Systems Preset")]
    public sealed class SystemsPreset : ScriptableObject
    {
        /// <summary>
        /// List of system type references that this preset provides.
        /// </summary>
        /// <remarks>
        /// <para>
        /// At runtime, each <see cref="SystemTypeRef"/> is resolved via
        /// <see cref="SystemTypeRef.Resolve"/> and filtered for validity by
        /// <see cref="GetValidTypes"/>.
        /// </para>
        /// <para>
        /// In the editor, Unity's OnValidate performs additional cleanup:
        /// it preserves empty slots for future selection while removing
        /// invalid or duplicate entries.
        /// </para>
        /// </remarks>
        [Header("Systems")]
        [Tooltip("ISystem implementation types (runtime-safe).")]
        [SystemTypeFilter(typeof(ISystem), allowAbstract: false)]
        public SystemTypeRef[]? systemTypes;

        /// <summary>
        /// Enumerates all valid <see cref="ISystem"/> types referenced by this preset.
        /// </summary>
        /// <returns>
        /// A sequence of concrete, non-abstract types that implement
        /// <see cref="ISystem"/> and can safely be instantiated.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A type is considered valid if:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="SystemTypeRef.Resolve"/> returns a non-null <see cref="Type"/>.
        /// </description></item>
        /// <item><description>
        /// The type is not abstract.
        /// </description></item>
        /// <item><description>
        /// The type is assignable to <see cref="ISystem"/>.
        /// </description></item>
        /// </list>
        /// </remarks>
        public IEnumerable<Type> GetValidTypes()
        {
            if (systemTypes == null) yield break;

            foreach (var tr in systemTypes)
            {
                var t = tr.Resolve();
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                yield return t;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity editor validation hook used to clean up the system type list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Preserves empty entries (where <see cref="SystemTypeRef.AssemblyQualifiedName"/>
        /// is null or whitespace) so that they remain available for later
        /// selection in the inspector.
        /// </description></item>
        /// <item><description>
        /// Removes entries that cannot be resolved, are abstract, or do not
        /// implement <see cref="ISystem"/>.
        /// </description></item>
        /// <item><description>
        /// Filters out duplicates based on the assembly-qualified name.
        /// </description></item>
        /// </list>
        /// <para>
        /// The resulting cleaned list is written back to <see cref="systemTypes"/>.
        /// </para>
        /// </remarks>
        private void OnValidate()
        {
            if (systemTypes == null || systemTypes.Length == 0) return;

            var list = new List<SystemTypeRef>(systemTypes.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var r in systemTypes)
            {
                var aqn = r.AssemblyQualifiedName;

                // Preserve empty slots for future selection in the inspector.
                if (string.IsNullOrWhiteSpace(aqn))
                {
                    list.Add(r);
                    continue;
                }

                var t = r.Resolve();
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                if (!seen.Add(aqn)) continue;

                list.Add(r);
            }

            systemTypes = list.ToArray();
        }
#endif
    }
}
