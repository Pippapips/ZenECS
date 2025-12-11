// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Component API)
// File: WorldComponentApi.cs
// Purpose: Typed component add/replace/remove and ref accessors.
// Key concepts:
//   • Permission & validation hooks: read/write gates + typed/object validators.
//   • Router deltas: binder layer notified on add/change/remove.
//   • Pools repository: fast ref access and presence checks per component type.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using ZenECS.Core.Binding;
using ZenECS.Core.Config;
using ZenECS.Core.ComponentPooling.Internal;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldComponentApi"/> – component CRUD and ref accessors.
    /// </summary>
    internal sealed partial class World : IWorldComponentApi
    {
        // ── Open generic MethodInfo caches ──────────────────────────────────
        private static MethodInfo? _miHasOpen;
        private static MethodInfo? _miAddOpen;
        private static MethodInfo? _miReplaceOpen;
        private static MethodInfo? _miSnapshotOpen;
        private static MethodInfo? _miRemoveOpen;

        // ── Per-type invoker caches (instance-bound) ────────────────────────
        private readonly Dictionary<Type, Func<Entity, bool>> _hasCache = new();
        private readonly Dictionary<Type, Func<Entity, object, bool>> _addBoxedCache = new();
        private readonly Dictionary<Type, Func<Entity, object, bool>> _replaceBoxedCache = new();
        private readonly Dictionary<Type, Func<Entity, bool>> _snapshotBoxedCache = new();
        private readonly Dictionary<Type, Func<Entity, bool>> _removeCache = new();

        /// <summary>
        /// Per-component-type singleton owner index.
        /// Ensures each singleton component type <c>T</c> has exactly one entity owner.
        /// </summary>
        private readonly Dictionary<Type, Entity> _singletonIndex = new();

        // ── Boxed / non-generic implementations ─────────────────────────────        

        /// <inheritdoc/>
        public bool HasComponentBoxed(Entity e, Type? componentType)
        {
            return componentType is not null && _hasInvoker(componentType)(e);
        }

        /// <summary>
        /// Adds a boxed component value to an entity, if the value is a struct.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value. Must be a non-null value type; otherwise an
        /// exception is thrown or the call is ignored.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the component was added; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="boxed"/> is not a value type.
        /// </exception>
        internal bool AddComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _addBoxedInvoker(t)(e, boxed);
        }

        /// <summary>
        /// Replaces a boxed component value on an entity, if the value is a struct.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value. Must be a non-null value type; otherwise an
        /// exception is thrown or the call is ignored.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the component was replaced; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="boxed"/> is not a value type.
        /// </exception>
        internal bool ReplaceComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _replaceBoxedInvoker(t)(e, boxed);
        }

        /// <summary>
        /// Removes a component from an entity using a runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="componentType">Component value type.</param>
        /// <returns>
        /// <see langword="true"/> if a component of the given type was removed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal bool RemoveComponentTyped(Entity e, Type? componentType)
            => componentType is not null && _removeInvoker(componentType)(e);

        /// <summary>
        /// Dispatches a snapshot delta for a boxed component value on an entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="boxed">
        /// Boxed component value (used to derive the component type). Must be a
        /// non-null value type; otherwise the call is ignored or throws.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a snapshot delta was dispatched;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="boxed"/> is not a value type.
        /// </exception>
        public bool SnapshotComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _snapshotBoxedInvoker(t)(e);
        }

        /// <summary>
        /// Dispatches a snapshot delta for a component determined by runtime type.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="t">Component value type.</param>
        /// <returns>
        /// <see langword="true"/> if a snapshot delta was dispatched;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool SnapshotComponentTyped(Entity e, Type? t)
        {
            return t != null && _snapshotBoxedInvoker(t)(e);
        }

        // ── Invoker builders (cached) ───────────────────────────────────────

        /// <summary>
        /// Resolves or builds a cached <c>HasComponent</c> invoker for
        /// the specified component type.
        /// </summary>
        /// <param name="t">Component value type.</param>
        /// <returns>Invoker delegate that checks component presence on an entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _hasInvoker(Type t)
        {
            if (_hasCache.TryGetValue(t, out var fn)) return fn;

            _miHasOpen ??= typeof(World).GetMethod(
                nameof(HasComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // HasComponent<T>(Entity)

            var closed = _miHasOpen.MakeGenericMethod(t);

            var worldConst = Expression.Constant(this);
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var call = Expression.Call(worldConst, closed, eParam);
            var lambda = Expression.Lambda<Func<Entity, bool>>(call, eParam).Compile();

            _hasCache[t] = lambda;
            return lambda;
        }

        /// <summary>
        /// Resolves or builds a cached boxed AddComponent invoker for
        /// the specified component type.
        /// </summary>
        /// <param name="t">Component value type.</param>
        /// <returns>Invoker delegate that adds the boxed component to an entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, object, bool> _addBoxedInvoker(Type t)
        {
            if (_addBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miAddOpen ??= typeof(World).GetMethod(
                nameof(AddComponent),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!; // AddComponent<T>(Entity, in T)

            var closed = _miAddOpen.MakeGenericMethod(t);

            var worldConst = Expression.Constant(this);
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var objParam = Expression.Parameter(typeof(object), "value");
            var valueCast = Expression.Convert(objParam, t);
            var call = Expression.Call(worldConst, closed, eParam, valueCast);
            var lambda = Expression.Lambda<Func<Entity, object, bool>>(call, eParam, objParam).Compile();

            _addBoxedCache[t] = lambda;
            return lambda;
        }

        /// <summary>
        /// Resolves or builds a cached boxed SnapshotComponent invoker for
        /// the specified component type.
        /// </summary>
        /// <param name="t">Component value type.</param>
        /// <returns>Invoker delegate that dispatches a snapshot delta.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _snapshotBoxedInvoker(Type t)
        {
            if (_snapshotBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miSnapshotOpen ??= typeof(World).GetMethod(
                nameof(SnapshotComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // SnapshotComponent<T>(Entity)

            var closed = _miSnapshotOpen.MakeGenericMethod(t);

            var worldConst = Expression.Constant(this);
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var call = Expression.Call(worldConst, closed, eParam);
            var lambda = Expression.Lambda<Func<Entity, bool>>(call, eParam).Compile();

            _snapshotBoxedCache[t] = lambda;
            return lambda;
        }

        /// <summary>
        /// Resolves or builds a cached boxed ReplaceComponent invoker for
        /// the specified component type.
        /// </summary>
        /// <param name="t">Component value type.</param>
        /// <returns>Invoker delegate that replaces an existing component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, object, bool> _replaceBoxedInvoker(Type t)
        {
            if (_replaceBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miReplaceOpen ??= typeof(World).GetMethod(
                nameof(ReplaceComponent),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!; // ReplaceComponent<T>(Entity, in T)

            var closed = _miReplaceOpen.MakeGenericMethod(t);

            var worldConst = Expression.Constant(this);
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var objParam = Expression.Parameter(typeof(object), "value");
            var valueCast = Expression.Convert(objParam, t);
            var call = Expression.Call(worldConst, closed, eParam, valueCast);
            var lambda = Expression.Lambda<Func<Entity, object, bool>>(call, eParam, objParam).Compile();

            _replaceBoxedCache[t] = lambda;
            return lambda;
        }

        /// <summary>
        /// Resolves or builds a cached RemoveComponent invoker for
        /// the specified component type.
        /// </summary>
        /// <param name="t">Component value type.</param>
        /// <returns>Invoker delegate that removes the component from an entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _removeInvoker(Type t)
        {
            if (_removeCache.TryGetValue(t, out var fn)) return fn;

            _miRemoveOpen ??= typeof(World).GetMethod(
                nameof(RemoveComponent),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!; // RemoveComponent<T>(Entity)

            var closed = _miRemoveOpen.MakeGenericMethod(t);

            var worldConst = Expression.Constant(this);
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var call = Expression.Call(worldConst, closed, eParam);
            var lambda = Expression.Lambda<Func<Entity, bool>>(call, eParam).Compile();

            _removeCache[t] = lambda;
            return lambda;
        }

        /// <summary>
        /// Checks whether the entity currently has the component.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if the entity has a component of type
        /// <typeparamref name="T"/>; otherwise <see langword="false"/>.
        /// </returns>
        public bool HasComponent<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            return pool != null && pool.Has(e.Id);
        }

        /// <summary>
        /// Adds a component to an entity if absent, honoring permission and validation hooks.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">Component value to add.</param>
        /// <returns>
        /// <see langword="true"/> if the component was added; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal bool AddComponent<T>(Entity e, in T value) where T : struct
        {
            // 1) Phase-level structural write check
            if (!_writePolicy.CanStructuralWrite())
            {
                if (!HandleDenied(
                        $"[Denied] Add<{typeof(T).Name}> e={e.Id} " +
                        $"reason=Phase({_writePolicy.CurrentPhase})"))
                    return false;
            }

            if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return false;
            }

            bool valid = _permissionHook.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return false;
            }
            else if (!_permissionHook.ValidateObject(value!))
            {
                if (!HandleDenied(
                        $"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return false;
            }

            if (HasComponent<T>(e)) return false;

            ref var r = ref RefComponent<T>(e);
            r = value;

            addSingletonIndex<T>(e);
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Added, value));
            ComponentEvents.RaiseAdded(this, e, value);
            return true;
        }

        /// <summary>
        /// Dispatches a snapshot delta for an existing component without modifying its value.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if the entity has the component and a snapshot
        /// delta was dispatched; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Snapshot deltas are typically used by binders to pull the current value
        /// into the presentation layer. Conceptually this is the "Snapshot" kind
        /// of <see cref="ComponentDeltaKind"/>, i.e., the value is pushed as-is
        /// without treating it as a semantic "change".
        /// </para>
        /// </remarks>
        public bool SnapshotComponent<T>(Entity e) where T : struct
        {
            if (!HasComponent<T>(e)) return false;
            var r = ReadComponent<T>(e);
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Snapshot, r));
            return true;
        }

        /// <summary>
        /// Replaces a component value in-place and dispatches a “Changed” delta.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">New component value.</param>
        /// <returns>
        /// <see langword="true"/> if the component was replaced; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal bool ReplaceComponent<T>(Entity e, in T value) where T : struct
        {
            // 1) Phase-level value write check (allowed in presentation if configured)
            if (!_writePolicy.CanValueWrite())
            {
                if (!HandleDenied(
                        $"[Denied] Replace<{typeof(T).Name}> e={e.Id} " +
                        $"reason=Phase({ _writePolicy.CurrentPhase })"))
                    return false;
            }
            
            if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return false;
            }

            bool valid = _permissionHook.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return false;
            }
            else if (!_permissionHook.ValidateObject(value!))
            {
                if (!HandleDenied(
                        $"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return false;
            }

            ref var r = ref RefComponent<T>(e);
            r = value;
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Changed, value));
            return true;
        }

        /// <summary>
        /// Removes a component from an entity and dispatches a “Removed” delta.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// <see langword="true"/> if a component of type <typeparamref name="T"/>
        /// was removed; otherwise <see langword="false"/>.
        /// </returns>
        internal bool RemoveComponent<T>(Entity e) where T : struct
        {
            // 1) Phase-level structural write check
            if (!_writePolicy.CanStructuralWrite())
            {
                if (!HandleDenied(
                        $"[Denied] Remove<{typeof(T).Name}> e={e.Id} " +
                        $"reason=Phase({ _writePolicy.CurrentPhase })"))
                    return false;
            }
            
            if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Remove<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return false;
            }

            var pool = _componentPoolRepository.TryGetPool<T>();
            if (pool == null) return false;
            
            pool.Remove(e.Id);
            removeSingletonIndex<T>(e);
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Removed));
            ComponentEvents.RaiseRemoved<T>(this, e);
            return true;
        }

        /// <summary>
        /// Gets a <c>ref</c> to a component on an entity, creating storage if missing.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>A reference to the component storage for the entity.</returns>
        private ref T RefComponent<T>(Entity e) where T : struct
        {
            var pool = (ComponentPool<T>)_componentPoolRepository.GetPool<T>();
            return ref pool.Ref(e.Id);
        }

        /// <summary>
        /// Gets a <c>ref</c> to an existing component or throws if the component is absent.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>A reference to the existing component storage.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity does not have a component of the specified type.
        /// </exception>
        private ref T RefComponentExisting<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }

        /// <summary>
        /// Reads a component value by value (non-ref) from an entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <returns>The current component value.</returns>
        public T ReadComponent<T>(Entity e) where T : struct
        {
            return RefComponent<T>(e);
        }

        /// <summary>
        /// Tries to read a component by value (non-ref) from an entity.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Target entity.</param>
        /// <param name="value">
        /// When this method returns, contains the component value if present;
        /// otherwise the default value of <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the entity has the component; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryReadComponent<T>(Entity e, out T value) where T : struct
        {
            if (!HasComponent<T>(e))
            {
                value = default;
                return false;
            }

            value = ReadComponent<T>(e);
            return true;
        }

        /// <summary>
        /// Enumerates all components currently present on the entity as boxed values.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// A sequence of tuples where <c>type</c> is the component type and
        /// <c>boxed</c> is the boxed component value.
        /// </returns>
        public IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e)
        {
            foreach (var kv in _componentPoolRepository.ReadOnlyPools)
                if (kv.Value.Has(e.Id))
                    yield return (kv.Key, kv.Value.GetBoxed(e.Id));
        }

        /// <summary>
        /// Handles denied write operations according to the configured
        /// <see cref="EcsRuntimeOptions.WritePolicy"/>.
        /// </summary>
        /// <param name="reason">Diagnostic message describing why the write failed.</param>
        /// <returns>
        /// Always <see langword="false"/>; this method is intended to be used
        /// inside guard clauses.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleDenied(string reason)
        {
            switch (EcsRuntimeOptions.WritePolicy)
            {
                case EcsRuntimeOptions.WriteFailurePolicy.Throw:
                    throw new InvalidOperationException(reason);
                case EcsRuntimeOptions.WriteFailurePolicy.Log:
                    EcsRuntimeOptions.Log.Warn(reason);
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Ensures that only one entity in this world has component type <typeparamref name="T"/>.
        /// Also keeps the singleton index consistent with the current state.
        /// </summary>
        /// <typeparam name="T">Singleton component value type.</typeparam>
        /// <param name="has">
        /// When this method returns, <see langword="true"/> if a singleton entity
        /// exists; otherwise <see langword="false"/>.
        /// </param>
        /// <returns>
        /// The singleton entity if it exists; otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when more than one entity in the world has component type
        /// <typeparamref name="T"/>.
        /// </exception>
        private Entity? EnsureSingletonConsistency<T>(out bool has) where T : struct
        {
            var type = typeof(T);

            // 1) Quick path: if index contains entry, validate it.
            if (_singletonIndex.TryGetValue(type, out var indexed))
            {
                // Check if indexed entity actually has T.
                if (HasComponent<T>(indexed))
                {
                    has = true;
                    return indexed;
                }
                else
                {
                    // Entry invalid (entity no longer has T) → remove and full scan.
                    _singletonIndex.Remove(type);
                }
            }

            // 2) Full scan
            Entity? found = null;
            bool foundAny = false;

            foreach (var (e, _) in Query<T>())
            {
                if (!foundAny)
                {
                    found = e;
                    foundAny = true;
                }
                else
                {
                    // More than one → violation
                    throw new InvalidOperationException(
                        $"Singleton violation: multiple entities contain component {type.FullName}");
                }
            }

            if (!foundAny)
            {
                has = false;
                return null;
            }

            // Exactly one → register
            _singletonIndex[type] = found!.Value;
            has = true;
            return found;
        }

        /// <summary>
        /// Gets the singleton entity for component type <typeparamref name="T"/>.
        /// Throws if missing or if multiple entities contain the component.
        /// </summary>
        /// <typeparam name="T">Singleton component value type.</typeparam>
        /// <returns>The owning entity.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no singleton or multiple entities contain the component.
        /// </exception>
        internal Entity GetSingletonEntityInternal<T>() where T : struct
        {
            var e = EnsureSingletonConsistency<T>(out bool has);
            if (!has)
                throw new InvalidOperationException(
                    $"No singleton of type {typeof(T).FullName} exists in this world.");
            return e!.Value;
        }

        /// <summary>
        /// Tries to get the singleton entity for component type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Singleton component value type.</typeparam>
        /// <param name="entity">
        /// When this method returns, contains the owning entity if it exists;
        /// otherwise the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a singleton entity exists; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal bool TryGetSingletonEntityInternal<T>(out Entity entity) where T : struct
        {
            var e = EnsureSingletonConsistency<T>(out bool has);
            if (has)
            {
                entity = e!.Value;
                return true;
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Sets a singleton component of type <typeparamref name="T"/>.
        /// Creates or updates the singleton entity as needed.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <param name="value">Singleton component value.</param>
        internal void SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent
        {
            // Check if exists
            if (TryGetSingletonEntityInternal<T>(out var e))
            {
                ReplaceComponent(e, value);
                _singletonIndex[typeof(T)] = e;
                return;
            }

            // Create new
            var newEntity = CreateEntity();
            AddComponent(newEntity, value);
            _singletonIndex[typeof(T)] = newEntity;
        }

        /// <summary>
        /// Gets the singleton component value for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <returns>The singleton component value.</returns>
        public T GetSingleton<T>() where T : struct, IWorldSingletonComponent
        {
            var e = GetSingletonEntityInternal<T>();
            return ReadComponent<T>(e);
        }

        /// <summary>
        /// Removes the singleton component of type <typeparamref name="T"/>, if present.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <returns>
        /// <see langword="true"/> if a singleton existed and was removed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal bool RemoveSingleton<T>() where T : struct, IWorldSingletonComponent
        {
            if (TryGetSingletonEntityInternal<T>(out var e))
            {
                RemoveComponent<T>(e);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the singleton component value for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Component value type implementing <see cref="IWorldSingletonComponent"/>.
        /// </typeparam>
        /// <param name="value">
        /// When this method returns, contains the singleton value if present;
        /// otherwise the default value for <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a singleton exists; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetSingleton<T>(out T value) where T : struct, IWorldSingletonComponent
        {
            if (TryGetSingletonEntityInternal<T>(out var e))
            {
                value = ReadComponent<T>(e);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Clears entries from the singleton index for the specified entity.
        /// </summary>
        /// <param name="e">Entity that is being removed or reset.</param>
        private void clearSingletonIndex(Entity e)
        {
            List<Type>? toRemove = null;

            // Remove from singleton index if owning any singleton components
            foreach (var kv in _singletonIndex)
            {
                if (kv.Value.Id == e.Id)
                {
                    toRemove ??= new List<Type>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var t in toRemove)
                    _singletonIndex.Remove(t);
            }
        }

        /// <summary>
        /// Updates the singleton index after a successful <c>AddComponent&lt;T&gt;</c>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Entity that received the component.</param>
        private void addSingletonIndex<T>(Entity e) where T : struct
        {
            // After successful AddComponent<T>(e, value)
            // Insert singleton enforcement:
            if (typeof(IWorldSingletonComponent).IsAssignableFrom(typeof(T)))
            {
                // Check if another entity already has T
                foreach (var (other, _) in Query<T>())
                {
                    if (other.Id != e.Id)
                    {
                        throw new InvalidOperationException(
                            $"Singleton violation: component {typeof(T).FullName} " +
                            $"is marked as IWorldSingletonComponent but added to multiple entities.");
                    }
                }

                // If OK, update singleton index
                _singletonIndex[typeof(T)] = e;
            }
        }

        /// <summary>
        /// Updates the singleton index after a successful <c>RemoveComponent&lt;T&gt;</c>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="e">Entity that lost the component.</param>
        private void removeSingletonIndex<T>(Entity e) where T : struct
        {
            // If removing singleton type → remove from index
            if (_singletonIndex.TryGetValue(typeof(T), out var owner) && owner.Id == e.Id)
            {
                _singletonIndex.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Checks whether the specified entity owns any singleton component.
        /// </summary>
        /// <param name="e">Entity to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the entity is the owner of at least one
        /// singleton component; otherwise <see langword="false"/>.
        /// </returns>
        public bool HasSingleton(Entity e)
        {
            return _singletonIndex.Select(keyValuePair => keyValuePair.Value).Any(se => se.Equals(e));
        }

        /// <summary>
        /// Returns all singleton components currently registered in this world.
        /// </summary>
        /// <returns>
        /// A sequence of tuples containing the component type and the owning entity.
        /// </returns>
        public IEnumerable<(Type type, Entity owner)> GetAllSingletons()
        {
            foreach (var kv in _singletonIndex)
                yield return (kv.Key, kv.Value);
        }

        /// <summary>
        /// Sets a singleton component using runtime types and boxed values.
        /// </summary>
        /// <param name="singletonType">Component value type marked as singleton.</param>
        /// <param name="boxed">Boxed component value.</param>
        internal void SetSingletonTyped(Type? singletonType, object? boxed)
        {
            if (singletonType == null) return;
 
            if (_singletonIndex.TryGetValue(singletonType, out var owner))
            {
                ReplaceComponentBoxed(owner, boxed);
                _singletonIndex[singletonType] = owner;
                return;
            }
            
            // Create new
            var newEntity = CreateEntity();
            AddComponentBoxed(newEntity, boxed);
            _singletonIndex[singletonType] = newEntity;
        }

        /// <summary>
        /// Removes a singleton component using a runtime component type.
        /// </summary>
        /// <param name="singletonType">Component value type to remove from singleton index.</param>
        internal void RemoveSingletonTyped(Type? singletonType)
        {
            if (singletonType == null) return;
            if (_singletonIndex.TryGetValue(singletonType, out var owner))
            {
                RemoveComponentTyped(owner, singletonType);
            }
        }
    }
}
