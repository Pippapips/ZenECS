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

using System;
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
#if ZENECS_ZENJECT
    /// <summary>
    /// Zenject-based project installer for ZenECS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the <c>ZENECS_ZENJECT</c> scripting define is enabled, this
    /// <see cref="ProjectInstaller"/> is used as a Zenject
    /// <see cref="MonoInstaller"/> to:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Create the global <see cref="IKernel"/> via
    /// <see cref="KernelLocator.CreateEcsDriverWithKernel(KernelOptions)"/>.
    /// </description></item>
    /// <item><description>
    /// Bind the kernel and shared service resolvers into the Zenject
    /// <see cref="DiContainer"/>.
    /// </description></item>
    /// <item><description>
    /// Expose the resolved services through <see cref="ZenEcsUnityBridge"/>
    /// for convenient access by editor tooling and runtime code.
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class ProjectInstaller : MonoInstaller
    {
        /// <summary>
        /// Configures Zenject bindings for ZenECS kernel and resolvers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Creates the kernel and assigns it to
        /// <see cref="ZenEcsUnityBridge.Kernel"/>.
        /// </description></item>
        /// <item><description>
        /// Binds the kernel instance into the container.
        /// </description></item>
        /// <item><description>
        /// Registers <see cref="ISharedContextResolver"/> and
        /// <see cref="ISystemPresetResolver"/> as singletons.
        /// </description></item>
        /// </list>
        /// </remarks>
        public override void InstallBindings()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel(
                new KernelOptions
                {
                    AutoSelectNewWorld = false,
                    StepOnlyCurrentWhenSelected = false,
                });

            Container.BindInstance(ZenEcsUnityBridge.Kernel);
            Container.Bind<ISharedContextResolver>().To<SharedContextResolver>().AsSingle();
            Container.Bind<ISystemPresetResolver>().To<SystemPresetResolver>().AsSingle();
        }

        /// <summary>
        /// Unity lifecycle callback used to export resolved services to
        /// <see cref="ZenEcsUnityBridge"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After Zenject has created and bound all services, this method
        /// resolves the shared context and system preset resolvers and assigns
        /// them to <see cref="ZenEcsUnityBridge"/> so that non-DI code and
        /// editor tools can access them.
        /// </para>
        /// </remarks>
        private void Awake()
        {
            ZenEcsUnityBridge.SharedContextResolver = Container.Resolve<ISharedContextResolver>();
            ZenEcsUnityBridge.SystemPresetResolver = Container.Resolve<ISystemPresetResolver>();
        }
    }
#else
    /// <summary>
    /// Minimal project-level bootstrap for ZenECS without Zenject.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>ZENECS_ZENJECT</c> is <b>not</b> defined, this
    /// <see cref="MonoBehaviour"/> takes the role of a lightweight installer.
    /// In <see cref="Awake"/>, it:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Creates the global <see cref="IKernel"/> via
    /// <see cref="KernelLocator.CreateEcsDriverWithKernel(KernelOptions)"/>.
    /// </description></item>
    /// <item><description>
    /// Assigns the kernel and default resolver implementations to
    /// <see cref="ZenEcsUnityBridge"/>.
    /// </description></item>
    /// </list>
    /// <para>
    /// The <see cref="DefaultExecutionOrderAttribute"/> ensures that the
    /// kernel is created very early in the frame, before most other
    /// MonoBehaviours.
    /// </para>
    /// </remarks>
    [DefaultExecutionOrder(-32001)]
    public sealed class ProjectInstaller : MonoBehaviour
    {
        /// <summary>
        /// Unity lifecycle callback that initializes the ZenECS kernel and
        /// default resolvers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Creates the kernel and assigns it to
        /// <see cref="ZenEcsUnityBridge.Kernel"/>.
        /// </description></item>
        /// <item><description>
        /// Instantiates <see cref="SharedContextResolver"/> and
        /// <see cref="SystemPresetResolver"/> and exposes them via
        /// <see cref="ZenEcsUnityBridge"/>.
        /// </description></item>
        /// </list>
        /// <para>
        /// It is safe to have a single <see cref="ProjectInstaller"/> per
        /// project (for example, in the initial bootstrap scene).
        /// </para>
        /// </remarks>
        private void Awake()
        {
            ZenEcsUnityBridge.Kernel = KernelLocator.CreateEcsDriverWithKernel(
                new KernelOptions
                {
                    AutoSelectNewWorld = false,
                    StepOnlyCurrentWhenSelected = false,
                });

            ZenEcsUnityBridge.SharedContextResolver = new SharedContextResolver();
            ZenEcsUnityBridge.SystemPresetResolver = new SystemPresetResolver();
        }
    }
#endif
}
