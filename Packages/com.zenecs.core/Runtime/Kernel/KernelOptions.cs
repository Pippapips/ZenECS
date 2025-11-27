// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: KernelOptions.cs
// Purpose: Options object that controls Kernel behavior and defaults for
//          world creation and stepping policies.
// Key concepts:
//   • ID factory: plug your own WorldId generator (e.g., deterministic for tests).
//   • Auto naming: prefix used when a world is created without an explicit name.
//   • Stepping policy: optionally step only the selected (current) world.
//   • Selection policy: optionally auto-select a newly created world.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Configures <see cref="IKernel"/> behavior for world creation and frame stepping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is intended to be stable; adapters and host applications should
    /// be able to rely on it as a durable configuration surface.
    /// </para>
    /// <para>Typical usage:</para>
    /// <code language="csharp"><![CDATA[
    /// var options = new KernelOptions
    /// {
    ///     AutoNamePrefix = "Game-",
    ///     AutoSelectNewWorld = true,
    ///     StepOnlyCurrentWhenSelected = true,
    ///     NewWorldIdFactory = () => new WorldId(MyDeterministicGuidSource.Next())
    /// };
    /// using var kernel = new Kernel(options);
    /// var world = kernel.CreateWorld(name: "Main");
    /// // Game loop:
    /// kernel.BeginFrame(dt);
    /// kernel.FixedStep(fixedDelta);
    /// kernel.LateFrame(alpha);
    /// ]]></code>
    /// </remarks>
    public sealed class KernelOptions
    {
        /// <summary>
        /// Gets or sets the factory used to generate a new <see cref="WorldId"/>
        /// whenever a world is created without an explicit id.
        /// </summary>
        /// <value>
        /// Defaults to a GUID-based id:
        /// <c>() =&gt; new WorldId(Guid.NewGuid())</c>.
        /// </value>
        /// <remarks>
        /// Override this for deterministic ids in tests or to integrate with an
        /// external id service.
        /// </remarks>
        public Func<WorldId> NewWorldIdFactory { get; set; } = () => new WorldId(Guid.NewGuid());

        /// <summary>
        /// Gets or sets the prefix used when auto-naming worlds that omit an explicit name.
        /// </summary>
        /// <value>Default: <c>"World-"</c>.</value>
        /// <remarks>
        /// The final auto name is typically constructed as
        /// <c>{AutoNamePrefix}{shortId}</c>, where <c>shortId</c> is a shortened
        /// representation of the generated <see cref="WorldId"/>.
        /// </remarks>
        public string AutoNamePrefix { get; set; } = "World-";

        /// <summary>
        /// Gets or sets a value indicating whether the kernel should step only the
        /// <em>current</em> world (if one is selected) or all worlds.
        /// </summary>
        /// <value>Default: <c>false</c> (step all worlds).</value>
        /// <remarks>
        /// When set to <see langword="true"/>, calls to
        /// <see cref="IKernel.BeginFrame(float)"/>,
        /// <see cref="IKernel.FixedStep(float)"/>, and
        /// <see cref="IKernel.LateFrame(float)"/> will drive only the
        /// <see cref="IKernel.CurrentWorld"/> (if non-null).
        /// </remarks>
        public bool StepOnlyCurrentWhenSelected { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the kernel may automatically set
        /// newly created worlds as the <em>current</em> world.
        /// </summary>
        /// <value>Default: <c>false</c>.</value>
        /// <remarks>
        /// This is a global default only. Callers can override the behavior per call
        /// using the <c>setAsCurrent</c> parameter of
        /// <see cref="IKernel.CreateWorld(WorldConfig?, string?, System.Collections.Generic.IEnumerable{string}?, WorldId?, bool)"/>.
        /// </remarks>
        public bool AutoSelectNewWorld { get; set; } = false;

        /// <summary>
        /// Generates a new <see cref="WorldId"/> by invoking <see cref="NewWorldIdFactory"/>.
        /// </summary>
        /// <returns>A freshly created world identifier.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var options = new KernelOptions();
        /// var id = options.NewWorldId(); // uses the configured factory
        /// ]]></code>
        /// </example>
        public WorldId NewWorldId() => NewWorldIdFactory();
    }
}