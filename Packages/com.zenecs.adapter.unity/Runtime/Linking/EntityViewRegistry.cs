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
        /// The <see cref="ConditionalWeakTable"/> will automatically remove the
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
        /// This method is intended to be called periodically or when worlds are
        /// disposed. Since <see cref="ConditionalWeakTable"/> automatically removes
        /// entries when the key (world) is garbage collected, this method primarily
        /// serves to clean up any remaining references in the registry dictionaries.
        /// </para>
        /// <para>
        /// In practice, the <see cref="ConditionalWeakTable"/> handles most cleanup
        /// automatically, but this method can be useful for explicit cleanup scenarios.
        /// </para>
        /// <para>
        /// For cleaning up a specific world, use <see cref="CleanupWorld(IWorld)"/> instead.
        /// </para>
        /// </remarks>
        public static void CleanupDeadWorlds()
        {
            // ConditionalWeakTable automatically removes entries when the key is GC'd,
            // so we don't need to manually iterate. However, we can force a collection
            // if needed by accessing the table (though this is generally not necessary).
            // This method is provided for explicit cleanup scenarios where immediate
            // cleanup is desired, though in practice the GC will handle it automatically.
            
            // Note: ConditionalWeakTable doesn't expose a way to enumerate keys,
            // so we can't directly clean up dead entries. The GC will handle it.
            // This method is kept for API consistency and future extensibility.
        }

        /// <summary>
        /// Per-world mapping from <see cref="Entity"/> to <see cref="EntityLink"/>.
        /// </summary>
        public sealed class Registry
        {
            /// <summary>
            /// Internal map from entity to link.
            /// </summary>
            private readonly Dictionary<Entity, EntityLink?> _map = new();

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
        }
    }
}
