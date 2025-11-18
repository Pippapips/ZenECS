#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Physics.Components;

namespace ZenECS.Physics.Grid2D.Unity.Binding.Binders
{
    public class Grid2DUnityTransform2DSyncBinder :
        BaseBinder,
        IBind<Position2D>
    {
        private DeltaTracker<Position2D> _pos;
        
        private UnityTransformContext? _unityTransformContext;

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

        public void OnDelta(in ComponentDelta<Position2D> delta)
        {
            _pos.ApplyDelta(delta);
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
                    var p = t.position;
                    p.x = _pos.Last.x;
                    p.z = _pos.Last.y;
                    t.position = p;
                }
            }
        }
    }
}