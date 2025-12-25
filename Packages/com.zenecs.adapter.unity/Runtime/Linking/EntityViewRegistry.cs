// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Linking
// File: EntityViewRegistry.cs
// Purpose: Per-world registry that maps entities to their primary EntityLink
//          (view) MonoBehaviour for quick lookup and callbacks.
// Key concepts:
//   • ConditionalWeakTable keyed by IWorld to hold a Registry per world.
//   • Registry: dictionary<Entity, Bucket> where Bucket holds a single link.
//   • Safe lookup: only returns links that are still alive and valid.
//   • Utility helpers: TryGet, Callback, HasLink, UnregisterAll for cleanup.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    /// <summary>
    /// Static entry point for obtaining per-world view registries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="EntityViewRegistry"/> maintains a
    /// <see cref="ConditionalWeakTable{TKey,TValue}"/> that associates
    /// <see cref="IWorld"/> instances with their own <see cref="Registry"/>.
    /// The registry stores mappings from <see cref="Entity"/> to
    /// <see cref="EntityLink"/> so that view components can be resolved
    /// efficiently at runtime.
    /// </para>
    /// </remarks>
    public static class EntityViewRegistry
    {
        /// <summary>
        /// Per-world registry storage keyed by <see cref="IWorld"/>.
        /// </summary>
        private static readonly ConditionalWeakTable<IWorld, Registry> _byWorld = new();

        /// <summary>
        /// Gets the <see cref="Registry"/> instance associated with the
        /// specified world, creating it if needed.
        /// </summary>
        /// <param name="w">The world whose view registry is requested.</param>
        /// <returns>
        /// A non-null <see cref="Registry"/> associated with <paramref name="w"/>.
        /// </returns>
        public static Registry For(IWorld w) => _byWorld.GetValue(w, _ => new Registry());

        /// <summary>
        /// Cleans up the registry for a specific world that is being disposed.
        /// </summary>
        /// <param name="world">The world whose registry should be cleaned up.</param>
        /// <remarks>
        /// <para>
        /// This method should be called when a world is being disposed to ensure
        /// that all entity links registered for that world are properly cleaned up.
        /// It clears all entries in the registry's internal dictionary.
        /// </para>
        /// <para>
        /// The <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/> will automatically remove the
        /// registry entry when the world is garbage collected, but calling this
        /// method ensures immediate cleanup of the dictionary contents.
        /// </para>
        /// </remarks>
        public static void CleanupWorld(IWorld world)
        {
            if (world == null) return;
            
            // Try to get the registry and clear it
            if (_byWorld.TryGetValue(world, out var registry))
            {
                registry.UnregisterAll();
            }
        }

        /// <summary>
        /// Cleans up registries for worlds that are no longer referenced.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Note:</b> This method currently performs no operation because
        /// <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/> automatically removes entries when
        /// the key (world) is garbage collected. The method is provided for API
        /// consistency and potential future extensibility.
        /// </para>
        /// <para>
        /// <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/> does not expose a way to enumerate
        /// keys, so manual cleanup of dead entries is not possible. The garbage
        /// collector handles cleanup automatically when worlds are no longer
        /// referenced.
        /// </para>
        /// <para>
        /// For cleaning up a specific world's registry immediately, use
        /// <see cref="CleanupWorld(IWorld)"/> instead.
        /// </para>
        /// </remarks>
        public static void CleanupDeadWorlds()
        {
            // No-op: ConditionalWeakTable automatically removes entries when keys are GC'd.
            // This method is kept for API consistency and potential future extensibility.
        }

        /// <summary>
        /// Per-world mapping from <see cref="Entity"/> to <see cref="EntityLink"/>.
        /// </summary>
        public sealed class Registry
        {
            /// <summary>
            /// Internal map from entity to link.
            /// </summary>
            /// <remarks>
            /// Initial capacity is set to 128 to reduce reallocations for typical
            /// entity counts. This is a reasonable default for most game scenarios.
            /// </remarks>
            private readonly Dictionary<Entity, EntityLink?> _map = new(128);

            /// <summary>
            /// Registers a link for the given entity.
            /// </summary>
            /// <param name="e">The entity to associate with the link.</param>
            /// <param name="link">The <see cref="EntityLink"/> to register.</param>
            /// <remarks>
            /// <para>
            /// Any previously registered link for <paramref name="e"/> in this
            /// registry will be replaced.
            /// </para>
            /// </remarks>
            public void Register(Entity e, EntityLink link)
            {
                _map[e] = link;
            }

            /// <summary>
            /// Unregisters a specific link for the given entity.
            /// </summary>
            /// <param name="e">The entity whose link should be removed.</param>
            /// <param name="link">
            /// The <see cref="EntityLink"/> instance to unregister. If this
            /// does not match the stored link, no action is taken.
            /// </param>
            /// <remarks>
            /// <para>
            /// If the stored link equals <paramref name="link"/>, the entity
            /// is removed from the internal map.
            /// </para>
            /// </remarks>
            public void Unregister(Entity e, EntityLink link)
            {
                if (_map.TryGetValue(e, out var existing) && existing == link)
                    _map.Remove(e);
            }

            /// <summary>
            /// Unregisters all links in this registry.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This method clears the internal dictionary entirely. It is
            /// typically used for world-level cleanup when the world is being
            /// disposed or reset.
            /// </para>
            /// </remarks>
            public void UnregisterAll()
            {
                _map.Clear();
            }

            /// <summary>
            /// Attempts to retrieve the link registered for an entity.
            /// </summary>
            /// <param name="e">The entity whose link is requested.</param>
            /// <param name="link">
            /// When this method returns, contains the registered
            /// <see cref="EntityLink"/> if found and alive; otherwise <c>null</c>.
            /// </param>
            /// <returns>
            /// <c>true</c> if an alive link was found; otherwise <c>false</c>.
            /// </returns>
            /// <remarks>
            /// <para>
            /// The method checks that the link exists, is non-null, and reports
            /// <see cref="EntityLink.IsAlive"/> as <c>true</c>.
            /// </para>
            /// </remarks>
            public bool TryGet(Entity e, out EntityLink? link)
            {
                link = null;
                if (_map.TryGetValue(e, out var stored) && stored != null && stored.IsAlive)
                {
                    link = stored;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Invokes a callback for the link associated with the specified
            /// entity, if it exists and is alive.
            /// </summary>
            /// <param name="e">The entity whose link should be used.</param>
            /// <param name="act">
            /// The action to invoke with the <see cref="EntityLink"/> instance.
            /// </param>
            /// <remarks>
            /// <para>
            /// If no link is found or the link is null or not alive, the
            /// callback is not invoked.
            /// </para>
            /// </remarks>
            public void Callback(Entity e, System.Action<EntityLink> act)
            {
                if (_map.TryGetValue(e, out var link) && link != null && link.IsAlive)
                    act(link);
            }

            /// <summary>
            /// Determines whether there is an alive link registered for the
            /// specified entity.
            /// </summary>
            /// <param name="e">The entity to check.</param>
            /// <returns>
            /// <c>true</c> if an alive <see cref="EntityLink"/> is registered
            /// for <paramref name="e"/>; otherwise <c>false</c>.
            /// </returns>
            public bool HasLink(Entity e) =>
                _map.TryGetValue(e, out var link) && link != null && link.IsAlive;

            /// <summary>
            /// Enumerates all entity-link pairs registered in this registry.
            /// </summary>
            /// <param name="aliveOnly">
            /// If <c>true</c>, only includes links where <see cref="EntityLink.IsAlive"/>
            /// is <c>true</c>. If <c>false</c>, includes all non-null links regardless
            /// of their alive status.
            /// </param>
            /// <returns>
            /// An enumerable sequence of tuples containing the <see cref="Entity"/>
            /// and its associated <see cref="EntityLink"/>. The filtering behavior
            /// depends on <paramref name="aliveOnly"/>.
            /// </returns>
            /// <remarks>
            /// <para>
            /// This method iterates through all entries in the internal dictionary
            /// and filters out entries based on <paramref name="aliveOnly"/>:
            /// </para>
            /// <list type="bullet">
            /// <item><description>
            /// When <paramref name="aliveOnly"/> is <c>true</c>, only links that are
            /// non-null and report <see cref="EntityLink.IsAlive"/> as <c>true</c>
            /// are included.
            /// </description></item>
            /// <item><description>
            /// When <paramref name="aliveOnly"/> is <c>false</c>, all non-null links
            /// are included regardless of their alive status.
            /// </description></item>
            /// </list>
            /// <para>
            /// The enumeration is performed lazily, so modifications to the registry
            /// during enumeration may affect the results. It is recommended to avoid
            /// modifying the registry while iterating over the results.
            /// </para>
            /// <para>
            /// Typical usage:
            /// </para>
            /// <code>
            /// var registry = EntityViewRegistry.For(world);
            /// foreach (var (entity, link) in registry.EnumerateViews(aliveOnly: true))
            /// {
            ///     Debug.Log($"Entity {entity.Id} → {link.gameObject.name}");
            /// }
            /// </code>
            /// </remarks>
            public IEnumerable<(Entity Entity, EntityLink Link)> EnumerateViews(bool aliveOnly = false)
            {
                foreach (var kvp in _map)
                {
                    if (kvp.Value != null && (!aliveOnly || kvp.Value.IsAlive))
                    {
                        yield return (kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }
}
