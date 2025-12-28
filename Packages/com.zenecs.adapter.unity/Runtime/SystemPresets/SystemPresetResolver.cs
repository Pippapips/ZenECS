// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — System Presets
// File: SystemPresetResolver.cs
// Purpose: Default implementation of ISystemPresetResolver that instantiates
//          ISystem implementations either via Zenject (if available) or via
//          Activator.CreateInstance, while skipping already-registered systems.
// Key concepts:
//   • Dual-mode behavior controlled by ZENECS_ZENJECT.
//   • De-duplication: does not create systems that already exist in CurrentWorld.
//   • Safe instantiation: logs warnings instead of throwing on failures.
//   • Kernel-aware: consults ZenEcsUnityBridge.Kernel for existing systems.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core.Systems;
using Object = System.Object;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.SystemPresets
{
    /// <summary>
    /// Default implementation of <see cref="ISystemPresetResolver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SystemPresetResolver"/> turns a list of <see cref="Type"/>
    /// descriptors into live <see cref="ISystem"/> instances, while avoiding
    /// duplicate system registrations on the current world.
    /// </para>
    /// <para>
    /// The behavior depends on the <c>ZENECS_ZENJECT</c> scripting define:
    /// </para>
    /// <list type="bullet">
        /// <item><description>
        /// <b>Zenject mode</b> (<c>ZENECS_ZENJECT</c> defined): systems are
        /// instantiated via Zenject DiContainer.Instantiate.
        /// </description></item>
    /// <item><description>
    /// <b>Non-Zenject mode</b>: systems are instantiated via
    /// <see cref="Activator.CreateInstance(Type)"/>.
    /// </description></item>
    /// </list>
    /// <para>
        /// In both modes, if <see cref="ZenEcsUnityBridge.Kernel"/> has a
        /// non-null CurrentWorld property, the resolver checks
        /// <see cref="ZenECS.Core.IWorldSystemsApi.TryGetSystem(Type, out ISystem)"/> and skips any type
        /// for which a system already exists.
    /// </para>
    /// </remarks>
    public sealed class SystemPresetResolver : ISystemPresetResolver
    {
#if ZENECS_ZENJECT
        /// <summary>
        /// Dependency injection container used to instantiate systems when
        /// running in Zenject mode.
        /// </summary>
        private readonly DiContainer? _container;

        /// <summary>
        /// Initializes a new instance of <see cref="SystemPresetResolver"/>
        /// that uses the given Zenject <see cref="DiContainer"/> to create
        /// system instances.
        /// </summary>
        /// <param name="container">
        /// The DI container used to instantiate <see cref="ISystem"/> types.
        /// </param>
        public SystemPresetResolver(DiContainer container)
        {
            _container = container;
        }
#endif
        /// <summary>
        /// Initializes a new instance of <see cref="SystemPresetResolver"/> in
        /// non-Zenject mode.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In this configuration, system instances are created via
        /// <see cref="Activator.CreateInstance(Type)"/> when requested.
        /// </para>
        /// </remarks>
        public SystemPresetResolver()
        {
        }

        /// <summary>
        /// Instantiates systems for the given types, skipping ones that are
        /// already registered on the current world.
        /// </summary>
        /// <param name="types">
        /// List of candidate system types to instantiate. Types that are
        /// abstract, do not implement <see cref="ISystem"/>, or already have a
        /// registered instance in the current world are skipped.
        /// </param>
        /// <returns>
        /// A list of newly created <see cref="ISystem"/> instances. The list
        /// may be empty if no new systems are needed or all instantiations
        /// fail.
        /// </returns>
        /// <remarks>
        /// <para>
        /// For each type <c>T</c> in <paramref name="types"/>:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// If <see cref="ZenEcsUnityBridge.Kernel"/> and its
        /// CurrentWorld property are non-null, call
        /// <see cref="ZenECS.Core.IWorldSystemsApi.TryGetSystem(Type, out ISystem)"/>; if a system
        /// is already registered for <c>T</c>, it is skipped.
        /// </description></item>
        /// <item><description>
        /// Otherwise, a new instance is created using either Zenject
        /// (DiContainer.Instantiate) or
        /// <see cref="Activator.CreateInstance(Type)"/>.
        /// </description></item>
        /// <item><description>
        /// If instantiation fails, a warning is logged to the Unity console
        /// and the type is ignored.
        /// </description></item>
        /// </list>
        /// </remarks>
        public List<ISystem> InstantiateSystems(List<Type> types)
        {
#if ZENECS_ZENJECT
            if (_container == null)
            {
                return InstantiateSystemsInternal(types, t => (ISystem)Activator.CreateInstance(t));
            }
            return InstantiateSystemsInternal(types, t => (ISystem)_container.Instantiate(t));
#else
            var list = new List<ISystem>(types.Count);
            foreach (var t in types)
            {
                try
                {
                    if (t == null || t.IsAbstract) continue;
                    var inst = Activator.CreateInstance(t);
                    if (inst is ISystem s)
                        list.Add(s);
                    else
                        Debug.LogWarning($"[WorldSystemInstaller] Type '{t.FullName}' is not an ISystem.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldSystemInstaller] new() failed: {t?.Name} — {ex.Message}");
                }
            }
            return list;
#endif
        }

#if ZENECS_ZENJECT
        /// <summary>
        /// Internal helper that instantiates systems using the provided factory function.
        /// </summary>
        /// <param name="types">List of system types to instantiate.</param>
        /// <param name="factory">Factory function that creates an ISystem instance from a Type.</param>
        /// <returns>A list of successfully instantiated systems.</returns>
        private static List<ISystem> InstantiateSystemsInternal(List<Type> types, Func<Type, ISystem> factory)
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            var list = new List<ISystem>(types.Count);

            foreach (var t in types)
            {
                // Skip if system already exists in current world
                if (kernel is { CurrentWorld: not null })
                {
                    if (kernel.CurrentWorld.TryGetSystem(t, out ISystem? system))
                        continue;
                }

                try
                {
                    list.Add(factory(t));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SystemPresetResolver] instantiate failed: {t?.Name} — {ex.Message}");
                }
            }

            return list;
        }
#endif
    }
}
