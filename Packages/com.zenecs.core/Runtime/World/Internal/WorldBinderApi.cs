// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem (Binder API)
// File: WorldBinderApi.cs
// Purpose: Adapter-facing binder attachment surface for a World.
// Key concepts:
//   • Decoupled view/binding layer: attach/detach binders per entity.
//   • Router-based dispatch: World delegates binder operations to IBindingRouter.
//   • Safety on despawn: World tears down binders when entities are destroyed.
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Binding;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Implements <see cref="IWorldBinderApi"/> for attaching/detaching view binders to entities.
    /// </summary>
    internal sealed partial class World : IWorldBinderApi
    {
        /// <summary>
        /// Attach a binder to an entity and optionally enforce strict context/permission checks.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">Binder instance to attach.</param>
        /// <param name="options">Attachment policy (strict/relaxed).</param>
        public void AttachBinder(Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict)
        {
            _bindingRouter.Attach(this, e, binder, options);
        }

        /// <summary>
        /// Detach all binders currently attached to the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        public void DetachAllBinders(Entity e)
        {
            _bindingRouter.DetachAll(e);
        }

        /// <summary>
        /// Detach a specific binder instance from the given entity.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="binder">Binder to remove.</param>
        public void DetachBinder(Entity e, IBinder binder)
        {
            _bindingRouter.Detach(e, binder);
        }
    }
}
