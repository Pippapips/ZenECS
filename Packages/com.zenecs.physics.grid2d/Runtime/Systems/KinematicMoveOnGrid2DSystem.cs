using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Components;
using ZenECS.Physics.Systems;

namespace ZenECS.Physics.Grid2D.Systems
{
    [ZenSystemWatch(typeof(FixedPosition2D), typeof(Velocity2D), typeof(CircleCollider2D), typeof(KinematicBodyTag2D))]
    [OrderAfter(typeof(ApplyMoveInput2DSystem))]
    [SimulationGroup]
    public sealed class KinematicMoveOnGrid2DSystem : IFixedRunSystem
    {
        private static readonly Filter _f = new Filter.Builder()
            .With<KinematicBodyTag2D>()
            .Build();
        
        public void Run(IWorld w, float dt)
        {
            var map = w.GetSingleton<MapGrid2D>();

            using var cmd = w.BeginWrite();
            
            foreach (var (e, pos, vel, col, stats) in
                     w.Query<FixedPosition2D, Velocity2D, CircleCollider2D, MovementStats2D>(_f))
            {
                var newPos = pos;
                if (KinematicGridMove2D.MoveWithTileCollision(ref newPos, in vel, in col, in map))
                {
                    // 실제로 위치가 바뀌었을 때만 스냅 + 보간 리셋
                    cmd.ReplaceComponent(e, newPos);

                    var newStats = stats;
                    newStats.InterpolationTime = 0f;
                    newStats.LastFixedX = newPos.x;
                    newStats.LastFixedY = newPos.y;
                    cmd.ReplaceComponent(e, newStats);
                }
            }
        }
    }
}