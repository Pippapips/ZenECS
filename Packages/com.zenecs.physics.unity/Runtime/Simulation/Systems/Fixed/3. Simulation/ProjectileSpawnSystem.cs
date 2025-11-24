﻿#nullable enable
using Unity.Mathematics;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedSimulation:
    /// - FireProjectileRequest를 소비해서 Projectile 엔티티를 생성.
    /// </summary>
    [ZenSystemWatch(typeof(FireProjectileRequest))]
    [FixedSimulationGroup]
    [OrderBefore(typeof(KinematicMoveOnGrid2DSystem))]
    public sealed class ProjectileSpawnSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            foreach (var (shooter, req, shooterPos, shooterStats)
                     in w.Query<FireProjectileRequest, FixedPosition2D, MovementStats2D>())
            {
                float2 dir = req.Direction;
                if (math.lengthsq(dir) < 1e-6f)
                    dir = new float2(1, 0);
                dir = math.normalize(dir);

                int units = shooterStats.UnitsPerUnity;
                int radiusFixed = (int)math.round(req.RadiusUnity * units);
                int maxDistFixed = (int)math.round(req.MaxDistanceUnity * units);

                var startPos = shooterPos;
                float muzzleOffsetUnity = req.RadiusUnity;
                int offsetX = (int)math.round(dir.x * muzzleOffsetUnity * units);
                int offsetY = (int)math.round(dir.y * muzzleOffsetUnity * units);
                startPos.x += offsetX;
                startPos.y += offsetY;

                float distancePerTickUnity = req.SpeedUnityPerSecond * dt;
                int vx = (int)math.round(dir.x * distancePerTickUnity * units);
                int vy = (int)math.round(dir.y * distancePerTickUnity * units);

                var proj = cmd.SpawnEntity();
                cmd.AddComponent(proj, new FixedPosition2D(startPos.x, startPos.y));
                cmd.AddComponent(proj, new Velocity2D(vx, vy));
                cmd.AddComponent(proj, new CircleCollider2D
                {
                    radius = radiusFixed,
                    layerMask = 1,
                    isTrigger = true
                });
                cmd.AddComponent(proj, new Projectile(
                    owner: shooter,
                    damage: (int)math.round(req.Damage),
                    radius: radiusFixed,
                    maxDistance: maxDistFixed
                ));
                cmd.AddComponent(proj, new KinematicBodyTag2D());

                // 🔹 MovementStats2D 복사/초기화
                var mstats = shooterStats;
                mstats.InterpolationAlpha = 0f;
                mstats.LastFixedX = startPos.x;
                mstats.LastFixedY = startPos.y;
                cmd.AddComponent(proj, mstats);
                
                // 초기 렌더링용 Position2D 세팅 (옵션)
                cmd.AddComponent(proj, new Position2D
                {
                    x = (float)startPos.x / units,
                    y = (float)startPos.y / units
                });

                var projRot = new FixedRotation2D();
                projRot.SetFromForward(dir);
                cmd.AddComponent(proj, projRot);

                cmd.RemoveComponent<FireProjectileRequest>(shooter);
            }
        }
    }
}
