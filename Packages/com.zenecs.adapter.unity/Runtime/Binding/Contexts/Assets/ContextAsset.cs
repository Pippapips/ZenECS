// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Binding
// File: ContextAsset.cs
// Purpose: Base ScriptableObject for Unity-side context configuration assets
//          that participate in ZenECS binding.
// Key concepts:
//   • Editor configuration: stores references to prefabs, canvases, etc.
//   • Runtime factory base: specialized assets create concrete IContext types.
//   • Extended by shared- and per-entity context asset variants.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    /// <summary>
    /// Base asset for Unity-side context configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ContextAsset"/> is the root ScriptableObject type used by
    /// ZenECS binding to describe how Unity-facing contexts are created and
    /// wired. Concrete subclasses do not usually contain logic by themselves,
    /// but instead act as configuration containers and factory descriptors.
    /// </para>
    /// <para>
    /// Typical specializations include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Shared context assets</b> that represent a single global context
    /// instance reused by many entities or views.
    /// </description></item>
    /// <item><description>
    /// <b>Per-entity context assets</b> that create a dedicated context
    /// instance for each entity that needs it.
    /// </description></item>
    /// </list>
    /// <para>
    /// At runtime, higher-level binding code or systems are responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Interpreting the configuration stored in the asset.</description></item>
    /// <item><description>Instantiating the appropriate context objects.</description></item>
    /// <item><description>Managing their lifetime alongside ECS entities or worlds.</description></item>
    /// </list>
    /// </remarks>
    public abstract class ContextAsset : ScriptableObject
    {
    }
}
