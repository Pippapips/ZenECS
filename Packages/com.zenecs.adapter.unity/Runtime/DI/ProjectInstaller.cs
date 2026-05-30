// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — DI
// File: ProjectInstaller.cs
// Purpose: Application-level installer that creates the ZenECS kernel and
//          wires up shared resolvers (contexts, system presets), either via
//          Zenject or a lightweight manual setup.
// Key concepts:
//   • Kernel bootstrap: single global IKernel instance via KernelLocator.
//   • ZenEcsUnityBridge: shared static access for editor/runtime tools.
//   • Dual mode:
//       - With Zenject: MonoInstaller-driven dependency injection.
//       - Without Zenject: MonoBehaviour-based bootstrap in Awake.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;

#if ZENECS_ZENJECT
using Zenject;
#else
using UnityEngine;
#endif

namespace ZenECS.Adapter.Unity.DI
{
    /// <summary>
    /// Scene-level bootstrap for the ZenECS kernel and shared resolvers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the <c>ZENECS_ZENJECT</c> scripting define is enabled, this
    /// <see cref="ProjectInstaller"/> is used as a Zenject
    /// <see cref="MonoInstaller"/> to create the kernel, bind shared services,
    /// and publish them to <see cref="ZenEcsUnityBridge"/> during install.
    /// </para>
    /// <para>
    /// When Zenject is not available, the same services are assigned to the
    /// bridge directly from <see cref="Awake"/>.
    /// </para>
    /// </remarks>
#if ZENECS_ZENJECT
    public sealed class ProjectInstaller : MonoInstaller
#else
    [DefaultExecutionOrder(-32001)]
    public sealed class ProjectInstaller : MonoBehaviour
#endif
    {
        /// <summary>
        /// Default kernel options used when bootstrapping from this installer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Both <see cref="KernelOptions.AutoSelectNewWorld"/> and
        /// <see cref="KernelOptions.StepOnlyCurrentWhenSelected"/> are disabled
        /// so that game code retains explicit control over world selection and
        /// stepping.
        /// </para>
        /// </remarks>
        static KernelOptions DefaultKernelOptions => new()
        {
            AutoSelectNewWorld = false,
            StepOnlyCurrentWhenSelected = false,
        };

#if ZENECS_ZENJECT
        /// <summary>
        /// Configures Zenject bindings for the ZenECS kernel and resolvers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// Creates the global <see cref="IKernel"/> via
        /// <see cref="KernelLocator.CreateEcsDriverWithKernel(KernelOptions?, bool)"/>
        /// and assigns it to <see cref="ZenEcsUnityBridge.Kernel"/>.
        /// </description></item>
        /// <item><description>
        /// Binds the kernel instance into the Zenject container.
        /// </description></item>
        /// <item><description>
        /// Registers <see cref="ISharedContextResolver"/> and
        /// <see cref="ISystemPresetResolver"/> as non-lazy singletons and
        /// publishes each instance to <see cref="ZenEcsUnityBridge"/> through
        /// <c>OnInstantiated</c> callbacks during install.
        /// </description></item>
        /// </list>
        /// </remarks>
        public override void InstallBindings()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel(DefaultKernelOptions);
            Container.BindInstance(ZenEcsUnityBridge.Kernel);

            Container.Bind<ISharedContextResolver>()
                .To<SharedContextResolver>()
                .AsSingle()
                .OnInstantiated<ISharedContextResolver>((_, resolver) =>
                    ZenEcsUnityBridge.SharedContextResolver = resolver)
                .NonLazy();

            Container.Bind<ISystemPresetResolver>()
                .To<SystemPresetResolver>()
                .AsSingle()
                .OnInstantiated<ISystemPresetResolver>((_, resolver) =>
                    ZenEcsUnityBridge.SystemPresetResolver = resolver)
                .NonLazy();
        }
#else
        /// <summary>
        /// Unity lifecycle callback used to bootstrap ZenECS without Zenject.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates the kernel and resolver instances, then assigns them to
        /// <see cref="ZenEcsUnityBridge"/> so that non-DI code and editor tools
        /// can access them.
        /// </para>
        /// </remarks>
        private void Awake()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel(DefaultKernelOptions);
            ZenEcsUnityBridge.SharedContextResolver = new SharedContextResolver();
            ZenEcsUnityBridge.SystemPresetResolver = new SystemPresetResolver();
        }
#endif
    }
}
