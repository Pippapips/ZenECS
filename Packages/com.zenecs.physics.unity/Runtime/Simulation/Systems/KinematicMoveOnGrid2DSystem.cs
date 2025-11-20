using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;
using CircleCollider2D = ZenECS.Physics.Unity.Simulation.Components.CircleCollider2D;

namespace ZenECS.Physics.Unity.Simulation.Systems
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
                var result = KinematicGridMove2D.MoveWithTileCollision(ref newPos, in vel, in col, in map);
                if (result.hitWall)
                {
                    if (w.TryReadComponent<Projectile>(e, out var projectile))
                    {
                        Debug.Log("HitEvent Here");
                        cmd.AddComponent(e, new HitEvent()
                        {
                            Damage = 10,
                            Source = projectile.Owner,
                            HitType = EHitType.MapWall
                        });
                    }
                }
                else if (result.moved)
                {
                    // 실제로 위치가 바뀌었을 때만 스냅 + 보간 리셋
                    cmd.ReplaceComponent(e, newPos);

                    var newStats = stats;
                    newStats.InterpolationAlpha = 0f;
                    newStats.LastFixedX = newPos.x;
                    newStats.LastFixedY = newPos.y;
                    cmd.ReplaceComponent(e, newStats);
                }
            }
        }
    }
}