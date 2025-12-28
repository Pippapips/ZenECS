// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: SharedContextResolver.cs
// Purpose: Default implementation of ISharedContextResolver that can either
//          delegate to a Zenject DiContainer or manage contexts manually.
// Key concepts:
//   • Dual-mode behavior controlled by ZENECS_ZENJECT define.
//   • Zenject mode: resolve contexts from an IoC container.
//   • Manual mode: store contexts in an internal type→instance dictionary.
//   • Marker-based lookup: resolve by SharedContextAsset.ContextType metadata.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;

#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Binding.Contexts
{
    /// <summary>
    /// Default implementation of <see cref="ISharedContextResolver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This resolver supports two operation modes, selected at compile time by
    /// the <c>ZENECS_ZENJECT</c> scripting define symbol:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Zenject mode</b> (<c>ZENECS_ZENJECT</c> defined): contexts are
    /// resolved from a Zenject DiContainer; lifecycle is entirely
    /// managed by Zenject. <see cref="AddContext"/>,
    /// <see cref="RemoveContext"/>, and <see cref="RemoveAllContexts"/> are
    /// implemented as no-ops.
    /// </description></item>
    /// <item><description>
    /// <b>Manual mode</b> (<c>ZENECS_ZENJECT</c> not defined): contexts are
    /// stored in an internal <see cref="Dictionary{TKey,TValue}"/> keyed by
    /// their runtime <see cref="System.Type"/>. The resolver provides both
    /// lookup and registration/removal operations.
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class SharedContextResolver : ISharedContextResolver
    {
#if ZENECS_ZENJECT
        private readonly DiContainer? _container;

        /// <summary>
        /// Initializes a new resolver that delegates context resolution to a
        /// Zenject <see cref="DiContainer"/>.
        /// </summary>
        /// <param name="container">
        /// The dependency injection container from which shared context
        /// instances will be resolved.
        /// </param>
        public SharedContextResolver(DiContainer container)
        {
            _container = container;
        }
#endif
        private readonly Dictionary<Type, IContext> _contexts = new();
        
        /// <summary>
        /// Initializes a new resolver in manual mode (non-Zenject).
        /// </summary>
        /// <remarks>
        /// <para>
        /// In this configuration, contexts are stored in an internal dictionary
        /// and must be registered via <see cref="AddContext"/> before they can
        /// be resolved.
        /// </para>
        /// </remarks>
        public SharedContextResolver()
        {
            
        }
        
        /// <inheritdoc />
        public IContext? Resolve(SharedContextAsset marker)
        {
            if (!marker) return null;
#if ZENECS_ZENJECT
            if (_container == null)
            {
                return _contexts.GetValueOrDefault(marker.ContextType);
            }
            return (IContext)_container.Resolve(marker.ContextType);
#else
            return _contexts.GetValueOrDefault(marker.ContextType);
#endif
        }

        /// <inheritdoc />
        public IContext? Resolve<T>() where T : IContext
        {
#if ZENECS_ZENJECT
            if (_container == null)
            {
                return _contexts.GetValueOrDefault(typeof(T));
            }
            return _container.Resolve<T>();
#else
            return _contexts.GetValueOrDefault(typeof(T));
#endif
        }

        /// <inheritdoc />
        public void AddContext(IContext? context)
        {
            if (context == null) return;

            var t = context.GetType();
            _contexts[t] = context;
        }

        /// <inheritdoc />
        public void RemoveContext(IContext? context)
        {
            if (context == null) return;

            var t = context.GetType();
            _contexts.Remove(t);
        }

        /// <inheritdoc />
        public void RemoveAllContexts()
        {
            _contexts.Clear();
        }
    }
}
