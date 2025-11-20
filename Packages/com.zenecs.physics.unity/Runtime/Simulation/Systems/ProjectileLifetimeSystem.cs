#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// Tracks projectile lifetime by accumulated travel distance (grid units).
    /// - Velocity2D 의 한 틱 이동량의 맨해튼 길이를 누적해서 MaxDistance 와 비교한다.
    /// </summary>
    [ZenSystemWatch(typeof(Projectile))]
    [OrderAfter(typeof(KinematicMoveOnGrid2DSystem))]
    [SimulationGroup]
    public sealed class ProjectileLifetimeSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            foreach (var (e, proj, vel) in w.Query<Projectile, Velocity2D>())
            {
                // 이동 거리: |vx| + |vy| (맨해튼)
                int step = Math.Abs(vel.vx) + Math.Abs(vel.vy);

                // 발사체가 정지된 경우는 생략 가능
                if (step <= 0)
                    continue;

                var newProj = proj;
                newProj.Traveled += step;

                if (newProj.Traveled >= newProj.MaxDistance)
                {
                    // 수명 종료 → 발사체 제거
                    cmd.DespawnEntity(e);
                }
                else
                {
                    cmd.ReplaceComponent(e, newProj);
                }
            }
        }
    }
}