// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 08 - Zenject Integration (Shared Component)
// File: UnityTransformContextAsset.cs
// Purpose: Example Per-Entity Context Asset that creates UnityTransformContext
// Key concepts:
//   • Context Asset inheriting from PerEntityContextAsset
//   • ScriptableObject-based Context factory
//   • Asset creation menu via CreateAssetMenu
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;
using Object = UnityEngine.Object;

namespace ZenEcsAdapterUnitySamples.ZenjectSamples
{
    /// <summary>
    /// ScriptableObject asset that creates UnityTransformContext instances.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ZenECSAdapterUnitySample/PerEntityUnityTransformContext",
        fileName = "UnityTransformContext")]
    public sealed class UnityTransformContextAsset : PerEntityContextAsset
    {
        /// <summary>The prefab to instantiate for each entity.</summary>
        [SerializeField] private GameObject? _modelPrefab;
        
        /// <inheritdoc />
        public override Type ContextType => typeof(UnityTransformContext);
        
        /// <inheritdoc />
        public override IContext Create()
        {
            return new UnityTransformContext(_modelPrefab);
        }
    }
}