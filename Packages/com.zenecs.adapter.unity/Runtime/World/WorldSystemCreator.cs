// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — DI
// File: WorldSystemInstaller.cs
// Purpose: Ensure a ZenECS world exists, tag it, and register systems from
//          local type references and/or SystemsPreset assets.
// Key concepts:
//   • World bootstrap: uses KernelLocator.EnsureWorld to create/find a world.
//   • System discovery: merges installer-local types and preset assets.
//   • Deduplication: distinct ISystem types by assembly-qualified name.
//   • Dual mode:
//       - With Zenject: systems can be instantiated via ISystemPresetResolver.
//       - Without Zenject: systems are created via Activator.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using ZenECS.Core.Systems;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Ensures a world exists, tags it, and registers systems for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WorldSystemCreator"/> is a scene-level bootstrap that:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Ensures a world instance exists via
    /// <see cref="KernelLocator.EnsureWorld(string,System.Collections.Generic.IEnumerable{string},bool)"/>.
    /// </description></item>
    /// <item><description>
    /// Merges system types from <see cref="systemPresets"/> and
    /// <see cref="systemTypes"/>, deduplicating by concrete type.
    /// </description></item>
    /// <item><description>
        /// Instantiates the systems (via DI or <see cref="Activator"/>) and
        /// registers them into the world using <see cref="IWorldSystemsApi.AddSystems"/>.
    /// </description></item>
    /// </list>
    /// <para>
        /// When the <c>ZENECS_ZENJECT</c> scripting define is set, the installer
        /// inherits from Zenject MonoInstaller and can resolve systems via
        /// an <see cref="ISystemPresetResolver"/> (typically bound in
        /// <see cref="ZenECS.Adapter.Unity.DI.ProjectInstaller"/>). Without Zenject, it falls back to
        /// a lightweight <see cref="MonoBehaviour"/> that uses
        /// <see cref="Activator.CreateInstance(Type)"/> for system construction.
    /// </para>
    /// </remarks>
    public sealed class WorldSystemCreator : MonoBehaviour
    {
        /// <summary>
        /// Name passed to <see cref="KernelLocator.EnsureWorld(string,System.Collections.Generic.IEnumerable{string},bool)"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If non-empty, a world with this name is created or retrieved. If
        /// empty or whitespace, <see cref="KernelLocator.CurrentWorld"/> is
        /// used instead.
        /// </para>
        /// </remarks>
        [Tooltip("Name passed to KernelLocator.EnsureWorld.")]
        public string worldName = "Game";

        /// <summary>
        /// Tags associated with the world for discovery and filtering.
        /// </summary>
        /// <remarks>
        /// These tags are passed to
        /// <see cref="KernelLocator.EnsureWorld(string,System.Collections.Generic.IEnumerable{string},bool)"/>
        /// and can be used later for querying or grouping worlds.
        /// </remarks>
        [Tooltip("Tags passed to EnsureWorld(name, tags). Used for world discovery/filtering.")]
        public string[] worldTags = Array.Empty<string>();

        /// <summary>
        /// Installer-local system types to be added to the world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This array is merged with all types provided by
        /// <see cref="systemPresets"/> at runtime. The combined set is
        /// deduplicated before instantiation.
        /// </para>
        /// <para>
        /// Only non-abstract types assignable to <see cref="ISystem"/> are
        /// considered valid.
        /// </para>
        /// </remarks>
        [Header("Systems")]
        [Tooltip("ISystem implementation types (installer-local). Merged with all presets at runtime.")]
        [SystemTypeFilter(typeof(ISystem), allowAbstract: false)]
        public SystemTypeRef[]? systemTypes;

        /// <summary>
        /// System preset assets to merge into this world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each <see cref="SystemsPreset"/> contributes a set of valid
        /// <see cref="ISystem"/> types (via
        /// <see cref="SystemsPreset.GetValidTypes"/>). All types from all
        /// presets are merged with <see cref="systemTypes"/> to form the final
        /// registration list.
        /// </para>
        /// </remarks>
        [Tooltip("System preset assets to merge into this world. All types from all presets are combined.")]
        public SystemsPreset[]? systemPresets;

        /// <summary>
        /// The resolved or created world that systems will be added to.
        /// </summary>
        private IWorld? _world;

        /// <summary>
        /// Optional resolver used to instantiate systems when Zenject is
        /// available. This field is set via dependency injection when
        /// <c>ZENECS_ZENJECT</c> is defined.
        /// </summary>
#pragma warning disable CS0649 // Field is set via Zenject [Inject] attribute
        private ISystemPresetResolver? _worldPresetResolver;
#pragma warning restore CS0649

#if ZENECS_ZENJECT
        /// <summary>
        /// Zenject injection hook used to resolve dependencies before system
        /// creation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by Zenject during dependency injection to
        /// resolve the optional <see cref="ISystemPresetResolver"/>. The world
        /// is resolved or created in <see cref="Awake"/> after all dependencies
        /// have been injected.
        /// </para>
        /// </remarks>
        [Inject]
        void Initialize([InjectOptional] ISystemPresetResolver worldPresetResolver)
        {
            _worldPresetResolver = worldPresetResolver;
        }
#endif
        
        /// <summary>
        /// Unity lifecycle callback used to resolve the world and register
        /// systems when Zenject is not in use.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In non-Zenject mode, systems are instantiated using
        /// <see cref="Activator.CreateInstance(Type)"/>.
        /// </para>
        /// </remarks>
        private void Awake()
        {
            _world = ResolveWorld();
            AddSystems();
        }

        // ───────────────────────────────── World resolution ─────────────────────────────────

        /// <summary>
        /// Resolves or creates the target world for this installer.
        /// </summary>
        /// <returns>
        /// The <see cref="IWorld"/> instance that systems should be added to.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If <see cref="worldName"/> is not null or whitespace, this method
        /// calls <see cref="KernelLocator.EnsureWorld(string,System.Collections.Generic.IEnumerable{string},bool)"/>
        /// with <see cref="worldTags"/> and <c>setCurrent: true</c>.
        /// Otherwise, it returns <see cref="KernelLocator.CurrentWorld"/>.
        /// </para>
        /// </remarks>
        private IWorld ResolveWorld()
        {
            var tags = (IEnumerable<string>)(worldTags ?? Array.Empty<string>());

            if (!string.IsNullOrWhiteSpace(worldName))
            {
                // New signature: IWorld EnsureWorld(string name, IEnumerable<string> tags, bool setCurrent = true)
                return KernelLocator.EnsureWorld(worldName.Trim(), tags, setCurrent: true);
            }

            // Fallback: use current world as-is.
            return KernelLocator.CurrentWorld;
        }

        // ───────────────────────────────── System registration ─────────────────────────────────

        /// <summary>
        /// Collects system types, instantiates them, and adds them to the world.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// Validates that <see cref="_world"/> is non-null.
        /// </description></item>
        /// <item><description>
        /// Collects distinct system types using <see cref="CollectDistinctTypes"/>.
        /// </description></item>
        /// <item><description>
        /// Instantiates systems either via
        /// <see cref="ISystemPresetResolver.InstantiateSystems"/> (Zenject
        /// mode) or <see cref="Activator.CreateInstance(Type)"/>.
        /// </description></item>
        /// <item><description>
        /// Adds the instantiated systems to the world via
        /// <see cref="IWorldSystemsApi.AddSystems"/>.
        /// </description></item>
        /// </list>
        /// </remarks>
        private void AddSystems()
        {
            if (_world == null)
            {
                Debug.LogWarning("[WorldSystemInstaller] World is null; systems are not registered.");
                return;
            }

            var types = CollectDistinctTypes(out var errors);
            if (errors.Count > 0)
            {
                Debug.LogWarning(
                    $"[WorldSystemCreator] Encountered {errors.Count} error(s) while collecting system types. " +
                    "Some systems may not be registered. Check the console for details.");
            }

            if (types.Count == 0)
            {
                // Only log if there were errors during collection; otherwise it's a normal
                // scenario where no systems are configured yet.
                if (errors.Count > 0)
                {
                    Debug.LogWarning(
                        "[WorldSystemCreator] No system types found to register after processing. " +
                        "Check systemPresets and systemTypes fields in the inspector.");
                }
                return;
            }

            // Prefer the central SystemPresetResolver if available.
            var instances = _worldPresetResolver?.InstantiateSystems(types);
            if (instances?.Count > 0)
            {
                // Systems will be activated on the next BeginFrame.
                _world.AddSystems(instances);
            }
        }

        /// <summary>
        /// Collects all concrete <see cref="ISystem"/> types from presets and
        /// installer-local settings, returning a distinct list.
        /// </summary>
        /// <param name="errors">
        /// When this method returns, contains a list of error messages encountered
        /// during type collection. This list will be empty if no errors occurred.
        /// </param>
        /// <returns>
        /// A list of distinct, non-abstract system types that implement
        /// <see cref="ISystem"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Sources:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// All <see cref="SystemsPreset"/> assets in <see cref="systemPresets"/>.
        /// </description></item>
        /// <item><description>
        /// All <see cref="SystemTypeRef"/> entries in
        /// <see cref="systemTypes"/>.
        /// </description></item>
        /// </list>
        /// <para>
        /// Types are deduplicated using their assembly-qualified name as the
        /// key. Abstract types or types not assignable to
        /// <see cref="ISystem"/> are ignored.
        /// </para>
        /// <para>
        /// Errors encountered during collection are logged and added to the
        /// <paramref name="errors"/> list, but do not prevent the collection
        /// process from continuing.
        /// </para>
        /// </remarks>
        private List<Type> CollectDistinctTypes(out List<string> errors)
        {
            errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<Type>();

            // 1) From presets
            if (systemPresets != null)
            {
                foreach (var preset in systemPresets)
                {
                    if (preset == null) continue;

                    try
                    {
                        var types = preset.GetValidTypes();
                        foreach (var t in types)
                            AddDistinct(t, seen, list);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"[WorldSystemCreator] Failed to read SystemsPreset '{preset.name}': {ex.Message}";
                        errors.Add(errorMsg);
                        Debug.LogWarning(errorMsg, preset);
                    }
                }
            }

            // 2) From installer-local systemTypes
            if (systemTypes != null)
            {
                foreach (var r in systemTypes)
                {
                    try
                    {
                        var t = r.Resolve();
                        AddDistinct(t, seen, list);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"[WorldSystemCreator] Failed to resolve SystemTypeRef: {ex.Message}";
                        errors.Add(errorMsg);
                        Debug.LogWarning(errorMsg);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Adds a system type to the list if it is valid and not yet seen.
        /// </summary>
        /// <param name="t">Candidate type to add.</param>
        /// <param name="visited">Set of type keys that have already been added.</param>
        /// <param name="dst">Destination list of system types.</param>
        private static void AddDistinct(Type? t, HashSet<string> visited, List<Type> dst)
        {
            if (t == null) return;
            if (t.IsAbstract) return;
            if (!typeof(ISystem).IsAssignableFrom(t)) return;

            var key = t.AssemblyQualifiedName ?? t.FullName;
            if (string.IsNullOrEmpty(key)) return;
            if (visited.Add(key))
                dst.Add(t);
        }
    }
}
