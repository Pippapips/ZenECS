// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Adapter.Unity — Binding
// File: Runtime/Binding/Contexts/Assets/ContextAsset.cs
// Purpose: Base ScriptableObject for context configuration assets.
// Key concepts:
//   • Editor-only config: SO stores references to prefabs, canvases, etc.
//   • Runtime factory: derived assets create IContext instances as needed.
//   • Split into shared (world-level) and per-entity variants.
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    /// <summary>
    /// Base asset for Unity-side context configuration.
    /// Concrete subclasses are either shared or per-entity context factories.
    /// </summary>
    public abstract class ContextAsset : ScriptableObject
    {
    }
}