// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World Command Buffer
// File: ICommandBuffer.cs
// Purpose: Record structural and value mutations for deferred application.
// Key concepts:
//   • Single-writer per world: buffers are bound to one IWorld instance.
//   • Deferred application: all mutations are applied at a deterministic barrier.
//   • Safety: systems never call world mutation APIs directly, only via buffers.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Records structural and value mutations to be applied later at a world barrier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A command buffer is always bound to a single <see cref="IWorld"/> and never
    /// applies mutations immediately; all recorded operations are applied by the
    /// runner/worker at a deterministic tick boundary.
    /// </para>
    /// <para>
    /// Systems must not call world mutation APIs directly. Instead, they obtain
    /// a buffer via <see cref="IWorldCommandBufferApi.BeginWrite"/> and record
    /// all entity/component/singleton changes through this interface.
    /// </para>
    /// <para>
    /// Implementations must treat the buffer as single-use: once
    /// <see cref="EndWrite"/> is called (or the buffer is disposed), no further
    /// commands may be recorded.
    /// </para>
    /// </remarks>
    public interface ICommandBuffer : IDisposable
    {
        /// <summary>
        /// Marks the end of the write phase for this buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After calling <see cref="EndWrite"/>, no further commands may be recorded.
        /// The buffer is queued for application at the next world barrier.
        /// </para>
        /// <para>
        /// <see cref="IDisposable.Dispose"/> should typically call <see cref="EndWrite"/>
        /// if it has not been called already.
        /// </para>
        /// </remarks>
        void EndWrite();

        // ──────────────────────────────────────────────────────────────────
        // Entity lifecycle
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Records the creation of a new entity in the bound world.
        /// </summary>
        /// <returns>
        /// An <see cref="Entity"/> handle that can be used to record further
        /// operations (components, tags, etc.) in the same buffer.
        /// </returns>
        /// <remarks>
        /// The entity becomes alive only when the buffer is applied at a barrier.
        /// </remarks>
        Entity CreateEntity();

        /// <summary>
        /// Records the destruction of an entity.
        /// </summary>
        /// <param name="e">The entity to destroy.</param>
        /// <remarks>
        /// The entity will be removed and all components destroyed when the
        /// buffer is applied at a barrier. If the entity is already dead at
        /// application time, this becomes a no-op.
        /// </remarks>
        void DestroyEntity(Entity e);

        // ──────────────────────────────────────────────────────────────────
        // Component operations
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Records adding a component to an entity.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">Component value to add.</param>
        /// <remarks>
        /// If the entity is not alive by the time the buffer is applied,
        /// this operation becomes a no-op.
        /// </remarks>
        void AddComponent<T>(Entity e, in T value) where T : struct;

        /// <summary>
        /// Records adding a component to an entity using a boxed value.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value; its runtime type determines the component type.
        /// </param>
        /// <remarks>
        /// If <paramref name="boxed"/> is <see langword="null"/>, the operation is ignored.
        /// Implementations are expected to validate and unbox the value when
        /// applying the buffer.
        /// </remarks>
        void AddComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Records replacing (or setting) a component value on an entity.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">New component value.</param>
        /// <remarks>
        /// If the component is missing when the buffer is applied, it may be
        /// created depending on world policy.
        /// </remarks>
        void ReplaceComponent<T>(Entity e, in T value) where T : struct;

        /// <summary>
        /// Records replacing (or setting) a component value on an entity using a boxed value.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value; its runtime type determines the component type.
        /// </param>
        /// <remarks>
        /// If the component is missing when the buffer is applied, it may be
        /// created depending on world policy. If <paramref name="boxed"/> is
        /// <see langword="null"/>, the operation is ignored.
        /// </remarks>
        void ReplaceComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Records removing a component from an entity.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <remarks>
        /// If the component is absent when the buffer is applied, this becomes a no-op.
        /// </remarks>
        void RemoveComponent<T>(Entity e) where T : struct;

        /// <summary>
        /// Records removing a component from an entity by runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="type">Component type to remove.</param>
        /// <remarks>
        /// If the component is absent when the buffer is applied, this becomes a no-op.
        /// </remarks>
        void RemoveComponentTyped(Entity e, Type type);

        // ──────────────────────────────────────────────────────────────────
        // Singleton operations
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Records setting a singleton component.
        /// </summary>
        /// <typeparam name="T">Singleton component type.</typeparam>
        /// <param name="value">New singleton value.</param>
        /// <returns>
        /// An <see cref="Entity"/> handle for the singleton entity. If a singleton
        /// of type <typeparamref name="T"/> already exists, returns the existing
        /// entity. Otherwise, returns a newly created entity.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a singleton of type <typeparamref name="T"/> already exists,
        /// its value is replaced. Otherwise, a dedicated singleton entity is
        /// created to hold the new value.
        /// </para>
        /// <para>
        /// The entity becomes alive only when the buffer is applied at a barrier.
        /// </para>
        /// </remarks>
        Entity SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent;

        /// <summary>
        /// Records setting a singleton component using a boxed value.
        /// </summary>
        /// <param name="type">Singleton component type.</param>
        /// <param name="boxed">
        /// Boxed singleton value; must be assignable to <paramref name="type"/>.
        /// </param>
        /// <returns>
        /// An <see cref="Entity"/> handle for the singleton entity. If a singleton
        /// of the specified type already exists, returns the existing entity.
        /// Otherwise, returns a newly created entity. If <paramref name="boxed"/>
        /// is <see langword="null"/>, returns an invalid entity.
        /// </returns>
        /// <remarks>
        /// If a singleton of the specified type already exists, its value is replaced.
        /// Otherwise, a dedicated singleton entity is created. If
        /// <paramref name="boxed"/> is <see langword="null"/>, the operation is ignored.
        /// </remarks>
        Entity SetSingletonTyped(Type type, object? boxed);

        /// <summary>
        /// Records removal of a singleton component and its dedicated entity.
        /// </summary>
        /// <typeparam name="T">Singleton component type.</typeparam>
        /// <remarks>
        /// If the singleton does not exist, this becomes a no-op.
        /// </remarks>
        void RemoveSingleton<T>() where T : struct, IWorldSingletonComponent;

        /// <summary>
        /// Records removal of a singleton component by runtime type.
        /// </summary>
        /// <param name="type">Singleton component type.</param>
        /// <remarks>
        /// If the singleton does not exist, this becomes a no-op.
        /// </remarks>
        void RemoveSingletonTyped(Type type);

        /// <summary>
        /// Records a command to despawn <b>all alive entities</b> in the world.
        /// </summary>
        /// <remarks>
        /// Applied at the barrier, this will behave as if every entity had been
        /// despawned individually (events, router, binders included).
        /// </remarks>
        void DestroyAllEntities();
    }
}