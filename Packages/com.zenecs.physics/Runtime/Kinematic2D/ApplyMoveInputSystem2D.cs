using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Physics.Kinematic2D
{
    public sealed class ApplyMoveInputSystem2D : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            // dt ignore
            foreach (var (e, move, stats, vel) in
                     w.Query<MoveInput2D, MovementStats2D, Velocity2D>())
            {
                var newVel = vel;
                newVel.vx = move.dx * stats.speedPerTick / 1000;
                newVel.vy = move.dy * stats.speedPerTick / 1000;
                w.ReplaceComponent(e, newVel);
            }
        }
    }
}