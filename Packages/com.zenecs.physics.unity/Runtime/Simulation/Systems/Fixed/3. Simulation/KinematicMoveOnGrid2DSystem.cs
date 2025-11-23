﻿#nullable enable
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;
using CircleCollider2D = ZenECS.Physics.Unity.Simulation.Components.CircleCollider2D;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedSimulation:
    /// - FixedPosition2D + Velocity2D + CircleCollider2D + MovementStats2D + KinematicBodyTag2D
    ///   를 가진 엔티티를 브롤스타즈 스타일로 이동시킨다.
    /// </summary>
    [ZenSystemWatch(
        typeof(FixedPosition2D),
        typeof(Velocity2D),
        typeof(CircleCollider2D),
        typeof(MovementStats2D),
        typeof(KinematicBodyTag2D)
    )]
    [FixedSimulationGroup]
    [OrderAfter(typeof(ProjectileSpawnSystem))]
    public sealed class KinematicMoveOnGrid2DSystem : IFixedRunSystem
    {
        private static readonly Filter _f = new Filter.Builder()
            .With<KinematicBodyTag2D>()
            .Without<Projectile>()
            .Without<DeadTag>()
            .Build();

        public void Run(IWorld w, float dt)
        {
            var map = w.GetSingleton<MapGrid2D>();
            using var cmd = w.BeginWrite();

            foreach (var (e, pos, vel, col, stats) in
                     w.Query<FixedPosition2D, Velocity2D, CircleCollider2D, MovementStats2D>(_f))
            {
                var newPos = pos;

                var moveResult = KinematicGridMove2D.MoveWithTileCollision(
                    ref newPos,
                    in vel,
                    in col,
                    in map,
                    KinematicMoveOptions2D.CharacterStrong
                );

                if (moveResult)
                {
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
