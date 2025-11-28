// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: ISharedContextResolver.cs
// Purpose: Abstraction for resolving shared (global) IContext instances
//          either from marker assets or directly by type.
// Key concepts:
//   • Shared context lookup: resolve global/shared contexts on demand.
//   • Pluggable backend: DI container (Zenject) or manual registry.
//   • Lightweight contract: no assumptions about lifetime management.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts
{
    /// <summary>
    /// Resolves shared <see cref="IContext"/> instances from marker assets or type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstraction allows binding code to obtain shared/global context
    /// instances without knowing how they are created or stored. A concrete
    /// implementation may:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Use a dependency injection container (for example, Zenject) to resolve
    /// <see cref="IContext"/> instances.
    /// </description></item>
    /// <item><description>
    /// Maintain an internal dictionary that maps context types to instances.
    /// </description></item>
    /// </list>
    /// <para>
    /// The interface does not define any lifetime policy; it is up to the
    /// implementation and surrounding application code to decide when
    /// contexts are created and disposed.
    /// </para>
    /// </remarks>
    public interface ISharedContextResolver
    {
        /// <summary>
        /// Resolves a shared context from a marker asset.
        /// </summary>
        /// <param name="marker">
        /// The <see cref="SharedContextAsset"/> that describes the shared
        /// context to resolve. Implementations typically use
        /// <see cref="SharedContextAsset.ContextType"/> to identify the
        /// concrete <see cref="IContext"/> type.
        /// </param>
        /// <returns>
        /// The resolved <see cref="IContext"/> instance, or <c>null</c> if
        /// the marker is <c>null</c> or no matching context is available.
        /// </returns>
        IContext? Resolve(SharedContextAsset marker);

        /// <summary>
        /// Resolves a shared context by its concrete type.
        /// </summary>
        /// <typeparam name="T">
        /// The concrete <see cref="IContext"/> type to resolve.
        /// </typeparam>
        /// <returns>
        /// The resolved <typeparamref name="T"/> instance, or <c>null</c> if
        /// no matching context is available.
        /// </returns>
        IContext? Resolve<T>() where T : IContext;

        /// <summary>
        /// Adds a context instance to the resolver.
        /// </summary>
        /// <param name="context">
        /// The context instance to register. If <c>null</c>, the call is
        /// ignored.
        /// </param>
        /// <remarks>
        /// <para>
        /// In implementations backed by a DI container, this method may be a
        /// no-op because the container is responsible for managing instances.
        /// In dictionary-based implementations it typically registers or
        /// replaces the context under its runtime type.
        /// </para>
        /// </remarks>
        void AddContext(IContext? context);

        /// <summary>
        /// Removes a context instance from the resolver.
        /// </summary>
        /// <param name="context">
        /// The context instance to remove. If <c>null</c>, the call is
        /// ignored.
        /// </param>
        /// <remarks>
        /// <para>
        /// In dictionary-based implementations this usually removes the entry
        /// associated with the context's runtime type. DI-based implementations
        /// may treat this as a no-op.
        /// </para>
        /// </remarks>
        void RemoveContext(IContext? context);

        /// <summary>
        /// Clears all registered contexts from the resolver.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Dictionary-based implementations typically remove all entries,
        /// effectively resetting the internal registry. DI-based implementations
        /// may treat this as a no-op if lifetime is managed externally.
        /// </para>
        /// </remarks>
        void RemoveAllContexts();
    }
}
