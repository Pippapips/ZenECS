// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: ContextRegistry.cs
// Purpose: Per-world registry of per-entity contexts (resource containers).
// Key concepts:
//   • Lookup-first: TryGet/Get/Has via IContextLookup.
//   • Lifecycle: Initialize/Deinitialize/Reinitialize management.
//   • Sharing policy: registry stores references; ownership defined by caller.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Binding.Internal
{
    /// <summary>
    /// Per-world registry storing contexts keyed by entity and context type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registry manages the association between <see cref="IContext"/> instances
    /// and entities, and drives optional lifecycle hooks such as
    /// <see cref="IContextInitialize"/> and <see cref="IContextReinitialize"/>.
    /// </para>
    /// <para>
    /// It also propagates attach/detach notifications to binders using
    /// <see cref="IContextAwareBinder.ContextAttached"/> and <see cref="IContextAwareBinder.ContextDetached"/>,
    /// so the view layer can react to context changes.
    /// </para>
    /// </remarks>
    internal sealed class ContextRegistry : IContextRegistry
    {
        /// <summary>
        /// Internal entry representing a single context registration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each entry stores a context instance along with its registration metadata.
        /// The entry tracks whether the context has been initialized via
        /// <see cref="IContextInitialize"/> lifecycle hooks.
        /// </para>
        /// <para>
        /// Entries are keyed by the runtime type of the context instance, allowing
        /// type-based lookup while supporting derived context types.
        /// </para>
        /// </remarks>
        private sealed class Entry
        {
            /// <summary>The context instance.</summary>
            public IContext Ctx;

            /// <summary>Type key used for this registration (runtime type of <see cref="Ctx"/>).</summary>
            public Type KeyType;

            /// <summary>
            /// Flag indicating whether <see cref="Ctx"/> has completed its
            /// initialization phase.
            /// </summary>
            public bool Initialized;

            /// <summary>
            /// Creates a new entry for the given context and key type.
            /// </summary>
            /// <param name="ctx">Context instance.</param>
            /// <param name="key">Runtime type used as key.</param>
            public Entry(IContext ctx, Type key)
            {
                Ctx = ctx;
                KeyType = key;
                Initialized = false;
            }
        }

        // World → Entity → (ContextType → Entry)
        private readonly Dictionary<IWorld, Dictionary<Entity, Dictionary<Type, Entry>>> _map
            = new(ReferenceEqualityComparer<IWorld>.Instance);

        /// <summary>
        /// Returns the per-world map (creating it if needed).
        /// </summary>
        /// <param name="w">World key.</param>
        private Dictionary<Entity, Dictionary<Type, Entry>> Bag(IWorld w)
            => _map.TryGetValue(w, out var d) ? d : (_map[w] = new());

        /// <summary>
        /// Returns the per-entity map (creating it if needed).
        /// </summary>
        /// <param name="w">World key.</param>
        /// <param name="e">Entity key.</param>
        private Dictionary<Type, Entry> Bag(IWorld w, Entity e)
            => Bag(w).TryGetValue(e, out var d) ? d : (Bag(w)[e] = new());

        // ── Lookup ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool TryGet<T>(IWorld w, Entity e, out T ctx) where T : class, IContext
        {
            ctx = null!;
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Exact type first
            if (dict.TryGetValue(typeof(T), out var exact) && exact.Ctx is T exactCtx)
            {
                ctx = exactCtx;
                return true;
            }

            // Most-specific assignable type
            Entry? best = null;
            foreach (var kv in dict)
            {
                var t = kv.Key;
                var entry = kv.Value;

                if (!typeof(T).IsAssignableFrom(t))
                    continue;
                if (entry.Ctx is not T cand)
                    continue;

                if (best == null || best.KeyType.IsAssignableFrom(t))
                {
                    best = entry;
                    ctx = cand;
                }
            }

            return ctx != null;
        }

        /// <summary>
        /// Non-generic <see cref="TryGet{T}(IWorld, Entity, out T)"/> that resolves
        /// any <see cref="IContext"/> instance.
        /// </summary>
        private bool TryGet(IWorld w, Entity e, out IContext ctx)
            => TryGet<IContext>(w, e, out ctx!);

        /// <inheritdoc/>
        public T Get<T>(IWorld w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var v)
                ? v
                : throw new KeyNotFoundException($"Context {typeof(T).Name} not found for {e}.");

        /// <inheritdoc/>
        public bool Has<T>(IWorld w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out _);

        /// <inheritdoc/>
        public bool Has(IWorld w, Entity e, IContext? ctx)
        {
            if (ctx == null) return false;
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Check for the exact same instance registered for this entity.
            foreach (var kv in dict)
            {
                if (ReferenceEquals(kv.Value.Ctx, ctx))
                    return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Has(IWorld w, Entity e, Type? contextType)
        {
            if (contextType is null) return false;
            if (!typeof(IContext).IsAssignableFrom(contextType))
                throw new ArgumentException("contextType must implement IContext.", nameof(contextType));

            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Type-compatible match (derived/implementing contexts also match)
            foreach (var kv in dict)
            {
                var c = kv.Value.Ctx;
                if (c != null && contextType.IsInstanceOfType(c))
                    return true;
            }

            return false;
        }

        // ── Register / Remove ────────────────────────────────────────────

        /// <inheritdoc/>
        public void Register(IWorld w, Entity e, IContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var key = ctx.GetType();
            var bag = Bag(w, e);

            if (!bag.TryGetValue(key, out var entry))
            {
                entry = new Entry(ctx, key);
                bag[key] = entry;
            }
            else
            {
                // Replace: deinitialize old if needed.
                if (entry.Initialized && entry.Ctx is IContextInitialize oldIni)
                {
                    oldIni.Deinitialize(w, e);
                    entry.Initialized = false;
                }

                entry.Ctx = ctx;
                entry.KeyType = key;
            }

            // Initialize new context if it supports lifecycle.
            if (ctx is IContextInitialize ini)
            {
                ini.Initialize(w, e, this);
                entry.Initialized = true;
            }

            // Notify binders that a context was attached.
            var binders = w.GetAllBinderList(e);
            if (binders != null)
            {
                foreach (var binder in binders)
                {
                    binder.ContextAttached(ctx);
                }
            }
        }

        /// <inheritdoc/>
        public bool Remove(IWorld w, Entity e, IContext ctx)
        {
            if (ctx == null) return false;
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Locate by exact instance first, then by type compatibility.
            var key = dict.Keys.FirstOrDefault(t => ReferenceEquals(dict[t].Ctx, ctx)) ??
                      dict.Keys.FirstOrDefault(t => t.IsInstanceOfType(ctx));

            if (key == null) return false;

            // Notify binders before removal.
            var binders = w.GetAllBinderList(e);
            if (binders != null)
            {
                foreach (var binder in binders)
                {
                    binder.ContextDetached(ctx);
                }
            }

            var entry = dict[key];

            // Deinitialize if needed.
            if (entry.Initialized && entry.Ctx is IContextInitialize ini)
            {
                ini.Deinitialize(w, e);
                entry.Initialized = false;
            }

            dict.Remove(key);
            if (dict.Count == 0)
                Bag(w).Remove(e);

            return true;
        }

        /// <inheritdoc/>
        public bool Remove<T>(IWorld w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && Remove(w, e, ctx);

        // ── Reinitialize ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool Reinitialize(IWorld w, Entity e, IContext ctx)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Locate by instance first, then by type compatibility.
            var key = dict.Keys.FirstOrDefault(t => ReferenceEquals(dict[t].Ctx, ctx)) ??
                      dict.Keys.FirstOrDefault(t => t.IsInstanceOfType(ctx));

            if (key == null) return false;

            var entry = dict[key];
            if (entry.Ctx is not IContextInitialize ini) return false;

            if (entry.Initialized)
            {
                // Fast path if supported.
                if (entry.Ctx is IContextReinitialize fast)
                {
                    fast.Reinitialize(w, e, this);
                }
                else
                {
                    ini.Deinitialize(w, e);
                    ini.Initialize(w, e, this);
                }
            }
            else
            {
                ini.Initialize(w, e, this);
            }

            entry.Initialized = true;
            return true;
        }

        /// <inheritdoc/>
        public bool Reinitialize<T>(IWorld w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && Reinitialize(w, e, ctx);

        // ── State / Clear ────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsInitialized(IWorld w, Entity e, IContext ctx)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            foreach (var kv in dict)
            {
                if (ReferenceEquals(kv.Value.Ctx, ctx))
                    return kv.Value.Initialized;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsInitialized<T>(IWorld w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && IsInitialized(w, e, ctx);

        /// <inheritdoc/>
        public void Clear(IWorld w, Entity e)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return;

            // Deinitialize all initialized contexts for this entity.
            foreach (var entry in dict.Values.ToArray())
            {
                if (entry.Initialized && entry.Ctx is IContextInitialize ini)
                    ini.Deinitialize(w, e);
            }

            Bag(w).Remove(e);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IContext>? GetAllContextList(IWorld w, Entity e)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return null;
            if (dict.Values.Count == 0) return null;

            // Return a snapshot list to avoid exposing internal collections.
            return dict.Values.Select(entry => entry.Ctx).ToList();
        }

        /// <inheritdoc/>
        public (Type type, object boxed)[] GetAllContexts(IWorld w, Entity e)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return Array.Empty<(Type, object)>();
            if (dict.Count == 0) return Array.Empty<(Type, object)>();

            var contexts = dict.Values.ToArray();
            var arr = new (Type, object)[contexts.Length];

            for (int i = 0; i < contexts.Length; i++)
            {
                var b = contexts[i];
                arr[i] = (b.KeyType, (object)b.Ctx);
            }

            return arr;
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            // Deinitialize everything, then drop the entire map.
            foreach (var (w, perE) in _map)
            {
                foreach (var (e, dict) in perE)
                {
                    foreach (var entry in dict.Values)
                    {
                        if (entry.Initialized && entry.Ctx is IContextInitialize ini)
                            ini.Deinitialize(w, e);
                    }
                }
            }

            _map.Clear();
        }
    }

    /// <summary>
    /// Reference equality comparer for dictionary keys of reference types.
    /// </summary>
    /// <typeparam name="T">Reference type to compare by identity.</typeparam>
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        /// <summary>
        /// Singleton instance of the comparer.
        /// </summary>
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        /// <inheritdoc/>
        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        /// <inheritdoc/>
        public int GetHashCode(T obj) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
