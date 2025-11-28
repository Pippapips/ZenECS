// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — System Presets
// File: SystemTypeRef.cs
// Purpose: Runtime-safe reference to a Type, stored as an assembly-qualified
//          name, with helpers to resolve it back to a System.Type.
// Key concepts:
//   • String-based type handle: AssemblyQualifiedName storage.
//   • Editor-friendly: typically driven by a MonoScript picker in custom UI.
//   • Runtime-safe: gracefully returns null when type resolution fails.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;

namespace ZenECS.Adapter.Unity.SystemPresets
{
    /// <summary>
    /// Runtime-safe type reference stored as an assembly-qualified name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SystemTypeRef"/> is a lightweight value type that keeps the
    /// assembly-qualified name of a type (for example
    /// <c>"MyGame.MovementSystem, MyGame.Assembly"</c>) and can resolve it
    /// back to a <see cref="Type"/> at runtime.
    /// </para>
    /// <para>
    /// Typical usage:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// In the editor, a custom inspector presents a type picker (for example,
    /// a <c>MonoScript</c>-based UI) and stores the selected type's
    /// <see cref="Type.AssemblyQualifiedName"/> into
    /// <see cref="AssemblyQualifiedName"/>.
    /// </description></item>
    /// <item><description>
    /// At runtime, <see cref="Resolve"/> is called to obtain the actual
    /// <see cref="Type"/> instance, or <c>null</c> if resolution fails.
    /// </description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public struct SystemTypeRef
    {
        /// <summary>
        /// The assembly-qualified name of the referenced type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example value:
        /// <c>"MyGame.MovementSystem, MyGame.Assembly"</c>.
        /// </para>
        /// <para>
        /// This field is serialized by Unity and should normally be modified
        /// via the <see cref="AssemblyQualifiedName"/> property or through
        /// editor tooling.
        /// </para>
        /// </remarks>
        [SerializeField]
        private string _assemblyQualifiedName;

        /// <summary>
        /// Gets or sets the assembly-qualified name of the referenced type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this property is <c>null</c> or whitespace,
        /// <see cref="Resolve"/> will return <c>null</c>.
        /// </para>
        /// </remarks>
        public string AssemblyQualifiedName
        {
            readonly get => _assemblyQualifiedName;
            set => _assemblyQualifiedName = value;
        }

        /// <summary>
        /// Resolves the stored assembly-qualified name into a <see cref="Type"/>.
        /// </summary>
        /// <returns>
        /// The resolved <see cref="Type"/> if successful; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method returns <c>null</c> when:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="AssemblyQualifiedName"/> is <c>null</c>, empty, or
        /// whitespace.
        /// </description></item>
        /// <item><description>
        /// <see cref="Type.GetType(string,bool)"/> cannot resolve the type
        /// (for example, the assembly is missing or the name has changed).
        /// </description></item>
        /// </list>
        /// </remarks>
        public readonly Type? Resolve() =>
            string.IsNullOrWhiteSpace(_assemblyQualifiedName)
                ? null
                : Type.GetType(_assemblyQualifiedName, throwOnError: false);

        /// <summary>
        /// Returns the raw assembly-qualified name string for debugging purposes.
        /// </summary>
        /// <returns>
        /// The stored <see cref="AssemblyQualifiedName"/>, or
        /// <see cref="string.Empty"/> if it is <c>null</c>.
        /// </returns>
        public override readonly string ToString() => _assemblyQualifiedName ?? string.Empty;

        /// <summary>
        /// Creates a new <see cref="SystemTypeRef"/> from a concrete type.
        /// </summary>
        /// <param name="t">The type to wrap.</param>
        /// <returns>
        /// A new <see cref="SystemTypeRef"/> whose
        /// <see cref="AssemblyQualifiedName"/> is taken from
        /// <paramref name="t"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="t"/> is <c>null</c>.
        /// </exception>
        public static SystemTypeRef FromType(Type t)
            => new SystemTypeRef
            {
                _assemblyQualifiedName = (t ?? throw new ArgumentNullException(nameof(t))).AssemblyQualifiedName
            };
    }
}
