#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Components;

namespace ZenECS.Physics.Systems
{
    [ZenSystemWatch(typeof(MovementStats2D), typeof(Velocity2D))]
    [SimulationGroup]
    public sealed class ApplyMoveInput2DSystem : IFixedRunSystem, ISystemLifecycle
    {
        private IDisposable? _input2DMessage = null;

        private int _dx;
        private int _dy;
        
        public void Initialize(IWorld w)
        {
            _input2DMessage = w.Subscribe<MoveInput2D>(input2D =>
            {
                _dx = input2D.dx;
                _dy = input2D.dy;
            });
        }
        
        public void Shutdown()
        {
            _input2DMessage?.Dispose();
        }
        
        public void Run(IWorld w, float dt)
        {
            // dt ignore
            foreach (var (e, stats, vel) in
                     w.Query<MovementStats2D, Velocity2D>())
            {
                var newVel = vel;
                newVel.vx = _dx * stats.GetSpeedPerTick(dt) / 1000;
                newVel.vy = _dy * stats.GetSpeedPerTick(dt) / 1000;
                w.ReplaceComponent(e, newVel);
            }
        }
    }
}