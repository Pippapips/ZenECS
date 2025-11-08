// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IRequireContext.cs
// Purpose: Declarative contract stating that a binder requires context T.
// Key concepts:
//   • Validated: router/inspector prevents attachment when required contexts are missing.
//   • Self-documenting: binder dependencies are explicit via generic markers.
//   • Composition: multiple IRequireContext<T> can be implemented for multi-deps.
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Declarative marker indicating that a binder <b>requires</b> a context <typeparamref name="T"/>
    /// to be registered on the same entity before attachment (validated by Router &amp; Inspector).
    /// </summary>
    /// <typeparam name="T">Required context type.</typeparam>
    public interface IRequireContext<T> where T : class, IContext { }
}