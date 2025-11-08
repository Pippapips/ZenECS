// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World • Binder API
// File: IWorldBinderApi.cs
// Purpose: Attach/detach view binders to entities and control attach policy.
// Key concepts:
//   • Decoupled view: binders consume contexts and deltas; world hosts lifecycle.
//   • Policy: strict mode throws when required contexts are missing; warn/skip.
//   • Safety: binders are detached on entity despawn/reset.
// License: MIT — Copyright (c) 2025 Pippapips Limited
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    /// <summary>Attach policy for binders.</summary>
    public enum AttachOptions
    {
        /// <summary>Default policy (alias of <see cref="Strict"/>).</summary>
        Default = 0,

        /// <summary>Throw if required context(s) are missing.</summary>
        Strict  = 1,

        /// <summary>Log a warning and skip the attach when requirements are missing.</summary>
        WarnOnly = 2
    }

    /// <summary>
    /// Adapter-facing binder surface: attach/detach view binders to entities.
    /// </summary>
    public interface IWorldBinderApi
    {
        /// <summary>
        /// Attach a binder to an entity using the given policy.
        /// </summary>
        void AttachBinder(Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict);

        /// <summary>Detach all binders from an entity.</summary>
        void DetachAllBinders(Entity e);

        /// <summary>Detach a specific binder instance from an entity.</summary>
        void DetachBinder(Entity e, IBinder binder);
    }
}