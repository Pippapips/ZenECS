// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • External Commands
// File: ExternalCommand.cs
// Purpose: Represent high-level world mutations that can be queued externally
//          and applied later via a command buffer.
// Key concepts:
//   • Command type enum: create/destroy, add/replace/remove, singleton ops.
//   • Boxed payload: type + boxed value for component operations.
//   • Callback: invoked when spawn is applied (Entity + ICommandBuffer).
//   • Integration: processed by World.ExternalCommandFlushTo → ICommandBuffer.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core;

namespace ZenECS.Core
{
    /// <summary>
    /// Enumerates the kinds of external commands that can be queued and later
    /// applied to a world via a command buffer.
    /// </summary>
    public enum ExternalCommandKind
    {
        /// <summary>
        /// Create a new entity. The resulting entity handle is provided via the
        /// <see cref="ExternalCommand.CreatedCallback"/> when the command is applied.
        /// </summary>
        CreateEntity,

        /// <summary>
        /// Destroy an existing entity.
        /// </summary>
        DestroyEntity,

        /// <summary>
        /// Add a component to an entity.
        /// </summary>
        AddComponent,

        /// <summary>
        /// Replace an existing component on an entity, or add it depending on the
        /// command-buffer semantics.
        /// </summary>
        ReplaceComponent,

        /// <summary>
        /// Remove a component from an entity.
        /// </summary>
        RemoveComponent,

        /// <summary>
        /// Set or replace a world singleton component.
        /// </summary>
        SetSingleton,

        /// <summary>
        /// Remove a world singleton component.
        /// </summary>
        RemoveSingleton,
    }

    /// <summary>
    /// Represents a single external world mutation request that can be enqueued
    /// by external systems (for example, networking or UI layers) and later
    /// applied to the world via a command buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// External commands are consumed by the internal
    /// <c>World.ExternalCommandFlushTo</c> method, which translates them into
    /// command-buffer operations and applies them at a deterministic simulation
    /// barrier.
    /// </para>
    /// </remarks>
    public readonly struct ExternalCommand
    {
        /// <summary>
        /// Gets the type of operation requested.
        /// </summary>
        public ExternalCommandKind Kind { get; }

        /// <summary>
        /// Gets the entity that this command targets, if applicable.
        /// For singleton operations, this is typically <see cref="Entity.None"/>.
        /// </summary>
        public Entity Entity { get; }

        /// <summary>
        /// Gets the component type for component-related operations.
        /// For non-component operations, this property is <see langword="null"/>.
        /// </summary>
        public Type? ComponentType { get; }

        /// <summary>
        /// Gets the boxed component value for
        /// <see cref="ExternalCommandKind.AddComponent"/> and
        /// <see cref="ExternalCommandKind.ReplaceComponent"/> operations.
        /// </summary>
        public object? ComponentBoxed { get; }

        /// <summary>
        /// Gets an optional callback that is invoked when a
        /// <see cref="ExternalCommandKind.CreateEntity"/> command is applied and the
        /// new entity handle is known.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The callback receives both the newly created <see cref="Entity"/> and
        /// the <see cref="ICommandBuffer"/> used to record the operation, allowing
        /// additional commands to be chained onto the same buffer.
        /// </para>
        /// </remarks>
        public Action<Entity, ICommandBuffer>? CreatedCallback { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalCommand"/> struct.
        /// </summary>
        /// <param name="kind">Kind of external command.</param>
        /// <param name="entity">Target entity (if applicable).</param>
        /// <param name="componentType">Component type for component-related commands.</param>
        /// <param name="componentBoxed">Boxed component value (for add/replace).</param>
        /// <param name="createdCallback">
        /// Optional callback invoked when a created entity handle is known.
        /// </param>
        private ExternalCommand(
            ExternalCommandKind kind,
            Entity entity,
            Type? componentType,
            object? componentBoxed,
            Action<Entity, ICommandBuffer>? createdCallback)
        {
            Kind = kind;
            Entity = entity;
            ComponentType = componentType;
            ComponentBoxed = componentBoxed;
            CreatedCallback = createdCallback;
        }

        // ──────────────────────────────────────────────────────────────────
        // Factory helpers
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="ExternalCommandKind.CreateEntity"/> command.
        /// </summary>
        /// <param name="onCreated">
        /// Optional callback invoked when the entity has been created and its
        /// handle is known. The callback also receives the <see cref="ICommandBuffer"/>
        /// used to record the operation.
        /// </param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        public static ExternalCommand CreateEntity(Action<Entity, ICommandBuffer>? onCreated = null)
            => new(
                kind: ExternalCommandKind.CreateEntity,
                entity: Entity.None,
                componentType: null,
                componentBoxed: null,
                createdCallback: onCreated);

        /// <summary>
        /// Creates a <see cref="ExternalCommandKind.DestroyEntity"/> command.
        /// </summary>
        /// <param name="entity">Target entity to destroy.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        public static ExternalCommand DestroyEntity(Entity entity)
            => new(
                kind: ExternalCommandKind.DestroyEntity,
                entity: entity,
                componentType: null,
                componentBoxed: null,
                createdCallback: null);

        /// <summary>
        /// Creates an <see cref="ExternalCommandKind.AddComponent"/> command.
        /// </summary>
        /// <param name="entity">Entity that should receive the component.</param>
        /// <param name="componentType">Component type to add.</param>
        /// <param name="boxed">Boxed component value.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="componentType"/> is <see langword="null"/>.
        /// </exception>
        public static ExternalCommand AddComponent(Entity entity, Type componentType, object? boxed)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: ExternalCommandKind.AddComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: boxed,
                createdCallback: null);
        }

