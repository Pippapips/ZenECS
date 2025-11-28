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
        /// Per-world mapping from <see cref="Entity"/> to <see cref="EntityLink"/>.
        /// </summary>
        public sealed class Registry
        {
            /// <summary>
            /// Small container that holds the registered <see cref="EntityLink"/>.
            /// </summary>
            /// <remarks>
            /// <para>
            /// A bucket is used so that the link reference can be cleared
            /// without removing the dictionary entry immediately, allowing for
            /// simple cleanup semantics.
            /// </para>
            /// </remarks>
            private sealed class Bucket
            {
                /// <summary>
                /// The link associated with the entity, or <c>null</c> if none.
                /// </summary>
                public EntityLink? Link;
            }

            /// <summary>
            /// Internal map from entity to bucket.
            /// </summary>
            private readonly Dictionary<Entity, Bucket> _map = new();

            /// <summary>
            /// Gets the bucket associated with the specified entity, creating
            /// a new one if necessary.
            /// </summary>
            /// <param name="e">The entity key.</param>
            /// <returns>
            /// A non-null <see cref="Bucket"/> associated with <paramref name="e"/>.
            /// </returns>
            private Bucket GetOrCreate(Entity e) =>
                _map.TryGetValue(e, out var b) ? b : (_map[e] = new Bucket());

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
                var b = GetOrCreate(e);
                b.Link = link;
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
            /// If the stored link equals <paramref name="link"/>, the bucket's
            /// link is cleared. If the cleared bucket no longer has a valid
            /// link, the entity is removed from the internal map.
            /// </para>
            /// </remarks>
            public void Unregister(Entity e, EntityLink link)
            {
                if (!_map.TryGetValue(e, out var b)) return;
                if (b.Link == link) b.Link = null;
                if (!b.Link) _map.Remove(e);
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
            /// The method checks both that the bucket exists and that the
            /// stored link reference is non-null and reports
            /// <see cref="EntityLink.IsAlive"/> as <c>true</c>.
            /// </para>
            /// </remarks>
            public bool TryGet(Entity e, out EntityLink? link)
            {
                link = null;
                if (_map.TryGetValue(e, out var b) && (b.Link && b.Link.IsAlive))
                {
                    link = b.Link;
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
            /// If no bucket is found or the link is null or not alive, the
            /// callback is not invoked.
            /// </para>
            /// </remarks>
            public void Callback(Entity e, System.Action<EntityLink> act)
            {
                if (!_map.TryGetValue(e, out var b)) return;
                if (b.Link && b.Link.IsAlive) act(b.Link);
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
                _map.TryGetValue(e, out var b) && (b.Link && b.Link.IsAlive);
        }
    }
}
