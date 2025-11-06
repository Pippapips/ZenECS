#nullable enable
using System;
using ZenECS.Core.DI;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.ComponentPooling;
using ZenECS.Core.Internal.Contexts;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Internal.Bootstrap
{
    /// <summary>
    /// Central place to assemble the Core's internal dependency graph.
    /// Keep this thin and engine-agnostic — adapters can overlay their own services in child scopes.
    /// </summary>
    internal static class CoreBootstrap
    {
        /// <summary>
        /// Build the process-wide root container.
        /// Register global, long-lived singletons here (e.g., KernelOptions).
        /// </summary>
        internal static ServiceContainer BuildRoot(KernelOptions? options = null)
        {
            var root = new ServiceContainer();

            // KernelOptions as a configurable singleton
            root.RegisterSingleton(options ?? new KernelOptions(), takeOwnership: false);

            // Global utilities (e.g., logging) can be registered here if needed.
            return root;
        }

        /// <summary>
        /// Build a per-world child scope. WorldImpl can optionally resolve services from here.
        /// Keep it lean; per-world resources should be owned and disposed with the world lifetime.
        /// </summary>
        internal static ServiceContainer BuildWorldScope(ServiceContainer root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            var world = root.CreateChildScope();

            // Composition Root
            world.RegisterFactory<IWorker>(_ => new Worker(), asSingleton: true);
            world.RegisterFactory<IMessageBus>(_ => new MessageBus(), asSingleton: true);
            world.RegisterFactory<IContextRegistry>(_ => new ContextRegistry(), asSingleton: true);
            world.RegisterFactory<IComponentPoolRepository>(_ => new ComponentPoolRepository(), asSingleton: true);
            world.RegisterFactory<IBindingRouter>(sp => new BindingRouter(
                    sp.GetRequired<IContextRegistry>()), asSingleton: true);
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