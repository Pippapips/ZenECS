// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Bootstrap / DI Composition Root
// File: CoreBootstrap.cs
// Purpose: Assemble process-wide and per-world service graphs for Core internals.
// Key concepts:
//   • Root scope: app-lifetime singletons (e.g., KernelOptions).
//   • World scope: per-world services (worker, message bus, router, pools, hooks).
//   • Engine-agnostic: adapters can extend/override in child scopes (Unity, server).
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.ComponentPooling;
using ZenECS.Core.Internal.Contexts;
using ZenECS.Core.Internal.DI;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Messaging;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Internal.Systems;

namespace ZenECS.Core.Internal.Bootstrap
{
    /// <summary>
    /// Central place to assemble the Core's internal dependency graph.
    /// Keep this thin and engine-agnostic — adapters can overlay their own services in child scopes.
    /// </summary>
    internal static class CoreBootstrap
    {
        /// <summary>
        /// Build the process-wide root container and register long-lived singletons.
        /// </summary>
        /// <param name="options">Optional kernel options instance.</param>
        /// <returns>Root <see cref="ServiceContainer"/>.</returns>
        internal static ServiceContainer BuildRoot(KernelOptions? options = null)
        {
            var root = new ServiceContainer();
            root.RegisterSingleton(options ?? new KernelOptions(), takeOwnership: false);
            root.Seal();
            return root;
        }

        /// <summary>
        /// Build a per-world child scope, registering world-scoped services.
        /// </summary>
        /// <param name="cfg">World configuration for initial capacities.</param>
        /// <param name="root">Root container.</param>
        /// <returns>World child scope (sealed).</returns>
        /// <exception cref="ArgumentNullException">Root is null.</exception>
        internal static ServiceContainer BuildWorldScope(WorldConfig cfg, ServiceContainer root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            var world = root.CreateChildScope();

            // Composition Root
            world.RegisterFactory<IWorker>(_ => new Worker(), asSingleton: true);
            world.RegisterFactory<IMessageBus>(_ => new MessageBus(), asSingleton: true);
            world.RegisterFactory<IContextRegistry>(_ => new ContextRegistry(), asSingleton: true);
            world.RegisterFactory<IComponentPoolRepository>(_ => new ComponentPoolRepository(cfg.InitialPoolBuckets), asSingleton: true);
            world.RegisterFactory<IBindingRouter>(sp => new BindingRouter(
                sp.GetRequired<IContextRegistry>(),
                cfg.InitialBinderBuckets), asSingleton: true);
            world.RegisterFactory<IPermissionHook>(_ => new PermissionHook(), asSingleton: true);
            world.RegisterFactory<ISystemRunner>(sp => new SystemRunner(
                sp.GetRequired<IMessageBus>(),
                sp.GetRequired<IWorker>(),
                sp.GetRequired<IBindingRouter>(),
                sp.GetRequired<IPermissionHook>()), asSingleton: true);

            world.Seal();
            return world;
        }
    }
}
