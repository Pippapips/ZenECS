// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Blueprints
// File: EntityBlueprint.cs
// Purpose: ScriptableObject blueprint that stores a component snapshot,
//          context assets, and binders, and can spawn a configured entity
//          into a ZenECS world.
// Key concepts:
//   • Component snapshot: EntityBlueprintData as a serialized component set.
//   • Context assets: shared/per-entity IContext factories and markers.
//   • Binders: managed-reference binders shallow-cloned per entity.
//   • External command: uses ExternalCommand.CreateEntity for safe creation.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
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
    /// Optionally use <paramref name="sharedContextResolver"/> to resolve
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

        [Header("Binders (managed reference)")]
        [SerializeReference] private List<IBinder> _binders = new();

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
        /// <paramref name="sharedContextResolver"/> and registered using
        /// <see cref="IWorld.RegisterContext(Entity, IContext)"/>.
        /// </description></item>
        /// <item><description>
        /// <see cref="PerEntityContextAsset"/>: creates a fresh context and
        /// registers it for the entity.
        /// </description></item>
        /// </list>
        /// </description></item>
        /// <item><description>
        /// Shallow-clones each binder, sets its apply/attach order and attaches
        /// it to the entity using <see cref="IWorld.AttachBinder"/>.
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
                _data?.ApplyTo(world, e, cmd);

                // 1) Contexts first (so binders see them in OnAttach).
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

                foreach (var b in _binders)
                {
                    if (b == null) continue;
                    var inst = (IBinder)ShallowCopy(b, b.GetType());
                    inst.SetApplyOrderAndAttachOrder(inst.ApplyOrder, b.AttachOrder);
                    world.AttachBinder(e, inst);
                }

                onCreated?.Invoke(e);
            }));
        }

        /// <summary>
        /// Creates a shallow copy of a binder (or other reference type).
        /// </summary>
        /// <param name="source">
        /// Source object instance to copy. Value types and Unity objects are
        /// returned as-is.
        /// </param>
        /// <param name="t">
        /// Runtime type of the source object.
        /// </param>
        /// <returns>
        /// A shallow-cloned instance of <paramref name="t"/> with all instance
        /// fields copied from <paramref name="source"/>; or the original value
        /// when the type is a value type or <see cref="UnityEngine.Object"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="t"/> does not expose a public, parameterless
        /// constructor.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method uses reflection to copy all non-static instance fields.
        /// It does not perform deep cloning of referenced objects.
        /// </para>
        /// </remarks>
        private static object ShallowCopy(object? source, Type t)
        {
            if (source == null) return null!;
            if (t.IsValueType) return source;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return source;

            object target;
            try { target = Activator.CreateInstance(t)!; }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Type '{t.FullName}' requires a public parameterless ctor.", ex);
            }

            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in t.GetFields(BF))
            {
                if (f.IsStatic) continue;
                var val = f.GetValue(source);
                f.SetValue(target, val);
            }
            return target;
        }
    }
}
