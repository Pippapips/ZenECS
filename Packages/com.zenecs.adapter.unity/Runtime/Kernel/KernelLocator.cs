// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity
// File: KernelLocator.cs
// Purpose: Global gateway for locating/creating the ZenECS kernel and worlds,
//          plus multi-world lookup helpers for names, tags, and IDs.
// Key concepts:
//   • Kernel resolution: prefer ZenEcsUnityBridge, then scene EcsDriver,
//     otherwise auto-create a driver GameObject.
//   • Current world access: convenience helpers over IKernel.CurrentWorld.
//   • World discovery: find/ensure worlds by id, name, prefix, or tags.
//   • Snapshot views: dictionary-style projections of all known worlds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using ZenECS.Core;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Global-access gateway for <see cref="IKernel"/> and multi-world helpers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kernel resolution priority:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// If <see cref="ZenEcsUnityBridge.Kernel"/> is already set, it is used.
    /// </description></item>
    /// <item><description>
    /// Otherwise, search the scene for an <see cref="EcsDriver"/> and use its
    /// <see cref="EcsDriver.Kernel"/> if available.
    /// </description></item>
    /// <item><description>
        /// If neither exists, automatically create a new <see cref="EcsDriver"/>
        /// GameObject (optionally marked with DontDestroyOnLoad)
        /// and create a kernel from it.
    /// </description></item>
    /// </list>
    /// <para>
    /// The class also provides helpers for discovering and creating worlds by
    /// name or tags, as well as snapshot-style dictionary views of all worlds.
    /// </para>
    /// </remarks>
    public static class KernelLocator
    {
        private static IKernel? _cached;

        /// <summary>
        /// Attempts to get the current active ZenECS kernel without throwing an exception.
        /// </summary>
        /// <param name="kernel">
        /// When this method returns, contains the resolved kernel if found or created;
        /// otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a kernel was successfully obtained or created; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Resolution order:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// Cached kernel stored in this locator.
        /// </description></item>
        /// <item><description>
        /// <see cref="ZenEcsUnityBridge.Kernel"/>, if assigned.
        /// </description></item>
        /// <item><description>
        /// <see cref="EcsDriver"/> in the scene (first found). If the driver
        /// exists but its Kernel property is <c>null</c>, the
        /// kernel is automatically created via the driver's CreateKernel method.
        /// </description></item>
        /// <item><description>
        /// Automatic creation of an <see cref="EcsDriver"/> via
        /// <see cref="CreateEcsDriverWithKernel(KernelOptions?, bool)"/>, which also creates a
        /// kernel instance.
        /// </description></item>
        /// </list>
        /// </remarks>
        public static bool TryGetCurrent(out IKernel? kernel)
        {
            // Validate cached kernel before using it (check if it's been disposed)
            if (_cached != null)
            {
                // Check if the cached kernel is still valid (not disposed)
                // IKernel implements IDisposable, so we can check IsRunning as a proxy for validity
                if (_cached.IsRunning)
                {
                    kernel = _cached;
                    return true;
                }
                // Cached kernel is no longer valid, clear it
                _cached = null;
            }

            // 1) Use the kernel registered on the bridge, if any.
            if (ZenEcsUnityBridge.Kernel != null)
            {
                kernel = _cached = ZenEcsUnityBridge.Kernel;
                return true;
            }

            // 2) Find an EcsDriver in the scene and use its kernel.
#if UNITY_2022_2_OR_NEWER
            var drv = UnityEngine.Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
            var drv = UnityEngine.Object.FindObjectOfType<EcsDriver>(true);
#endif
            if (drv != null)
            {
                // If EcsDriver exists but Kernel is not yet created, create it now.
                // CreateKernel() will automatically update ZenEcsUnityBridge.Kernel.
                if (drv.Kernel == null)
                {
                    var createdKernel = drv.CreateKernel();
                    kernel = _cached = createdKernel;
                    return true;
                }
                
                // Kernel already exists, use it.
                kernel = _cached = drv.Kernel;
                return true;
            }

            // 3) Auto-create a driver and kernel if nothing exists.
            var autoCreatedKernel = CreateEcsDriverWithKernel();
            if (autoCreatedKernel != null)
            {
                kernel = _cached = autoCreatedKernel;
                return true;
            }

            kernel = null;
            return false;
        }

        /// <summary>
        /// Gets the current active ZenECS kernel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property uses <see cref="TryGetCurrent(out IKernel?)"/> internally.
        /// If no kernel can be obtained or created, an <see cref="InvalidOperationException"/>
        /// is thrown. For safe access without exceptions, use <see cref="TryGetCurrent(out IKernel?)"/>
        /// instead.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no kernel can be obtained or created.
        /// </exception>
        public static IKernel Current =>
            TryGetCurrent(out var k) && k != null ? k : throw new InvalidOperationException(
                "[KernelLocator] No ZenECS kernel is available. " +
                "Ensure there is a ProjectInstaller/EcsDriver in the scene or call CreateEcsDriverWithKernel() manually.");

        /// <summary>
        /// Creates a new <see cref="EcsDriver"/> GameObject and optionally
        /// creates its kernel.
        /// </summary>
        /// <param name="options">
        /// Optional <see cref="KernelOptions"/> used when creating the kernel.
        /// When <c>null</c>, default options are used.
        /// </param>
        /// <param name="dontDestroyOnLoad">
        /// If <c>true</c>, the created GameObject is marked with
        /// <see cref="UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)"/>.
        /// </param>
        /// <returns>
        /// The created <see cref="IKernel"/> instance, or <c>null</c> if
        /// kernel creation failed.
        /// </returns>
        public static IKernel? CreateEcsDriver(KernelOptions? options = null, bool dontDestroyOnLoad = true)
        {
            var go = new GameObject("[ZenECS] EcsDriver (auto)");
            var drv = go.AddComponent<EcsDriver>();

            if (dontDestroyOnLoad && drv.gameObject != null)
            {
                UnityEngine.Object.DontDestroyOnLoad(drv.gameObject);
            }

            return drv.CreateKernel(options);
        }

        /// <summary>
        /// Convenience helper that creates an <see cref="EcsDriver"/> and
        /// immediately initializes its kernel.
        /// </summary>
        /// <param name="options">
        /// Optional <see cref="KernelOptions"/> to use for kernel creation.
        /// </param>
        /// <param name="dontDestroyOnLoad">
        /// Whether to mark the created GameObject as non-destructible on load.
        /// </param>
        /// <returns>
        /// The created <see cref="IKernel"/> instance, or <c>null</c> if
        /// kernel creation failed.
        /// </returns>
        public static IKernel? CreateEcsDriverWithKernel(KernelOptions? options = null, bool dontDestroyOnLoad = true)
        {
            return CreateEcsDriver(options, dontDestroyOnLoad);
        }

        /// <summary>
        /// Gets the current world from the active kernel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is a convenience wrapper around
        /// <see cref="IKernel.CurrentWorld"/> and throws if it is <c>null</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="IKernel.CurrentWorld"/> is <c>null</c>.
        /// </exception>
        public static IWorld CurrentWorld =>
            Current.CurrentWorld ?? throw new InvalidOperationException("Kernel.CurrentWorld is null");

        /// <summary>
        /// Caches the specified kernel instance as the current one for this locator.
        /// </summary>
        /// <param name="k">Kernel instance to cache.</param>
        internal static void Attach(IKernel k) => _cached = k;

        /// <summary>
        /// Clears the cached kernel if it matches the given instance.
        /// </summary>
        /// <param name="k">Kernel instance that is being detached.</param>
        internal static void Detach(IKernel k)
        {
            if (ReferenceEquals(_cached, k))
                _cached = null;
        }

        /// <summary>
        /// Unity domain reload hook that resets the cached kernel reference
        /// before any scenes are loaded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is only executed in Unity runtime. In non-Unity environments
        /// (such as DocFX builds), the attribute is provided by Unity stubs and has
        /// no effect.
        /// </para>
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnDomainReload() => _cached = null;

        // ──────────────────────────────────────────────
        // Multi-world helpers (strongly typed)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Attempts to get a world by its unique <see cref="WorldId"/>.
        /// </summary>
        /// <param name="id">The world identifier to look up.</param>
        /// <param name="world">
        /// When this method returns, contains the resolved world if found,
        /// otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a world with the given <paramref name="id"/> was
        /// found; otherwise <c>false</c>.
        /// </returns>
        public static bool TryGetWorldById(WorldId id, out IWorld? world)
            => Current.TryGet(id, out world);

        /// <summary>
        /// Finds worlds by exact name (kernel-side matching).
        /// </summary>
        /// <param name="name">
        /// The world name to search for. If <c>null</c> or whitespace,
        /// an empty sequence is returned.
        /// </param>
        /// <returns>
        /// An enumerable of worlds whose names match <paramref name="name"/>
        /// exactly.
        /// </returns>
        public static IEnumerable<IWorld> FindByName(string name)
            => string.IsNullOrWhiteSpace(name) ? Enumerable.Empty<IWorld>() : Current.FindByName(name);

        /// <summary>
        /// Finds worlds whose names start with the specified prefix.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to match against world names. If <c>null</c>, an empty
        /// sequence is returned.
        /// </param>
        /// <returns>
        /// An enumerable of worlds whose names start with
        /// <paramref name="prefix"/> (kernel-defined semantics).
        /// </returns>
        public static IEnumerable<IWorld> FindByNamePrefix(string prefix)
            => prefix is null ? Enumerable.Empty<IWorld>() : Current.FindByNamePrefix(prefix);

        /// <summary>
        /// Attempts to get the first world that matches the specified name.
        /// </summary>
        /// <param name="name">The world name to search for.</param>
        /// <param name="world">
        /// When this method returns, contains the first matching world if
        /// found; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a world with the given name was found; otherwise
        /// <c>false</c>.
        /// </returns>
        public static bool TryGetWorldByName(string name, out IWorld world)
        {
            world = FindByName(name).FirstOrDefault();
            return world != null;
        }

        /// <summary>
        /// Sets the given world as the current world on the kernel.
        /// </summary>
        /// <param name="w">World instance to set as current.</param>
        /// <returns>
        /// <c>true</c> if the operation succeeded; <c>false</c> if
        /// <paramref name="w"/> is <c>null</c> or an exception was thrown.
        /// </returns>
        public static bool SetCurrentWorld(IWorld w)
        {
            if (w == null) return false;
            try
            {
                Current.SetCurrentWorld(w);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KernelLocator] Failed to set current world: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ensures a world exists by name; if missing, creates it and
        /// optionally sets it as current.
        /// </summary>
        /// <param name="name">The world name to find or create.</param>
        /// <param name="setAsCurrent">
        /// If <c>true</c>, the resolved or created world is set as the current
        /// world on the kernel.
        /// </param>
        /// <returns>
        /// The existing or newly created <see cref="IWorld"/> instance.
        /// </returns>
        public static IWorld EnsureWorld(string name, bool setAsCurrent = true)
        {
            if (TryGetWorldByName(name, out var w))
            {
                if (setAsCurrent) SetCurrentWorld(w);
                return w;
            }

            return Current.CreateWorld(
                cfg: null,
                name: name,
                tags: null,
                presetId: null,
                setAsCurrent: setAsCurrent);
        }

        // ──────────────────────────────────────────────
        // Tag helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Finds worlds that contain the specified tag.
        /// </summary>
        /// <param name="tag">
        /// Tag to search for. If <c>null</c> or whitespace, an empty sequence
        /// is returned.
        /// </param>
        /// <returns>
        /// An enumerable of worlds that contain <paramref name="tag"/> (case
        /// sensitivity is defined by the kernel implementation).
        /// </returns>
        public static IEnumerable<IWorld> FindByTag(string tag)
            => string.IsNullOrWhiteSpace(tag) ? Enumerable.Empty<IWorld>() : Current.FindByTag(tag);

        /// <summary>
        /// Finds worlds that match any of the provided tags (logical OR).
        /// </summary>
        /// <param name="tags">
        /// Collection of tags; if <c>null</c> or empty, an empty sequence is
        /// returned.
        /// </param>
        /// <returns>
        /// An enumerable of worlds that contain at least one of the provided
        /// <paramref name="tags"/>.
        /// </returns>
        public static IEnumerable<IWorld> FindByAnyTag(params string[] tags)
            => (tags == null || tags.Length == 0) ? Enumerable.Empty<IWorld>() : Current.FindByAnyTag(tags);

        /// <summary>
        /// Finds worlds that contain all of the provided tags (logical AND).
        /// </summary>
        /// <param name="tags">
        /// Collection of tags that a world must contain. If <c>null</c> or
        /// empty, an empty sequence is returned.
        /// </param>
        /// <returns>
        /// An enumerable of worlds that contain all specified
        /// <paramref name="tags"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation detail: this is computed as the intersection of
        /// world ID sets for each individual tag. The algorithm is optimized
        /// to minimize allocations by using HashSet intersection operations
        /// and maintaining a single dictionary for world lookups.
        /// </para>
        /// </remarks>
        public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return Enumerable.Empty<IWorld>();

            // AND = intersection of result sets. Use WorldId as key.
            // Start with the first tag's worlds, then intersect with subsequent tags.
            HashSet<WorldId>? acc = null;
            Dictionary<WorldId, IWorld>? lastMap = null;

            foreach (var t in tags)
            {
                var list = Current.FindByTag(t) ?? Enumerable.Empty<IWorld>();
                var map = list.Where(w => w != null).ToDictionary(w => w.Id, w => w);
                if (acc == null)
                {
                    // First tag: initialize accumulator with all world IDs from this tag
                    acc = new HashSet<WorldId>(map.Keys);
                    lastMap = map;
                }
                else
                {
                    // Subsequent tags: intersect with existing accumulator
                    acc.IntersectWith(map.Keys);
                    // Update lastMap to the most recent tag's map for final lookup
                    lastMap = map;
                }
            }

            if (acc == null || lastMap == null) return Enumerable.Empty<IWorld>();
            // Return only worlds that are in the intersection (acc) and exist in lastMap
            return acc.Where(lastMap.ContainsKey).Select(id => lastMap[id]);
        }

        /// <summary>
        /// Attempts to get the first world that contains the specified tag.
        /// </summary>
        /// <param name="tag">The tag used to filter worlds.</param>
        /// <param name="world">
        /// When this method returns, contains the first matching world if
        /// found; otherwise <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a matching world is found; otherwise <c>false</c>.
        /// </returns>
        public static bool TryGetWorldByTag(string tag, out IWorld world)
        {
            world = FindByTag(tag).FirstOrDefault();
            return world != null;
        }

        /// <summary>
        /// Ensures a world exists by name with tags; if missing, creates it
        /// using the provided tags.
        /// </summary>
        /// <param name="name">The world name to find or create.</param>
        /// <param name="tags">
        /// Tags to assign when creating a new world. Ignored if the world
        /// already exists.
        /// </param>
        /// <param name="setCurrent">
        /// If <c>true</c>, sets the resolved or created world as current.
        /// </param>
        /// <returns>
        /// The existing or newly created <see cref="IWorld"/> instance.
        /// </returns>
        public static IWorld EnsureWorld(string name, IEnumerable<string> tags, bool setCurrent = true)
        {
            if (TryGetWorldByName(name, out var w))
            {
                if (setCurrent) SetCurrentWorld(w);
                return w;
            }

            return Current.CreateWorld(
                cfg: null,
                name: name,
                tags: tags,
                presetId: null,
                setAsCurrent: setCurrent);
        }

        // ──────────────────────────────────────────────
        // 📚 Dictionary views
        // ──────────────────────────────────────────────

        /// <summary>
        /// Builds a snapshot dictionary of all worlds keyed by
        /// <see cref="WorldId"/>.
        /// </summary>
        /// <returns>
        /// A read-only dictionary mapping world IDs to their corresponding
        /// <see cref="IWorld"/> instances.
        /// </returns>
        public static IReadOnlyDictionary<WorldId, IWorld> AllWorldsById()
        {
            var dict = new Dictionary<WorldId, IWorld>();
            foreach (var w in AllWorlds)
            {
                if (w == null) continue;
                dict[w.Id] = w;
            }

            return dict;
        }

        /// <summary>
        /// Builds a snapshot dictionary of all worlds keyed by name.
        /// </summary>
        /// <param name="ignoreCase">
        /// If <c>true</c>, dictionary keys are compared using
        /// <see cref="StringComparer.OrdinalIgnoreCase"/>; otherwise
        /// <see cref="StringComparer.Ordinal"/>.
        /// </param>
        /// <returns>
        /// A read-only dictionary where each key is a world name and each
        /// value is a list of worlds that share that name.
        /// </returns>
        public static IReadOnlyDictionary<string, List<IWorld>> AllWorldsByName(bool ignoreCase = true)
        {
            var cmp = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var dict = new Dictionary<string, List<IWorld>>(cmp);
            foreach (var w in AllWorlds)
            {
                if (w == null) continue;
                var name = w.Name ?? string.Empty;
                if (!dict.TryGetValue(name, out var list)) dict[name] = list = new List<IWorld>();
                list.Add(w);
            }

            return dict;
        }

        /// <summary>
        /// Enumerates all worlds known to the kernel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementation detail: internally this uses
        /// <see cref="FindByNamePrefix(string)"/> with an empty prefix to
        /// retrieve all worlds.
        /// </para>
        /// </remarks>
        public static IEnumerable<IWorld> AllWorlds =>
            Current.FindByNamePrefix(string.Empty).Where(w => w != null);
    }
}
