#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.DI;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Adapter.Unity.Install; // SystemsPreset
#if ZENECS_ZENJECT
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Install
{
#if ZENECS_ZENJECT
    /// <summary>
    /// Ensures a world exists, tags it, and registers systems.
    /// - If Zenject is present, systems can be instantiated via DI (SystemPresetResolver).
    /// - Otherwise, systems are created via Activator.
    /// - Merges:
    ///     * All SystemsPreset assets in <see cref="systemPresets"/>
    ///     * All installer-local <see cref="systemTypes"/>
    ///   and deduplicates by concrete Type.
    /// </summary>
    public sealed class WorldSystemInstaller : MonoInstaller
#else
    public sealed class WorldSystemInstaller : MonoBehaviour
#endif
    {
        [Tooltip("Name passed to KernelLocator.EnsureWorld.")]
        public string worldName = "Game";

        [Tooltip("Tags passed to EnsureWorld(name, tags). Used for world discovery/filtering.")]
        public string[] worldTags = Array.Empty<string>();

        [Header("Systems")]
        [Tooltip("ISystem implementation types (installer-local). Merged with all presets at runtime.")]
        [SystemTypeFilter(typeof(ISystem), allowAbstract: false)]
        public SystemTypeRef[]? systemTypes;

        [Tooltip("System preset assets to merge into this world. All types from all presets are combined.")]
        public SystemsPreset[]? systemPresets;

        private IWorld? _world;

#if ZENECS_ZENJECT
        public override void InstallBindings()
        {
            _world = ResolveWorld();
        }

        private void Awake()
        {
            AddSystems();
        }
#else
        private void Awake()
        {
            _world = ResolveWorld();
            AddSystems();
        }
#endif

        // ───────────────────────────────── World resolution ─────────────────────────────────

        private IWorld ResolveWorld()
        {
            var tags = (IEnumerable<string>)(worldTags ?? Array.Empty<string>());

            if (!string.IsNullOrWhiteSpace(worldName))
            {
                // new signature: IWorld EnsureWorld(string name, IEnumerable<string> tags, bool setCurrent = true)
                return KernelLocator.EnsureWorld(worldName.Trim(), tags, setCurrent: true);
            }

            // Fallback: use current world as-is.
            return KernelLocator.CurrentWorld;
        }

        // ───────────────────────────────── System registration ─────────────────────────────────

        private void AddSystems()
        {
            if (_world == null)
            {
                Debug.LogWarning("[WorldSystemInstaller] World is null; systems are not registered.");
                return;
            }

            var types = CollectDistinctTypes();
            if (types.Count == 0)
                return;

#if ZENECS_ZENJECT
            // Prefer the central SystemPresetResolver if available
            var instances = ZenEcsUnityBridge.SystemPresetResolver?.InstantiateSystems(types)
                            ?? InstantiateSystemsActivator(types);
#else
            var instances = InstantiateSystemsActivator(types);
#endif
            if (instances != null && instances.Count > 0)
            {
                _world.AddSystems(instances); // Will be applied on next BeginFrame
            }
        }

        /// <summary>
        /// Collects all concrete ISystem types from:
        /// - all <see cref="systemPresets"/> (SystemsPreset assets)
        /// - all <see cref="systemTypes"/> (installer-local)
        /// and returns a distinct list (deduplicated by assembly-qualified name).
        /// </summary>
        private List<Type> CollectDistinctTypes()
        {
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
                        Debug.LogWarning($"[WorldSystemInstaller] Failed to read SystemsPreset '{preset.name}': {ex.Message}", preset);
                    }
                }
            }

            // 2) From installer-local systemTypes
            if (systemTypes != null)
            {
                foreach (var r in systemTypes)
                {
                    var t = r.Resolve();
                    AddDistinct(t, seen, list);
                }
            }

            return list;

            static void AddDistinct(Type? t, HashSet<string> visited, List<Type> dst)
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

        // ───────────────────────────────── System instantiation ─────────────────────────────────

        private static List<ISystem> InstantiateSystemsActivator(List<Type> types)
        {
            var list = new List<ISystem>(types.Count);
            foreach (var t in types)
            {
                try
                {
                    if (t == null || t.IsAbstract) continue;
                    var inst = Activator.CreateInstance(t);
                    if (inst is ISystem s)
                        list.Add(s);
                    else
                        Debug.LogWarning($"[WorldSystemInstaller] Type '{t.FullName}' is not an ISystem.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldSystemInstaller] new() failed: {t?.Name} — {ex.Message}");
                }
            }
            return list;
        }
    }
}
