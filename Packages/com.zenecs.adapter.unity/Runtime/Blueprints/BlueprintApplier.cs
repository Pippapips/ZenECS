// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity — Blueprints Utility
// File: BlueprintApplier.cs
// Purpose: Fast application of boxed component instances to a world/API by compiling
//          and caching delegates for ReplaceComponent<T>(Entity, in T).
// Key concepts:
//   • One-time reflection per component/world → cached Expression delegates.
//   • Multi-world safety: delegates take the world/API as an argument (no captures).
//   • ByRef support: correctly passes variables for `in T`/`ref T` parameters.
//   • AOT/IL2CPP fallback: uses MethodInfo.Invoke when Expression.Compile is unavailable.
//   • Thread-safe caches (ConcurrentDictionary) for hot paths.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// Provides a fast and type-safe runtime mechanism to apply boxed component instances
    /// to an <see cref="IWorld"/> or its <see cref="IWorldComponentApi"/>. This utility
    /// dynamically compiles delegates for <c>ReplaceComponent&lt;T&gt;(Entity, in T)</c>
    /// and caches them for reuse across worlds and component types.
    /// </summary>
    public static class BlueprintApplier
    {
        /// <summary>
        /// Internal key used for caching compiled delegates based on (WorldType, ComponentType).
        /// </summary>
        private readonly struct Key : IEquatable<Key>
        {
            public readonly Type WorldType;
            public readonly Type CompType;

            public Key(Type worldType, Type compType)
            {
                WorldType = worldType;
                CompType  = compType;
            }

            public bool Equals(Key other) => WorldType == other.WorldType && CompType == other.CompType;
            public override bool Equals(object obj) => obj is Key k && Equals(k);
            public override int GetHashCode() => HashCode.Combine(WorldType, CompType);
        }

        /// <summary>
        /// Cache of compiled invoker delegates for (WorldType, ComponentType).
        /// </summary>
        private static readonly ConcurrentDictionary<Key, Action<IWorld, Entity, object>> _cache = new();

        /// <summary>
        /// Cache of accessors that retrieve <see cref="IWorldComponentApi"/> from a specific <see cref="IWorld"/> type.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<IWorld, IWorldComponentApi>> _apiAccessors = new();

        /// <summary>
        /// Applies a boxed component instance to the specified <see cref="IWorldComponentApi"/>.
        /// This overload is the fastest and most direct option.
        /// </summary>
        /// <param name="api">The <see cref="IWorldComponentApi"/> to modify.</param>
        /// <param name="e">Target entity to replace or add the component to.</param>
        /// <param name="boxed">The boxed component instance (non-null).</param>
        public static void AddBoxed(IWorldComponentApi api, Entity e, object boxed)
        {
            if (boxed == null) return;
            var t   = boxed.GetType();
            var inv = GetInvokerForApi(t);
            inv(api, e, boxed);
        }

        /// <summary>
        /// Applies a boxed component instance to a world, automatically resolving
        /// the internal <see cref="IWorldComponentApi"/> implementation.
        /// </summary>
        /// <param name="w">The target world instance.</param>
        /// <param name="e">Target entity to replace or add the component to.</param>
        /// <param name="boxed">The boxed component instance (non-null).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="w"/> is null.</exception>
        public static void AddBoxed(IWorld w, Entity e, object boxed)
        {
            if (w == null) throw new ArgumentNullException(nameof(w));
            if (boxed == null) return;

            var worldType = w.GetType();
            var compType  = boxed.GetType();

            var key = new Key(worldType, compType);
            var act = _cache.GetOrAdd(key, _ =>
            {
                var apiGetter  = _apiAccessors.GetOrAdd(worldType, BuildApiAccessor);
                var apiInvoker = GetInvokerForApi(compType);

                // (IWorld w, Entity e, object o) => apiInvoker(apiGetter(w), e, o)
                var pW = Expression.Parameter(typeof(IWorld), "w");
                var pE = Expression.Parameter(typeof(Entity), "e");
                var pO = Expression.Parameter(typeof(object), "boxed");

                var getApiCall = Expression.Invoke(Expression.Constant(apiGetter), pW);
                var call       = Expression.Invoke(Expression.Constant(apiInvoker), getApiCall, pE, pO);
                return Expression.Lambda<Action<IWorld, Entity, object>>(call, pW, pE, pO).Compile();
            });

            act(w, e, boxed);
        }

        /// <summary>
        /// Builds an accessor delegate that extracts the <see cref="IWorldComponentApi"/> instance
        /// from a specific world type. Supports direct implementation, property, or field lookup.
        /// </summary>
        /// <param name="worldType">The concrete world type.</param>
        /// <returns>A compiled delegate that returns <see cref="IWorldComponentApi"/> for a given world instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no <see cref="IWorldComponentApi"/> implementation or field/property can be located.
        /// </exception>
        private static Func<IWorld, IWorldComponentApi> BuildApiAccessor(Type worldType)
        {
            var iface = typeof(IWorldComponentApi);

            // 1) World directly implements IWorldComponentApi
            if (iface.IsAssignableFrom(worldType))
            {
                var pW   = Expression.Parameter(typeof(IWorld), "w");
                var cast = Expression.Convert(pW, iface);
                return Expression.Lambda<Func<IWorld, IWorldComponentApi>>(cast, pW).Compile();
            }

            // 2) IWorldComponentApi stored as a property
            var candidateProp = worldType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(p => iface.IsAssignableFrom(p.PropertyType) && p.GetMethod != null);

            if (candidateProp != null)
            {
                var pW    = Expression.Parameter(typeof(IWorld), "w");
                var wCast = Expression.Convert(pW, worldType);
                var get   = Expression.Call(wCast, candidateProp.GetMethod!);
                var cast  = Expression.Convert(get, iface);
                return Expression.Lambda<Func<IWorld, IWorldComponentApi>>(cast, pW).Compile();
            }

            // 3) IWorldComponentApi stored as a field
            var candidateField = worldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(f => iface.IsAssignableFrom(f.FieldType));

            if (candidateField != null)
            {
                var pW    = Expression.Parameter(typeof(IWorld), "w");
                var wCast = Expression.Convert(pW, worldType);
                var fld   = Expression.Field(wCast, candidateField);
                var cast  = Expression.Convert(fld, iface);
                return Expression.Lambda<Func<IWorld, IWorldComponentApi>>(cast, pW).Compile();
            }

            throw new InvalidOperationException(
                $"Unable to locate an IWorldComponentApi reference within '{worldType.Name}'. " +
                "Ensure the world implements IWorldComponentApi or exposes it through a property or field " +
                "(e.g., public IWorldComponentApi Components { get; }).");
        }

        /// <summary>
        /// Builds and caches a strongly-typed invoker delegate that calls
        /// <c>IWorldComponentApi.ReplaceComponent&lt;T&gt;(Entity, in T)</c>.
        /// </summary>
        /// <param name="compType">The concrete component type <typeparamref name="T"/>.</param>
        /// <returns>A compiled invoker delegate: <c>(api, e, boxed) =&gt; api.ReplaceComponent&lt;T&gt;(e, (T)boxed)</c>.</returns>
        /// <exception cref="MissingMethodException">
        /// Thrown if the method <c>ReplaceComponent&lt;T&gt;(Entity, in T)</c> cannot be found on <see cref="IWorldComponentApi"/>.
        /// </exception>
        private static Action<IWorldComponentApi, Entity, object> GetInvokerForApi(Type compType)
        {
            var iface = typeof(IWorldComponentApi);

            var open = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi =>
                    mi.IsGenericMethodDefinition &&
                    mi.Name == "ReplaceComponent" &&
                    mi.GetParameters().Length == 2 &&
                    mi.GetParameters()[0].ParameterType == typeof(Entity));

            if (open == null)
                throw new MissingMethodException("IWorldComponentApi.ReplaceComponent<T>(Entity, in T) not found.");

            var closed = open.MakeGenericMethod(compType);

            var pApi = Expression.Parameter(typeof(IWorldComponentApi), "api");
            var pE   = Expression.Parameter(typeof(Entity), "e");
            var pObj = Expression.Parameter(typeof(object), "boxed");

            // Unbox the object to T
            var vComp  = Expression.Variable(compType, "v");
            var assign = Expression.Assign(vComp, Expression.Convert(pObj, compType));

            // Handle ByRef or in T parameters (never Convert to a ByRef type)
            var p2type = closed.GetParameters()[1].ParameterType;
            Expression arg2;
            if (p2type.IsByRef)
            {
                arg2 = vComp; // pass variable for ByRef
            }
            else if (p2type != compType)
            {
                arg2 = Expression.Convert(vComp, p2type);
            }
            else
            {
                arg2 = vComp;
            }

            var call = Expression.Call(pApi, closed, pE, arg2);
            var body = Expression.Block(new[] { vComp }, assign, call);

            try
            {
                return Expression.Lambda<Action<IWorldComponentApi, Entity, object>>(body, pApi, pE, pObj).Compile();
            }
            catch
            {
                // AOT/IL2CPP fallback using reflection (ByRef automatically handled)
                return (api, e, obj) =>
                {
                    var args = new object?[] { e, obj };
                    closed.Invoke(api, args);
                };
            }
        }
    }
}
