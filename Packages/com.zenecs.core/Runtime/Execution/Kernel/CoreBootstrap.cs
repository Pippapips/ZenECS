// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Bootstrap / DI Composition Root
// File: CoreBootstrap.cs
// Purpose: Assemble process-wide and per-world service graphs for Core internals.
// Key concepts:
//   • Root scope: app-lifetime singletons (e.g., KernelOptions).
//   • World scope: per-world services (worker, message bus, router, pools, hooks).
//   • Engine-agnostic: adapters can extend/override in child scopes (Unity, server).
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core.Infrastructure.Internal;
using ZenECS.Core.Binding.Internal;
using ZenECS.Core.ComponentPooling.Internal;
using ZenECS.Core.Hooking.Internal;
using ZenECS.Core.Messaging.Internal;
using ZenECS.Core.Scheduling.Internal;
using ZenECS.Core.Systems.Internal;

namespace ZenECS.Core.Internal.Bootstrap
{
    /// <summary>
    /// Central place to assemble the Core's internal dependency graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is intentionally engine-agnostic: it knows only about core
    /// services and does not reference Unity, networking stacks, or other
    /// host-specific APIs. Adapters (Unity, server, headless, etc.) are expected
    /// to create child scopes and register additional or overriding services.
    /// </para>
    /// <para>
    /// Typical flow:
    /// <list type="number">
    /// <item><description>Call <see cref="BuildRoot"/> once at application startup.</description></item>
    /// <item><description>For each world, call <see cref="BuildWorldScope"/> to obtain a sealed child scope.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    internal static class CoreBootstrap
    {
        /// <summary>
        /// Builds the process-wide root container and registers long-lived singletons.
        /// </summary>
        /// <param name="options">
        /// Optional kernel options instance. If <see langword="null"/>,
        /// a new <see cref="KernelOptions"/> instance is created and registered.
        /// </param>
        /// <returns>
        /// A sealed root <see cref="ServiceContainer"/> representing the
        /// application-lifetime scope.
        /// </returns>
        /// <remarks>
        /// The returned container is sealed, so no further registrations can be
        /// added. Child scopes created from this root are used for per-world services.
        /// </remarks>
        internal static ServiceContainer BuildRoot(KernelOptions? options = null)
        {
            var root = new ServiceContainer();

            // Application-wide configuration / options.
            root.RegisterSingleton(options ?? new KernelOptions(), takeOwnership: false);

            root.Seal();
            return root;
        }

        /// <summary>
        /// Builds a per-world child scope, registering world-scoped services.
        /// </summary>
        /// <param name="cfg">
        /// World configuration used to initialize capacities for internal
        /// data structures such as component pools and binder buckets.
        /// </param>
        /// <param name="root">
        /// Root container created by <see cref="BuildRoot"/>. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A sealed child <see cref="ServiceContainer"/> representing the
        /// dependency graph for a single world.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="root"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The world scope contains:
        /// <list type="bullet">
        /// <item><description><see cref="IWorker"/> for executing systems.</description></item>
        /// <item><description><see cref="IMessageBus"/> for intra-world messaging.</description></item>
        /// <item><description><see cref="IContextRegistry"/> for managing contexts.</description></item>
        /// <item><description><see cref="IComponentPoolRepository"/> for component pooling.</description></item>
        /// <item><description><see cref="IBindingRouter"/> for binder routing.</description></item>
        /// <item><description><see cref="IPermissionHook"/> for write-permission checks.</description></item>
        /// <item><description><see cref="ISystemRunner"/> orchestrating system execution.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The child scope is sealed before being returned so that internal
        /// invariants remain stable. Engine adapters are expected to create
        /// their own child scopes layered on top of this one.
        /// </para>
        /// </remarks>
        internal static ServiceContainer BuildWorldScope(WorldConfig cfg, ServiceContainer root)
        {
            if (root is null)
                throw new ArgumentNullException(nameof(root));

            var world = root.CreateChildScope();

            // Composition root — register world-scoped services.

            // Worker runs systems and jobs for this world.
            world.RegisterFactory<IWorker>(_ => new Worker(), asSingleton: true);

            // Message bus for intra-world messaging and events.
            world.RegisterFactory<IMessageBus>(_ => new MessageBus(), asSingleton: true);

            // Context registry to manage per-entity context objects.
            world.RegisterFactory<IContextRegistry>(_ => new ContextRegistry(), asSingleton: true);

            // Component pool repository for struct component pooling.
            world.RegisterFactory<IComponentPoolRepository>(
                _ => new ComponentPoolRepository(cfg.InitialPoolBuckets),
                asSingleton: true);

            // Binding router to dispatch deltas and manage binder lifetimes.
            world.RegisterFactory<IBindingRouter>(
                sp => new BindingRouter(
                    sp.GetRequired<IContextRegistry>(),
                    cfg.InitialBinderBuckets),
                asSingleton: true);

            // Permission hook for structural write validation.
            world.RegisterFactory<IPermissionHook>(
                _ => new PermissionHook(),
                asSingleton: true);

            // System runner orchestrates system execution pipeline.
            world.RegisterFactory<ISystemRunner>(
                sp => new SystemRunner(
                    sp.GetRequired<IMessageBus>(),
                    sp.GetRequired<IWorker>(),
                    sp.GetRequired<IBindingRouter>(),
                    sp.GetRequired<IPermissionHook>()),
                asSingleton: true);

            world.Seal();
            return world;
        }
    }
}
