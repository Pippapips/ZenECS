// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Binding
// File: IBindingRouter.cs
// Purpose: Router interface for binder attach/detach, delta dispatch, and frame apply.
// Key concepts:
//   • Ordered per-entity binder list (Priority + attach order).
//   • Type-routed deltas → IBind<T>.
//   • Single frame barrier: ApplyAll() before Presentation.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal.Binding
{
    /// <summary>
    /// Router that owns binder lists per entity, validates requirements,
    /// fans out deltas, and applies binders each frame.
    /// </summary>
    internal interface IBindingRouter
    {
        /// <summary>Attach a binder to the entity.</summary>
        void Attach(IWorld w, Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict);

        /// <summary>Detach a specific binder from the entity.</summary>
        void Detach(Entity e, IBinder binder);

        /// <summary>Detach a specific binder from the entity.</summary>
        bool Detach(Entity e, Type binderType);

        /// <summary>Detach all binders from the entity.</summary>
        void DetachAll(Entity e);

        /// <summary>Notify the router that the entity was destroyed (auto-detach all).</summary>
        void OnEntityDestroyed(IWorld w, Entity e);

        /// <summary>Invoke <see cref="IBinder.Apply"/> for all binders in all entities.</summary>
        void ApplyAll(IWorld w);

        /// <summary>
        /// Dispatch a component delta to binders attached to the target entity that
        /// implement <see cref="IBind{T}"/>.
        /// </summary>
        void Dispatch<T>(in ComponentDelta<T> d) where T : struct;

        (Type type, object boxed)[] GetAllBinders(Entity e);

        IReadOnlyList<IBinder>? GetAllBinderList(Entity e);
    }
}