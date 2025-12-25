// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 08 - Zenject Integration (Shared Component)
// File: UnityTransformSyncBinderAsset.cs
// Purpose: Example Binder Asset that creates UnityTransformSyncBinder
// Key concepts:
//   • Binder Asset inheriting from BinderAsset
//   • ScriptableObject-based Binder factory
//   • Asset creation menu via CreateAssetMenu
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Binders.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenEcsAdapterUnitySamples.ZenjectSamples
{
    /// <summary>
    /// ScriptableObject asset that creates UnityTransformSyncBinder instances.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ZenECSAdapterUnitySample/BinderUnityTransformSyncBinder",
        fileName = "UnityTransformSyncBinder")]
    public sealed class UnityTransformSyncBinderAsset : BinderAsset
    {
        /// <inheritdoc />
        public override Type BinderType => typeof(UnityTransformSyncBinder);
        
        /// <inheritdoc />
        public override IBinder Create()
        {
            return new UnityTransformSyncBinder();
        }
    }
}