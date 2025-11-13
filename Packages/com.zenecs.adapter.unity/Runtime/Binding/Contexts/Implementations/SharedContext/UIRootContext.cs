#nullable enable
using UnityEngine;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts
{
    /// <summary>
    /// Entity-owned model context wrapping a Unity GameObject instance.
    /// </summary>
    public sealed class UIRootContext : IContext
    {
        /// <summary>The instantiated GameObject for this entity's model.</summary>
        public GameObject CanvasObject { get; set; } = null!;

        /// <summary>Cached root transform for fast access.</summary>
        public Transform CanvasRoot { get; set; } = null!;
    }
}