// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 08 - Zenject Integration (Shared Component)
// File: UnityTransformContext.cs
// Purpose: Example of Per-Entity Context that connects Unity GameObject to Entity
// Key concepts:
//   • IContext interface implementation
//   • Reinitialization support via IContextReinitialize
//   • GameObject ↔ Entity connection via EntityLink
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenEcsAdapterUnitySamples.ZenjectSamples
{
    /// <summary>
    /// Entity-owned model context wrapping a Unity GameObject instance.
    /// </summary>
    public sealed class UnityTransformContext : IContext, IContextReinitialize
    {
        /// <summary>The instantiated GameObject for this entity's model.</summary>
        public GameObject? Instance { get; set; } = null!;

        /// <summary>Cached root transform for fast access.</summary>
        public Transform? Root { get; set; } = null!;

        private GameObject? _prefab;

        /// <summary>
        /// Initializes a new instance of UnityTransformContext.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate for each entity.</param>
        public UnityTransformContext(GameObject? prefab)
        {
            _prefab = prefab;
        }
        
        /// <inheritdoc />
        public void Initialize(IWorld w, Entity e, IContextLookup l)
        {
            if (!_prefab)
                return;
            
            Instance = Object.Instantiate(_prefab);
            Root = Instance.transform;

            Instance.CreateEntityLink(w, e);
        }
        
        /// <inheritdoc />
        public void Deinitialize(IWorld w, Entity e)
        {
            Instance?.DestroyEntityLink();
            
            Object.Destroy(Instance);
            Instance = null;
            Root = null;
        }
        
        /// <inheritdoc />
        public void Reinitialize(IWorld w, Entity e, IContextLookup l)
        {
            Deinitialize(w, e);
            Initialize(w, e, l);
        }
    }
}