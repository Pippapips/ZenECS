// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Editor Integration
// File: EditorCommand.cs
// Purpose: Describe editor-driven structural mutations in a world-agnostic way.
// Notes:
//   • Used by tools like EcsExplorer to request changes safely
//   • Commands are later flushed via ExternalEditorCommandQueue at a barrier
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

namespace ZenECS.EditorCommands
{
    /// <summary>
    /// Operation kind that an editor can request against a world.
    /// </summary>
    public enum EditorCommandKind
    {
        SpawnEntity,
        DespawnEntity,

        AddComponent,
        ReplaceComponent,
        RemoveComponent,
        RemoveSingleton,
    }

    /// <summary>
    /// A single editor-originated structural/value mutation request.
    /// <para>
    /// This does not directly touch the world. Instead, instances are recorded
    /// in an <see cref="ExternalEditorCommandQueue"/> and later translated into
    /// <see cref="ICommandBuffer"/> calls at a safe simulation barrier.
    /// </para>
    /// </summary>
    public readonly struct EditorCommand
    {
        /// <summary>
        /// Type of operation requested.
        /// </summary>
        public EditorCommandKind Kind { get; }

        /// <summary>
        /// Target entity, if applicable.
        /// <para>
        /// For <see cref="EditorCommandKind.SpawnEntity"/> this field is not used.
        /// </para>
        /// </summary>
        public Entity Entity { get; }

        /// <summary>
        /// Component type for component-related operations.
        /// </summary>
        public Type? ComponentType { get; }

        /// <summary>
        /// Boxed component value for Add/Replace operations.
        /// </summary>
        public object? ComponentBoxed { get; }

        /// <summary>
        /// Optional callback invoked when a spawn command is applied and the
        /// new entity handle is known.
        /// </summary>
        public Action<Entity>? SpawnCallback { get; }

        private EditorCommand(
            EditorCommandKind kind,
            Entity entity,
            Type? componentType,
            object? componentBoxed,
            Action<Entity>? spawnCallback)
        {
            Kind = kind;
            Entity = entity;
            ComponentType = componentType;
            ComponentBoxed = componentBoxed;
            SpawnCallback = spawnCallback;
        }

        // ──────────────────────────────────────────────────────────────────
        // Factory helpers
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Request spawning a new entity.
        /// </summary>
        /// <param name="onSpawn">
        /// Optional callback invoked with the created entity when the command
        /// is flushed to a world command buffer.
        /// </param>
        public static EditorCommand SpawnEntity(Action<Entity>? onSpawn = null)
            => new(
                kind: EditorCommandKind.SpawnEntity,
                entity: Entity.None,
                componentType: null,
                componentBoxed: null,
                spawnCallback: onSpawn);

        /// <summary>
        /// Request despawning <paramref name="entity"/>.
        /// </summary>
        public static EditorCommand DespawnEntity(Entity entity)
            => new(
                kind: EditorCommandKind.DespawnEntity,
                entity: entity,
                componentType: null,
                componentBoxed: null,
                spawnCallback: null);

        /// <summary>
        /// Request adding a component to an entity.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentType">Component type (must be a struct type).</param>
        /// <param name="boxed">Boxed component value.</param>
        public static EditorCommand AddComponent(Entity entity, Type componentType, object? boxed)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: EditorCommandKind.AddComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: boxed,
                spawnCallback: null);
        }

        /// <summary>
        /// Request replacing an existing component value on an entity.
        /// If the component is missing when applied, world policy may create it.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentType">Component type (must be a struct type).</param>
        /// <param name="boxed">Boxed component value.</param>
        public static EditorCommand ReplaceComponent(Entity entity, Type componentType, object? boxed)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: EditorCommandKind.ReplaceComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: boxed,
                spawnCallback: null);
        }

        /// <summary>
        /// Request removing a component from an entity.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentType">Component type.</param>
        public static EditorCommand RemoveComponent(Entity entity, Type componentType)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: EditorCommandKind.RemoveComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: null,
                spawnCallback: null);
        }
        
        /// <summary>
        /// Request removing a component from an entity.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentType">Component type.</param>
        public static EditorCommand RemoveSingleton(Type componentType)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: EditorCommandKind.RemoveSingleton,
                entity: Entity.None,
                componentType: componentType,
                componentBoxed: null,
                spawnCallback: null);
        }
    }
}
