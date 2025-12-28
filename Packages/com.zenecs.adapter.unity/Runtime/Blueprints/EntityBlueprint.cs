// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Blueprints
// File: EntityBlueprint.cs
// Purpose: ScriptableObject blueprint that stores a component snapshot,
//          context assets, and binders, and can spawn a configured entity
//          into a ZenECS world.
// Key concepts:
//   • Component snapshot: EntityBlueprintData as a serialized component set.
//   • Context assets: shared/per-entity IContext factories and markers.
//   • Binders: ScriptableObject binder assets that create binders per entity.
//   • External command: uses ExternalCommand.CreateEntity for safe creation.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Binding.Binders.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// Unity-side blueprint for spawning preconfigured entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="EntityBlueprint"/> captures a snapshot of ECS component
    /// data (<see cref="EntityBlueprintData"/>), plus a set of context assets
    /// and binders that should be attached when the entity is created.
    /// </para>
    /// <para>
    /// Typical usage:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Author the component snapshot and contexts in the inspector.
    /// </description></item>
    /// <item><description>
    /// At runtime, call <see cref="Spawn"/> with a target world to enqueue
    /// creation of the entity and application of the snapshot.
    /// </description></item>
        /// <item><description>
        /// Optionally use a shared context resolver to resolve
        /// shared contexts when spawning.
        /// </description></item>
    /// </list>
    /// </remarks>
    [CreateAssetMenu(menuName = "ZenECS/Entity Blueprint", fileName = "EntityBlueprint")]
    public sealed class EntityBlueprint : ScriptableObject
    {
        [Header("Components (snapshot)")]
        [SerializeField] private EntityBlueprintData _data = new();

        /// <summary>
        /// Gets the serialized component snapshot associated with this blueprint.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="EntityBlueprintData"/> contains a list of
        /// component entries encoded as JSON strings. It is consumed by
        /// <see cref="EntityBlueprintData.ApplyTo"/> during entity creation.
        /// </remarks>
        public EntityBlueprintData Data => _data;

        [Header("Contexts (ScriptableObject assets)")]
        [SerializeField] private List<ContextAsset> _contextAssets = new();

        [Header("Binders (ScriptableObject assets)")]
        [SerializeField] private List<BinderAsset> _binderAssets = new();

        /// <summary>
        /// Spawns an entity and applies the blueprint's component snapshot,
        /// contexts, and binders.
        /// </summary>
        /// <param name="world">
        /// Target <see cref="IWorld"/> where the new entity will be created.
        /// </param>
        /// <param name="sharedContextResolver">
        /// Optional shared-context resolver used to resolve
        /// <see cref="SharedContextAsset"/> markers into concrete
        /// <see cref="IContext"/> instances. When <c>null</c>, shared context
        /// markers are ignored.
        /// </param>
        /// <param name="onCreated">
        /// Optional callback invoked after the entity has been created and all
        /// components, contexts, and binders have been applied.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method enqueues an <see cref="ExternalCommand.CreateEntity"/>
        /// into the world rather than creating the entity immediately. The
        /// actual creation and attachment happen when the world's external
        /// command queue is processed.
        /// </para>
        /// <para>
        /// The spawn pipeline inside the command:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// Applies the component snapshot via <see cref="EntityBlueprintData.ApplyTo"/>.
        /// </description></item>
        /// <item><description>
        /// Registers contexts from <see cref="_contextAssets"/>:
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="SharedContextAsset"/>: resolved via
        /// a shared context resolver and registered using
        /// <see cref="IWorldContextApi.RegisterContext(Entity, IContext)"/>.
        /// </description></item>
        /// <item><description>
        /// <see cref="PerEntityContextAsset"/>: creates a fresh context and
        /// registers it for the entity.
        /// </description></item>
        /// </list>
        /// </description></item>
        /// <item><description>
        /// Creates binders from <see cref="_binderAssets"/>: each
        /// <see cref="BinderAsset"/> creates a fresh binder and attaches it
        /// to the entity.
        /// </description></item>
        /// <item><description>
        /// Invokes <paramref name="onCreated"/> if provided.
        /// </description></item>
        /// </list>
        /// </remarks>
        public void Spawn(
            IWorld world,
            ISharedContextResolver? sharedContextResolver,
            Action<Entity>? onCreated = null)
        {
            world.ExternalCommandEnqueue(ExternalCommand.CreateEntity((e, cmd) =>
            {
                ApplyComponents(world, e, cmd);
                ApplyContexts(world, e, sharedContextResolver);
                ApplyBinders(world, e);
                onCreated?.Invoke(e);
            }));
        }

        /// <summary>
        /// Applies component snapshot to the entity.
        /// </summary>
        private void ApplyComponents(IWorld world, Entity e, ICommandBuffer cmd)
        {
            _data?.ApplyTo(world, e, cmd);
        }

        /// <summary>
        /// Applies context assets to the entity.
        /// </summary>
        private void ApplyContexts(IWorld world, Entity e, ISharedContextResolver? sharedContextResolver)
        {
            // Contexts first (so binders see them in OnAttach).
            for (int i = 0; i < _contextAssets.Count; i++)
            {
                var asset = _contextAssets[i];
                switch (asset)
                {
                    case SharedContextAsset markerAsset:
                    {
                        if (sharedContextResolver != null)
                        {
                            var ctx = sharedContextResolver.Resolve(markerAsset);
                            if (ctx != null)
                                world.RegisterContext(e, ctx);
                        }
                        break;
                    }
                    case PerEntityContextAsset perEntityAsset:
                    {
                        var ctx = perEntityAsset.Create();
                        world.RegisterContext(e, ctx);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Applies binder assets to the entity.
        /// </summary>
        private void ApplyBinders(IWorld world, Entity e)
        {
            for (int i = 0; i < _binderAssets.Count; i++)
            {
                var asset = _binderAssets[i];
                if (asset != null)
                {
                    var binder = asset.Create();
                    if (binder != null)
                        world.AttachBinder(e, binder);
                }
            }
        }

    }
}
