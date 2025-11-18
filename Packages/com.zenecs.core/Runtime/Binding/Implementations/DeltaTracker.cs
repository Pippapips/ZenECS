using System;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Tracks component state and a "dirty" flag for a single component type
    /// based purely on delta notifications.
    /// </summary>
    /// <typeparam name="T">The component value type.</typeparam>
    public struct DeltaTracker<T> where T : struct
    {
        /// <summary>
        /// True if the component is currently present on the entity
        /// after the last processed delta.
        /// </summary>
        public bool Has;

        /// <summary>
        /// True if there is a pending change that should be applied
        /// to the view in the next <c>OnApply</c> call.
        /// </summary>
        public bool Dirty;

        /// <summary>
        /// True if the last processed delta represented a removal
        /// (i.e. the component was removed from the entity).
        /// This is useful if the view needs to actively "unset" something.
        /// </summary>
        public bool Removed;

        /// <summary>
        /// The last known value of the component, as reported by deltas.
        /// Only meaningful when <see cref="Has"/> is true.
        /// </summary>
        public T Last;

        /// <summary>
        /// Returns true if the tracker has any change that needs to be applied
        /// to the view (i.e. <see cref="Dirty"/> is true).
        /// </summary>
        public bool NeedsApply => Dirty;

        /// <summary>
        /// Clears all flags and resets the tracker to an "empty" state.
        /// Call this when the binder is unbound or reused.
        /// </summary>
        public void Reset()
        {
            Has = false;
            Dirty = false;
            Removed = false;
            Last = default!;
        }

        /// <summary>
        /// Sets the component as present with the given value and marks it as dirty.
        /// Intended for initial snapshots or "add/update" deltas.
        /// </summary>
        /// <param name="value">The latest component value.</param>
        /// <param name="markDirty">
        /// If true (default), <see cref="Dirty"/> is set so that the view
        /// will be updated on the next apply.
        /// </param>
        public void Set(in T value, bool markDirty = true)
        {
            Last = value;
            Has = true;
            Removed = false;

            if (markDirty)
                Dirty = true;
        }

        /// <summary>
        /// Marks the component as removed. Optionally marks as dirty so
        /// the view can react to the removal.
        /// </summary>
        /// <param name="markDirty">
        /// If true (default), <see cref="Dirty"/> is set.
        /// </param>
        public void MarkRemoved(bool markDirty = true)
        {
            Has = false;
            Removed = true;

            if (markDirty)
                Dirty = true;
        }

        /// <summary>
        /// Clears the dirty/removed flags after the view has consumed
        /// the latest state during an apply pass.
        /// </summary>
        public void ClearDirty()
        {
            Dirty = false;
            Removed = false;
        }

        // ------------------------------------------------------------------
        // Optional helper: integrate directly with your ComponentDelta<T>.
        // Adjust field/property names to match your actual delta type.
        // ------------------------------------------------------------------

        /// <summary>
        /// Applies a <c>ComponentDelta&lt;T&gt;</c> to the tracker.
        /// This assumes a delta type with a <c>Kind</c> and <c>Value</c>.
        /// Adjust to your actual delta API.
        /// </summary>
        /// <param name="delta">The delta describing the component change.</param>
        public void ApplyDelta(in ComponentDelta<T> delta)
        {
            switch (delta.Kind)
            {
                case ComponentDeltaKind.Snapshot:
                case ComponentDeltaKind.Added:
                case ComponentDeltaKind.Changed:
                    Set(delta.Value, markDirty: true);
                    break;

                case ComponentDeltaKind.Removed:
                    MarkRemoved(markDirty: true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(delta.Kind),
                        delta.Kind,
                        "Unknown ComponentDeltaKind");
            }
        }
    }
}