        /// <summary>
        /// Creates an <see cref="ExternalCommandKind.ReplaceComponent"/> command.
        /// </summary>
        /// <param name="entity">Entity whose component should be replaced.</param>
        /// <param name="componentType">Component type to replace.</param>
        /// <param name="boxed">New boxed component value.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="componentType"/> is <see langword="null"/>.
        /// </exception>
        public static ExternalCommand ReplaceComponent(Entity entity, Type componentType, object? boxed)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: ExternalCommandKind.ReplaceComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: boxed,
                createdCallback: null);
        }

        /// <summary>
        /// Creates an <see cref="ExternalCommandKind.RemoveComponent"/> command.
        /// </summary>
        /// <param name="entity">Entity from which the component should be removed.</param>
        /// <param name="componentType">Component type to remove.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="componentType"/> is <see langword="null"/>.
        /// </exception>
        public static ExternalCommand RemoveComponent(Entity entity, Type componentType)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: ExternalCommandKind.RemoveComponent,
                entity: entity,
                componentType: componentType,
                componentBoxed: null,
                createdCallback: null);
        }

        /// <summary>
        /// Creates an <see cref="ExternalCommandKind.SetSingleton"/> command.
        /// </summary>
        /// <param name="componentType">Singleton component type.</param>
        /// <param name="boxed">Boxed component value.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="componentType"/> is <see langword="null"/>.
        /// </exception>
        public static ExternalCommand SetSingleton(Type componentType, object? boxed)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: ExternalCommandKind.SetSingleton,
                entity: Entity.None,
                componentType: componentType,
                componentBoxed: boxed,
                createdCallback: null);
        }

        /// <summary>
        /// Creates an <see cref="ExternalCommandKind.RemoveSingleton"/> command.
        /// </summary>
        /// <param name="componentType">Singleton component type to remove.</param>
        /// <returns>A new <see cref="ExternalCommand"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="componentType"/> is <see langword="null"/>.
        /// </exception>
        public static ExternalCommand RemoveSingleton(Type componentType)
        {
            if (componentType is null)
                throw new ArgumentNullException(nameof(componentType));

            return new(
                kind: ExternalCommandKind.RemoveSingleton,
                entity: Entity.None,
                componentType: componentType,
                componentBoxed: null,
                createdCallback: null);
        }
    }
}
