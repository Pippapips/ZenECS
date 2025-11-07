#nullable enable
using System;
using ZenECS.Core.Internal.Contexts;

namespace ZenECS.Core.Binding
{
    public interface IBinder
    {
        Entity Entity { get; }
        int Priority { get; }   // 다수의 바인더가 붙어있을 경우 낮은 순 부터 먼저 Apply됨
        void Bind(IWorld world, Entity e, IContextLookup contextLookup);
        void Unbind();
        void Apply(); // 프레임 말(프리젠테이션 끝)에 항상 1회
    }

    internal interface IAttachOrderMarker
    {
        int AttachOrder { get; set; }
    }

    public abstract class BaseBinder : IBinder, IAttachOrderMarker
    {
        protected IWorld? _world { get; private set; }
        protected IContextLookup? _contextLookup { get; private set; }
        public Entity Entity { get; private set; }
        public virtual int Priority { get; set; }
        int IAttachOrderMarker.AttachOrder { get; set; }
        private bool _bound;

        public void Bind(IWorld world, Entity e, IContextLookup contextLookup)
        {
            if (_bound) throw new Exception();
            _world = world;
            _contextLookup = contextLookup;
            Entity = e;
            _bound = true;
            OnBind(e);
        }
        
        public void Unbind()
        {
            if (!_bound) return;
            try
            {
                OnUnbind();
            }
            finally
            {
                _bound = false;
                _world = null;
                Entity = default;
            }
        }
        
        public virtual void Apply() { }
        
        protected virtual void OnBind(Entity e) { }
        protected virtual void OnUnbind() { }
    }
}