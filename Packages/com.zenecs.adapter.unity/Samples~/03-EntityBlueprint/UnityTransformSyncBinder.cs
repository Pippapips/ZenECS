// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 03 - EntityBlueprint
// File: UnityTransformSyncBinder.cs
// Purpose: Example Binder that synchronizes Unity Transform with ECS Position/Rotation components
// Key concepts:
//   • Binder implementation inheriting from BaseBinder
//   • Component delta reception via IBind<T>
//   • State tracking using DeltaTracker
//   • Unity GameObject reference via Context
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    /// <summary>
    /// Binder that synchronizes Unity Transform with ECS Position and Rotation components.
    /// </summary>
    [Serializable]
    public sealed class UnityTransformSyncBinder :
        BaseBinder,
        IBind<Position>,
        IBind<Rotation>
    {
        private DeltaTracker<Position> _pos;
        private DeltaTracker<Rotation> _rot;
        private UnityTransformContext? _unityTransformContext;

        /// <inheritdoc />
        protected override void OnBind(Entity e)
        {
        }

        /// <inheritdoc />
        protected override void OnContextAttached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext modelContext:
                    _unityTransformContext = modelContext;
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnContextDetached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext _:
                    _unityTransformContext = null;
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnUnbind()
        {
            _unityTransformContext = null;
        }

        /// <inheritdoc />
        public void OnDelta(in ComponentDelta<Position> delta)
        {
            _pos.ApplyDelta(delta);
        }

        /// <inheritdoc />
        public void OnDelta(in ComponentDelta<Rotation> delta)
        {
            _rot.ApplyDelta(delta);
        }

        /// <inheritdoc />
        protected override void OnApply(IWorld w, Entity e)
        {
            var t = _unityTransformContext?.Root;
            if (t == null) return;

            if (_pos.NeedsApply)
            {
                _pos.ClearDirty();
                if (_pos.Has)
                {
                    t.position = new Vector3(_pos.Last.X, _pos.Last.Y, _pos.Last.Z);
                }
            }

            if (_rot.NeedsApply)
            {
                _rot.ClearDirty();
                if (_rot.Has)
                {
                    t.eulerAngles = new Vector3(_rot.Last.X, _rot.Last.Y, _rot.Last.Z);
                }
            }
        }
    }
}
