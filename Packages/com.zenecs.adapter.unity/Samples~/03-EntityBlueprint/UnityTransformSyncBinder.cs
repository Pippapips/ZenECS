#nullable enable
using System;
using BountyBang.Contexts;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    [Serializable]
    public class UnityTransformSyncBinder :
        BaseBinder,
        IBind<Position>,
        IBind<Rotation>
    {
        private DeltaTracker<Position> _pos;
        private DeltaTracker<Rotation> _rot;
        private UnityTransformContext? _unityTransformContext;
        private bool _wasDead;

        protected override void OnBind(Entity e)
        {
        }

        protected override void OnContextAttached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext modelContext:
                    _unityTransformContext = modelContext;
                    break;
            }
        }

        protected override void OnContextDetached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext _:
                    _unityTransformContext = null;
                    break;
            }
        }

        protected override void OnUnbind()
        {
            _unityTransformContext = null;

            var w = this.World;
            if (w == null) return;
            Debug.Log($"<color=#339900>Unbind 2D Sync Binder</color> F:{w.Kernel.FrameCount} T:{w.Kernel.FixedFrameCount}");
        }

        public void OnDelta(in ComponentDelta<Position> delta)
        {
            _pos.ApplyDelta(delta);
        }

        public void OnDelta(in ComponentDelta<Rotation> delta)
        {
            _rot.ApplyDelta(delta);
        }

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
