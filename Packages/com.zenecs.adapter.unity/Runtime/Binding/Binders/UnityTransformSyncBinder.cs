#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Binders.Implementations
{
    public class UnityTransformSyncBinder :
        BaseBinder,
        IBind<Position>,
        IBind<Rotation>,
        IBind<Scale>
    {
        private DeltaTracker<Position> _pos;
        private DeltaTracker<Rotation> _rot;
        private DeltaTracker<Scale> _scale;
        
        private UnityTransformContext? _unityTransformContext;

        protected override void OnBind(Entity e, IReadOnlyList<IContext>? contexts)
        {
            var world = this.World;
            if (world == null) return;
            if (contexts == null) return;
            foreach (var context in contexts)
            {
                OnContextAttached(context);
            }
        }

        protected override void OnContextAttached(IContext context)
        {
            switch (context)
            {
                case UnityTransformContext modelContext:
                    _unityTransformContext = modelContext;
                    break;
                case SharedUIRootContext _:
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
                case SharedUIRootContext _:
                    break;
            }
        }

        protected override void OnUnbind()
        {
            _unityTransformContext = null;
        }

        public void OnDelta(in ComponentDelta<Position> delta)
        {
            _pos.ApplyDelta(delta);
        }
        
        public void OnDelta(in ComponentDelta<Rotation> delta)
        {
            _rot.ApplyDelta(delta);
        }

        public void OnDelta(in ComponentDelta<Scale> delta)
        {
            _scale.ApplyDelta(delta);
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
                    t.position = _pos.Last.Value;
                }
            }

            if (_rot.NeedsApply)
            {
                _rot.ClearDirty();
                if (_rot.Has)
                {
                    t.rotation = _rot.Last.Value;
                }
            }

            if (_scale.NeedsApply)
            {
                _scale.ClearDirty();
                if (_scale.Has)
                {
                    t.localScale = _scale.Last.Value;
                }
            }
        }
    }
}