#nullable enable
using System;

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
        internal static ServiceHost BuildRoot(KernelOptions? options = null)
        {
            var root = new ServiceHost();

            // KernelOptions as a configurable singleton
            root.RegisterSingleton(options ?? new KernelOptions(), takeOwnership: false);

            // Global utilities (e.g., logging) can be registered here if needed.
            return root;
        }

        /// <summary>
        /// Build a per-world child scope. WorldImpl can optionally resolve services from here.
        /// Keep it lean; per-world resources should be owned and disposed with the world lifetime.
        /// </summary>
        internal static ServiceHost BuildWorldScope(ServiceHost root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            var world = root.CreateChildScope();

            // Example registrations (commented — WorldImpl currently constructs these directly):
            // world.RegisterFactory<EntityStore>(_ => new EntityStore(), asSingleton: true);
            // world.RegisterFactory<MessageBus>(_ => new MessageBus(), asSingleton: true);
            // world.RegisterFactory<EventHub>(_ => new EventHub(), asSingleton: true);
            // world.RegisterFactory<QueryGateway>(h => new QueryGateway(h.GetRequired<EntityStore>()), asSingleton: true);
            world.RegisterFactory<ISystemRunner>(_ => new DefaultSystemRunner(), asSingleton: true);

            return world;
        }
    }
}