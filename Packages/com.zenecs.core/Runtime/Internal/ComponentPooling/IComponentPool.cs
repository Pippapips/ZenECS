using System.Collections.Generic;

namespace ZenECS.Core.Internal.ComponentPooling
{
    /// <summary>
    /// Common interface for all component pools.
    /// Keeps the minimal set of APIs required for snapshot save/load and tooling reflection.
    /// </summary>
    internal interface IComponentPool
    {
        /// <summary>
        /// Ensures that the internal storage is large enough to access the given entity ID.
        /// If necessary, expands the underlying arrays.
        /// </summary>
        void EnsureCapacity(int entityId);

        /// <summary>
        /// Returns whether the entity currently holds this component type.
        /// </summary>
        bool Has(int entityId);

        /// <summary>
        /// Removes the component from the given entity.
        /// Optionally clears the stored data to default.
        /// </summary>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>
        /// Retrieves the component as a boxed value (returns null if not present).
        /// </summary>
        object? GetBoxed(int entityId);

        /// <summary>
        /// Sets the component using a boxed value.
        /// Adds a new component or overwrites an existing one.
        /// </summary>
        void SetBoxed(int entityId, object value);

        /// <summary>
        /// Enumerates all active components in the pool as (entityId, boxed value) pairs.
        /// </summary>
        IEnumerable<(int id, object boxed)> EnumerateAll();

        /// <summary>
        /// Returns the number of active components stored in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Clears all data and resets bit flags — typically used before loading a new snapshot.
        /// </summary>
        void ClearAll();
    }
}