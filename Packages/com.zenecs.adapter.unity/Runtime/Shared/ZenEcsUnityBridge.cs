// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity
// File: ZenEcsUnityBridge.cs
// Purpose: Global bridge between ZenECS core and Unity-specific infrastructure,
//          exposing shared resolvers and the kernel to both runtime and editor.
// Key concepts:
//   • Static hub: single place where Unity-side code can reach ZenECS services.
//   • Kernel handle: optional global IKernel reference for tools and bootstraps.
//   • Resolvers: shared context + system preset resolvers for world setup.
//   • Editor-safe: no direct UnityEditor references; safe for player builds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Global bridge between ZenECS core and Unity-specific infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ZenEcsUnityBridge"/> is a lightweight static hub that allows
    /// Unity runtime code, editor tools, and installers to coordinate around
    /// a few shared ZenECS services:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="Kernel"/> — the primary <see cref="IKernel"/> instance used
    /// by the application.
    /// </description></item>
    /// <item><description>
    /// <see cref="SystemPresetResolver"/> — used to instantiate
    /// <see cref="ISystem"/> implementations from presets or type lists.
    /// </description></item>
        /// <item><description>
        /// <see cref="SharedContextResolver"/> — used to resolve shared
        /// <see cref="Binding.Contexts.Assets.SharedContextAsset"/> markers to
        /// concrete <see cref="ZenECS.Core.Binding.IContext"/> instances.
        /// </description></item>
    /// </list>
    /// <para>
    /// The bridge intentionally contains no direct references to
    /// <c>UnityEditor</c> so that it is safe to use in player builds as well
    /// as editor-only tooling.
    /// </para>
    /// <para>
    /// Typical initialization flows:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>With Zenject</b> — <c>ProjectInstaller</c> (Zenject installer)
    /// creates the kernel and binds resolvers, then assigns them to the
    /// bridge.
    /// </description></item>
    /// <item><description>
    /// <b>Without Zenject</b> — a lightweight <c>ProjectInstaller</c>
    /// <c>MonoBehaviour</c> or another bootstrap script assigns the kernel
    /// and resolver implementations directly.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static class ZenEcsUnityBridge
    {
        /// <summary>
        /// Gets or sets the resolver used to create <see cref="ISystem"/> instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This resolver is typically used by <c>WorldSystemInstaller</c> to
        /// instantiate systems from a merged list of types and
        /// <see cref="SystemsPreset"/> assets. When <c>null</c>, callers
        /// should fall back to directly constructing systems (for example via
        /// <see cref="System.Activator.CreateInstance(System.Type)"/>).
        /// </para>
        /// </remarks>
        public static ISystemPresetResolver? SystemPresetResolver { get; set; }

        /// <summary>
        /// Gets or sets the shared context resolver used by binding and blueprints.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This resolver is used when applying
        /// <see cref="Binding.Contexts.Assets.SharedContextAsset"/> markers,
        /// for example from <see cref="Blueprints.EntityBlueprint"/> during
        /// entity spawn. It maps marker assets or context types to concrete
        /// <see cref="ZenECS.Core.Binding.IContext"/> instances that are then registered with worlds.
        /// </para>
        /// <para>
        /// When this property is <c>null</c>, shared-context resolution is
        /// simply skipped, and only per-entity contexts are created.
        /// </para>
        /// </remarks>
        public static ISharedContextResolver? SharedContextResolver { get; set; }

        /// <summary>
        /// Gets or sets the optional global ZenECS kernel reference.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is usually initialized in a bootstrap path such as
        /// <c>ProjectInstaller</c> or an editor bootstrap script, and can then
        /// be consumed by runtime code or tools that do not have direct access
        /// to the DI container or scene-level <c>EcsDriver</c>.
        /// </para>
        /// <para>
        /// When <c>null</c>, callers should either:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Use <see cref="KernelLocator.Current"/> to lazily create or locate
        /// a kernel.
        /// </description></item>
        /// <item><description>
        /// Or treat the absence of a kernel as a "not yet initialized" state
        /// and defer their logic.
        /// </description></item>
        /// </list>
        /// </remarks>
        public static IKernel? Kernel { get; set; }

        /// <summary>
        /// Clears all registered services from the bridge.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method resets <see cref="Kernel"/>, <see cref="SharedContextResolver"/>,
        /// and <see cref="SystemPresetResolver"/> to <c>null</c>. It is typically
        /// called during cleanup or when switching between different kernel instances.
        /// </para>
        /// </remarks>
        internal static void Clear()
        {
            Kernel = null;
            SharedContextResolver = null;
            SystemPresetResolver = null;
        }
    }
}
