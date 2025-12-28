// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — System Presets
// File: ISystemPresetResolver.cs
// Purpose: Abstraction for creating concrete ISystem instances from a list of
//          system types, optionally using DI containers or manual factories.
// Key concepts:
//   • System instantiation: turns Type descriptors into live ISystem instances.
//   • Pluggable backend: DI (e.g., Zenject) or Activator.CreateInstance.
//   • Validation point: filter out duplicates or invalid/abstract types here.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.SystemPresets
{
    /// <summary>
    /// Creates concrete <see cref="ISystem"/> instances from a list of system types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ISystemPresetResolver"/> is a pluggable abstraction used by
    /// higher-level installers (for example, <c>WorldSystemInstaller</c>) to
    /// turn a set of <see cref="Type"/> descriptors into live
    /// <see cref="ISystem"/> instances.
    /// </para>
    /// <para>
    /// Typical implementations include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// A dependency injection container (such as Zenject) that resolves
    /// <see cref="ISystem"/> instances from bindings.
    /// </description></item>
    /// <item><description>
    /// A simple factory that uses <see cref="Activator.CreateInstance(Type)"/>
    /// to construct each system.
    /// </description></item>
    /// </list>
    /// <para>
    /// The interface defines a single method,
    /// <see cref="InstantiateSystems"/>, which is responsible for validating
    /// the type list (for example, removing non-<see cref="ISystem"/> or
    /// abstract types) and constructing the final instance list.
    /// </para>
    /// </remarks>
    public interface ISystemPresetResolver
    {
        /// <summary>
        /// Instantiates concrete <see cref="ISystem"/> instances from the given
        /// list of system types.
        /// </summary>
        /// <param name="types">
        /// List of candidate types to instantiate. Implementations may assume
        /// that the caller attempted to filter out obvious invalid types, but
        /// should still perform validation as needed.
        /// </param>
        /// <returns>
        /// A list of successfully created <see cref="ISystem"/> instances.
        /// Implementations are free to drop types that are invalid, abstract,
        /// or already represented in the result.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method acts as both a construction and a validation step:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Non-<see cref="ISystem"/> types should be ignored.
        /// </description></item>
        /// <item><description>
        /// Abstract or open-generic types should not be instantiated.
        /// </description></item>
        /// <item><description>
        /// Duplicate types may be filtered out according to the resolver's
        /// policy.
        /// </description></item>
        /// </list>
        /// </remarks>
        List<ISystem> InstantiateSystems(List<Type> types);
    }
}
