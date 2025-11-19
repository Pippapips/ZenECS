#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Records structural and value mutations to be applied later at a world barrier.
    /// <para>
    /// A command buffer is always bound to a single <see cref="IWorld"/> and
    /// never applies mutations immediately; all recorded operations are applied
    /// by the runner/worker at a deterministic tick boundary.
    /// </para>
    /// <para>
    /// Systems must never call world mutation APIs directly. Instead, they obtain
    /// a buffer via <see cref="IWorldCommandBufferApi.BeginWrite"/> and record
    /// all entity/component/singleton changes through this interface.
    /// </para>
    /// </summary>
    public interface ICommandBuffer : System.IDisposable
    {
        void EndWrite();
        
        // ──────────────────────────────────────────────────────────────────
        // Entity lifecycle
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Record the creation of a new entity in the bound world.
        /// The entity becomes alive only when the buffer is applied at a barrier.
        /// </summary>
        /// <returns>
        /// An <see cref="Entity"/> handle that can be used to record further
        /// operations (components, tags, etc.) in the same buffer.
        /// </returns>
        Entity SpawnEntity();

        /// <summary>
        /// Record the despawn of an entity.
        /// The entity will be removed and all components destroyed when the
        /// buffer is applied at a barrier.
        /// </summary>
        void DespawnEntity(Entity e);

        // ──────────────────────────────────────────────────────────────────
        // Component operations
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Record adding a component to an entity.
        /// If the entity is not alive by the time the buffer is applied,
        /// this operation becomes a no-op.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">Component value to add.</param>
        void AddComponent<T>(Entity e, in T value) where T : struct;
        void AddComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Record replacing (or setting) a component value on an entity.
        /// If the component is missing when the buffer is applied, it may be
        /// created depending on world policy.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">New component value.</param>
        void ReplaceComponent<T>(Entity e, in T value) where T : struct;
        void ReplaceComponentBoxed(Entity e, object? boxed);

        /// <summary>
        /// Record removing a component from an entity.
        /// If the component is absent when the buffer is applied, this becomes a no-op.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="e">Target entity.</param>
        void RemoveComponent<T>(Entity e) where T : struct;
        void RemoveComponentTyped(Entity e, Type type);

        // ──────────────────────────────────────────────────────────────────
        // Singleton operations
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Record setting a singleton component.
        /// <para>
        /// If a singleton of type <typeparamref name="T"/> already exists,
        /// its value is replaced. Otherwise, a dedicated singleton entity is
        /// created to hold the new value.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Singleton component type.</typeparam>
        /// <param name="value">New singleton value.</param>
        void SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent;

        /// <summary>
        /// Record removal of a singleton component and its dedicated entity.
        /// If the singleton does not exist, this becomes a no-op.
        /// </summary>
        /// <typeparam name="T">Singleton component type.</typeparam>
        void RemoveSingleton<T>() where T : struct, IWorldSingletonComponent;
    }
}