// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Linking
// File: EntityLink.cs
// Purpose: MonoBehaviour bridge that links a Unity GameObject to a ZenECS
//          entity and registers it in a per-world view registry.
// Key concepts:
//   • Lightweight link: stores (IWorld, Entity) on a GameObject.
//   • View registry: auto-registers/unregisters with EntityViewRegistry.
//   • Editor-only helpers: extension methods to create/destroy links safely
//     inside the Unity Editor.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    /// <summary>
    /// MonoBehaviour that links a Unity <see cref="GameObject"/> to a ZenECS entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="EntityLink"/> stores a reference to an <see cref="IWorld"/> and
    /// the corresponding <see cref="Entity"/>. When attached, it automatically
    /// registers itself in <see cref="EntityViewRegistry"/> for that world, and
    /// unregisters when detached or destroyed.
    /// </para>
    /// <para>
    /// This component is intended to be unique per GameObject, enforced by
    /// <see cref="DisallowMultipleComponentAttribute"/>.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EntityLink : MonoBehaviour
    {
        /// <summary>
        /// Gets the world that owns the linked entity.
        /// </summary>
        public IWorld? World { get; private set; }

        /// <summary>
        /// Gets the ECS entity linked to this GameObject.
        /// </summary>
        public Entity Entity { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the link currently points to a
        /// valid, alive entity in a non-null world.
        /// </summary>
        public bool IsAlive => World != null && Entity.Id >= 0 && World.IsAlive(Entity);

        /// <summary>
        /// Attaches this link to a given world and entity.
        /// </summary>
        /// <param name="w">The world that owns the entity.</param>
        /// <param name="e">The entity to link to this GameObject.</param>
        /// <remarks>
        /// <para>
        /// If the link was previously attached to a different world or entity,
        /// it will be unregistered from the old world's view registry before
        /// being registered with the new one.
        /// </para>
        /// </remarks>
        public void Attach(IWorld w, in Entity e)
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this);

            World = w;
            Entity = e;

            if (World != null)
                EntityViewRegistry.For(World).Register(Entity, this);
        }

        /// <summary>
        /// Detaches this GameObject from its current world/entity link.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the link is registered in a world view registry, it will be
        /// unregistered. After detaching, <see cref="World"/> will be
        /// <c>null</c> and <see cref="Entity"/> will be reset to its default.
        /// </para>
        /// </remarks>
        public void Detach()
        {
            if (World != null)
                EntityViewRegistry.For(World).Unregister(Entity, this);

            World = null;
            Entity = default;
        }

        /// <summary>
        /// Unity lifecycle callback invoked when this component or its
        /// GameObject is about to be destroyed.
        /// </summary>
        /// <remarks>
        /// Automatically calls <see cref="Detach"/> to unregister the link
        /// from the associated world registry.
        /// </remarks>
        private void OnDestroy()
        {
            Detach();
        }
    }

    /// <summary>
    /// Extension methods that provide convenient helpers for working with
    /// <see cref="EntityLink"/> in the Unity Editor.
    /// </summary>
    public static class EntityLinkExtensions
    {
        /// <summary>
        /// Ensures a link exists on the GameObject and attaches it to the
        /// specified world and entity.
        /// </summary>
        /// <param name="go">The target GameObject to attach the link to.</param>
        /// <param name="w">The world that owns the entity.</param>
        /// <param name="e">The entity to link.</param>
        /// <returns>
        /// The created or existing <see cref="EntityLink"/> instance in the
        /// Unity Editor; otherwise <c>null</c> in non-editor builds.
        /// </returns>
        /// <remarks>
        /// <para>
        /// In the Unity Editor, this method either reuses an existing
        /// <see cref="EntityLink"/> or adds a new one to <paramref name="go"/>,
        /// then calls <see cref="EntityLink.Attach"/>.
        /// </para>
        /// <para>
        /// In non-editor builds, this method is compiled out and always
        /// returns <c>null</c>.
        /// </para>
        /// </remarks>
        public static EntityLink? CreateEntityLink(this GameObject go, IWorld w, in Entity e)
        {
#if UNITY_EDITOR
            if (!go) return null;
            var link = go.GetComponent<EntityLink>() ?? go.AddComponent<EntityLink>();
            link.Attach(w, e);
            return link;
#else
            return null;
#endif
        }

        /// <summary>
        /// Destroys the <see cref="EntityLink"/> attached to the GameObject,
        /// if any, and detaches it from the world registry.
        /// </summary>
        /// <param name="go">The GameObject whose link should be destroyed.</param>
        /// <remarks>
        /// <para>
        /// In the Unity Editor, this method looks up any existing
        /// <see cref="EntityLink"/> on <paramref name="go"/>, calls
        /// <see cref="EntityLink.Detach"/>, and leaves destruction to Unity's
        /// usual component lifecycle.
        /// </para>
        /// <para>
        /// In non-editor builds, this method is compiled out and does nothing.
        /// </para>
        /// </remarks>
        public static void DestroyEntityLink(this GameObject go)
        {
#if UNITY_EDITOR
            if (!go) return;
            var link = go.GetComponent<EntityLink>();
            if (link == null) return;
            link.Detach();
#endif
        }
    }
}
