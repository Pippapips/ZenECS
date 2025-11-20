#nullable enable
using System;
using Unity.Mathematics;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// Consumes FireProjectileRequest and spawns deterministic grid-based projectiles.
    /// - FixedPosition2D / Velocity2D / CircleCollider2D / KinematicBodyTag2D 를 설정해서
    ///   기존 KinematicMoveOnGrid2DSystem 파이프라인을 그대로 재사용한다.
    /// </summary>
    [ZenSystemWatch(typeof(FireProjectileRequest))]
    [SimulationGroup]
    public sealed class ProjectileSpawnSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            // Shooter + FireProjectileRequest + FixedPosition2D + MovementStats2D
            foreach (var (shooter, req, shooterPos, stats) in
                     w.Query<FireProjectileRequest, FixedPosition2D, MovementStats2D>())
            {
                int unitsPerUnity = stats.UnitsPerUnity;
                // stats에 값이 셋업되어 있으면 그걸, 아니면 dt 사용
                float fixedDelta = stats.FixedDeltaTime > 0f ? stats.FixedDeltaTime : dt;

                // ----- 방향/속도: float 방향 + speed → int grid velocity -----
                var dir = math.normalizesafe(req.Direction, new float2(1f, 0f));

                // 유니티 단위 속도(m/s) → 그리드 units / tick
                float speedUnitsPerTickF = req.Speed * unitsPerUnity * fixedDelta;
                int speedUnitsPerTick = (int)math.round(speedUnitsPerTickF);

                int vx = (int)math.round(dir.x * speedUnitsPerTick);
                int vy = (int)math.round(dir.y * speedUnitsPerTick);

                // 0 속도로 나오는 경우를 방지하려면 최소 1 step 부여해도 됨
                if (vx == 0 && vy == 0)
                {
                    // 방향이 거의 0이면 그냥 오른쪽 한 칸 같은 기본값
                    vx = speedUnitsPerTick;
                    vy = 0;
                }

                // ----- radius / maxDistance / damage 를 int grid units 로 변환 -----
                int radiusUnits = (int)math.round(req.Radius * unitsPerUnity);
                int maxDistanceUnit = (int)math.round(req.MaxDistance * unitsPerUnity);
                int damageInt = (int)math.round(req.Damage);

                if (radiusUnits <= 0) radiusUnits = 1;
                if (maxDistanceUnit < 0) maxDistanceUnit = 0;

                // ----- 발사체 엔티티 생성 -----
                var projectile = cmd.SpawnEntity();

                // 고정 좌표: shooter 의 현재 그리드 위치에서 시작
                cmd.AddComponent(projectile, new FixedPosition2D
                {
                    x = shooterPos.x,
                    y = shooterPos.y
                });

                // 그리드 속도
                cmd.AddComponent(projectile, new Velocity2D
                {
                    vx = vx,
                    vy = vy
                });

                // 맵 충돌용 콜라이더 (trigger)
                cmd.AddComponent(projectile, new CircleCollider2D
                {
                    radius = radiusUnits,
                    layerMask = 1,
                    isTrigger = true
                });

                // 발사체 메타
                cmd.AddComponent(projectile, new Projectile
                {
                    Owner = req.Shooter,
                    Damage = damageInt,
                    Radius = radiusUnits,
                    MaxDistance = maxDistanceUnit,
                    Traveled = 0
                });

                // 이동/보간 파이프라인 재사용을 위한 MovementStats2D
                cmd.AddComponent(projectile, new MovementStats2D
                {
                    UnitsPerUnity = unitsPerUnity,
                    FixedDeltaTime = fixedDelta,
                    MoveSpeedUnityPerSecond = req.Speed,
                    InterpolationAlpha = 0f,
                    LastFixedX = shooterPos.x,
                    LastFixedY = shooterPos.y
                });

                // Kinematic 파이프 라인에 태그
                cmd.AddComponent(projectile, new KinematicBodyTag2D());

                // 현재 렌더링 좌표 (유니티 단위) 셋업
                float px = (float)shooterPos.x / unitsPerUnity;
                float py = (float)shooterPos.y / unitsPerUnity;
                cmd.AddComponent(projectile, new Position2D
                {
                    x = px,
                    y = py
                });

                // 이 요청은 한 번 쓰고 제거
                cmd.RemoveComponent<FireProjectileRequest>(shooter);
            }
        }
    }
}