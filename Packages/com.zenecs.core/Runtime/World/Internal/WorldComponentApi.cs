// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Component API)
// File: WorldComponentApi.cs
// Purpose: Typed component add/replace/remove and ref accessors.
// Key concepts:
//   • Permission & validation hooks: read/write gates + typed/object validators.
//   • Router deltas: binder layer notified on add/change/remove.
//   • Pools repository: fast ref access and presence checks per component type.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ZenECS.Core.Abstractions.Config;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.ComponentPooling;

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
        /// Ensures each singleton component type T has exactly one entity owner.
        /// </summary>
        private readonly Dictionary<Type, Entity> _singletonIndex = new();

        // ── Boxed / non-generic implementations ─────────────────────────────        

        /// <inheritdoc/>
        public bool HasComponentBoxed(Entity e, Type? componentType)
        {
            return componentType is not null && _hasInvoker(componentType)(e);
        }

        /// <inheritdoc/>
        public bool AddComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _addBoxedInvoker(t)(e, boxed);
        }

        /// <inheritdoc/>
        public bool ReplaceComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _replaceBoxedInvoker(t)(e, boxed);
        }

        public bool SnapshotComponentBoxed(Entity e, object? boxed)
        {
            if (boxed is null) return false;
            var t = boxed.GetType();
            if (!t.IsValueType)
                throw new ArgumentException("Boxed component must be a value type (struct).", nameof(boxed));

            return _snapshotBoxedInvoker(t)(e);
        }
        
        public bool SnapshotComponentTyped(Entity e, Type? t)
        {
            return t != null && _snapshotBoxedInvoker(t)(e);
        }

        /// <inheritdoc/>
        public bool RemoveComponentBoxed(Entity e, Type? componentType)
            => componentType is not null && _removeInvoker(componentType)(e);

        // ── Invoker builders (cached) ───────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _hasInvoker(Type t)
        {
            if (_hasCache.TryGetValue(t, out var fn)) return fn;

            _miHasOpen ??= typeof(World).GetMethod(
                nameof(HasComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // HasComponent<T>(Entity)

            var closed = _miHasOpen.MakeGenericMethod(t);
            bool Wrapped(Entity e) => (bool)closed.Invoke(this, new object[] { e })!;

            _hasCache[t] = Wrapped;
            return Wrapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, object, bool> _addBoxedInvoker(Type t)
        {
            if (_addBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miAddOpen ??= typeof(World).GetMethod(
                nameof(AddComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // AddComponent<T>(Entity, in T)

            var closed = _miAddOpen.MakeGenericMethod(t);
            bool Wrapped(Entity e, object value) => (bool)closed.Invoke(this, new object[] { e, value })!;

            _addBoxedCache[t] = Wrapped;
            return Wrapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _snapshotBoxedInvoker(Type t)
        {
            if (_snapshotBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miSnapshotOpen ??= typeof(World).GetMethod(
                nameof(SnapshotComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // SnapshotComponent<T>(Entity)

            var closed = _miSnapshotOpen.MakeGenericMethod(t);
            bool Wrapped(Entity e) => (bool)closed.Invoke(this, new object[] { e })!;

            _snapshotBoxedCache[t] = Wrapped;
            return Wrapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, object, bool> _replaceBoxedInvoker(Type t)
        {
            if (_replaceBoxedCache.TryGetValue(t, out var fn)) return fn;

            _miReplaceOpen ??= typeof(World).GetMethod(
                nameof(ReplaceComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // ReplaceComponent<T>(Entity, in T)

            var closed = _miReplaceOpen.MakeGenericMethod(t);
            bool Wrapped(Entity e, object value) => (bool)closed.Invoke(this, new object[] { e, value })!;

            _replaceBoxedCache[t] = Wrapped;
            return Wrapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Entity, bool> _removeInvoker(Type t)
        {
            if (_removeCache.TryGetValue(t, out var fn)) return fn;

            _miRemoveOpen ??= typeof(World).GetMethod(
                nameof(RemoveComponent),
                BindingFlags.Instance | BindingFlags.Public)!; // RemoveComponent<T>(Entity)

            var closed = _miRemoveOpen.MakeGenericMethod(t);
            bool Wrapped(Entity e) => (bool)closed.Invoke(this, new object[] { e })!;

            _removeCache[t] = Wrapped;
            return Wrapped;
        }

        /// <summary>
        /// Check if the entity currently has the component.
        /// </summary>
        public bool HasComponent<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            return pool != null && pool.Has(e.Id);
        }

        /// <summary>
        /// Add a component to an entity if absent, honoring permission/validation hooks.
        /// </summary>
        public bool AddComponent<T>(Entity e, in T value) where T : struct
        {
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
            return true;
        }

        /// <summary>
        /// Snapshot a component value in-place and dispatch a “Changed” delta.
        /// </summary>
        public bool SnapshotComponent<T>(Entity e) where T : struct
        {
            if (!HasComponent<T>(e)) return false;
            var r = ReadComponent<T>(e);
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Snapshot, r));
            return true;
        }

        /// <summary>
        /// Replace a component value in-place and dispatch a “Changed” delta.
        /// </summary>
        public bool ReplaceComponent<T>(Entity e, in T value) where T : struct
        {
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
        /// Remove a component from an entity and dispatch a “Removed” delta.
        /// </summary>
        public bool RemoveComponent<T>(Entity e) where T : struct
        {
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
            return true;
        }

        /// <summary>
        /// Get a <c>ref</c> to a component on an entity (creates storage if missing).
        /// </summary>
        private ref T RefComponent<T>(Entity e) where T : struct
        {
            var pool = (ComponentPool<T>)_componentPoolRepository.GetPool<T>();
            return ref pool.Ref(e.Id);
        }

        /// <summary>
        /// Get a <c>ref</c> to an existing component; throws if the component is absent.
        /// </summary>
        private ref T RefComponentExisting<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }

        /// <summary>
        /// Read component by ref (alias of <see cref="RefComponent{T}(Entity)"/>).
        /// </summary>
        public ref T ReadComponent<T>(Entity e) where T : struct
        {
            return ref RefComponent<T>(e);
        }

        /// <summary>
        /// Try to read a component by value (non-ref); returns <see langword="false"/> if absent.
        /// </summary>
        public bool TryRead<T>(Entity e, out T value) where T : struct
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
        /// Enumerate all components currently present on the entity (boxed values).
        /// </summary>
        public IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e)
        {
            foreach (var kv in _componentPoolRepository.Pools)
                if (kv.Value.Has(e.Id))
                    yield return (kv.Key, kv.Value.GetBoxed(e.Id));
        }

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
        /// Ensure only one entity in this world has component type T.
        /// Throws if more than one exists.
        /// This method also updates the _singletonIndex accordingly.
        /// </summary>
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
        /// Get singleton entity for T. Throws if missing or multiple.
        /// </summary>
        internal Entity GetSingletonEntityInternal<T>() where T : struct
        {
            var e = EnsureSingletonConsistency<T>(out bool has);
            if (!has)
                throw new InvalidOperationException(
                    $"No singleton of type {typeof(T).FullName} exists in this world.");
            return e!.Value;
        }

        /// <summary>
        /// Try get singleton entity for T.
        /// No creation performed.
        /// Returns false if missing.
        /// </summary>
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
        
        public void SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent
        {
            // Check if exists
            if (TryGetSingletonEntityInternal<T>(out var e))
            {
                ReplaceComponent(e, value);
                _singletonIndex[typeof(T)] = e;
                return;
            }

            // Create new
            var newEntity = SpawnEntity();
            AddComponent(newEntity, value);
            _singletonIndex[typeof(T)] = newEntity;
        }

        public T GetSingleton<T>() where T : struct, IWorldSingletonComponent
        {
            var e = GetSingletonEntityInternal<T>();
            return ReadComponent<T>(e);
        }

        public bool RemoveSingleton<T>(in T value) where T : struct, IWorldSingletonComponent
        {
            if (TryGetSingletonEntityInternal<T>(out var e))
            {
                RemoveComponent<T>(e);
                return true;
            }

            return false;
        }

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

        private void removeSingletonIndex<T>(Entity e) where T : struct
        {
            // If removing singleton type → remove from index
            if (_singletonIndex.TryGetValue(typeof(T), out var owner) && owner.Id == e.Id)
            {
                _singletonIndex.Remove(typeof(T));
            }
        }

        public bool HasSingleton(Entity e)
        {
            return _singletonIndex.Select(keyValuePair => keyValuePair.Value).Any(se => se.Equals(e));
        }
    }
}