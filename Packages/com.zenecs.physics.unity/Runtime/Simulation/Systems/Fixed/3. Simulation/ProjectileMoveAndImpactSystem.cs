#nullable enable
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation;
using ZenECS.Physics.Unity.Simulation.Components;
using CircleCollider2D = ZenECS.Physics.Unity.Simulation.Components.CircleCollider2D;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedSimulation:
    /// - 발사체를 이동시키고 타일/피격 대상과 충돌 시 HitEvent + ProjectileImpactInfo 생성.
    /// - 코너 보정 없이, 충돌 순간 바로 Impact 처리 후 DeadTag 부여.
    /// </summary>
    [FixedSimulationGroup]
    [OrderAfter(typeof(ProjectileSpawnSystem))]
    public sealed class ProjectileMoveAndImpactSystem : IFixedRunSystem
    {
        private static readonly Filter _projectileFilter = new Filter.Builder()
            .With<Projectile>()
            .With<FixedPosition2D>()
            .With<Velocity2D>()
            .With<CircleCollider2D>()
            .Without<DeadTag>()
            .Build();

        private static readonly Filter _hitboxFilter = new Filter.Builder()
            .With<HitboxCircle>()
            .With<FixedPosition2D>()
            .With<Health>()
            .Without<DeadTag>()
            .Build();

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            var map = w.GetSingleton<MapGrid2D>();

            // 피격 대상 목록 캐싱 (이번 틱 동안은 고정으로 사용)
            var targets = new List<(Entity e, HitboxCircle hb, FixedPosition2D pos)>();
            foreach (var (e, hb, pos) in w.Query<HitboxCircle, FixedPosition2D>(_hitboxFilter))
            {
                targets.Add((e, hb, pos));
            }

            foreach (var (projEntity, proj, pos, vel, col)
                     in w.Query<Projectile, FixedPosition2D, Velocity2D, CircleCollider2D>(_projectileFilter))
            {
                int startX = pos.x;
                int startY = pos.y;
                int dx = vel.vx;
                int dy = vel.vy;

                if (dx == 0 && dy == 0)
                    continue;

                int targetX = startX + dx;
                int targetY = startY + dy;

                bool hitSomething = false;
                float2 impactPoint = float2.zero;
                float2 impactNormal = float2.zero;
                Entity hitTarget = Entity.None;

                // 1) 타일 충돌 검사 (최신 CheckCircle 시그니처 사용)
                bool blockedByTile = TileCollision2D.CheckCircle(
                    in map,
                    targetX,
                    targetY,
                    col.radius,
                    col.layerMask,
                    out int _,       // hitTileX (현재는 사용 안 함)
                    out int _        // hitTileY (현재는 사용 안 함)
                );

                if (blockedByTile)
                {
                    hitSomething = true;

                    // 벽 법선 추정 (기존 유틸 유지)
                    impactNormal = TileNormalUtil2D.EstimateWallNormal(
                        map,
                        in col,
                        startX,
                        startY,
                        targetX,
                        targetY
                    );

                    float2 start = new float2(startX, startY);
                    float2 move = new float2(dx, dy);
                    float2 center = start + move;

                    float2 n = math.lengthsq(impactNormal) > 0.5f
                        ? math.normalize(impactNormal)
                        : math.normalize(-move);

                    impactPoint = center - n * col.radius;
                }

                // 2) 피격 대상 충돌 (타일에 막히지 않았을 때만)
                if (!blockedByTile)
                {
                    float2 projCenter = new float2(targetX, targetY);

                    foreach (var (t, hb, tPos) in targets)
                    {
                        // 자기 자신(Owner)은 무시
                        if (t.Equals(proj.Owner))
                            continue;

                        float2 targetCenter = new float2(tPos.x, tPos.y);
                        float2 diff = projCenter - targetCenter;
                        float distSq = math.lengthsq(diff);
                        float radiusSum = col.radius + hb.Radius;
                        float radiusSq = radiusSum * radiusSum;

                        if (distSq <= radiusSq)
                        {
                            hitSomething = true;
                            hitTarget = t;

                            float2 dir = math.normalize(diff);
                            impactNormal = -dir;
                            impactPoint = targetCenter + dir * hb.Radius;
                            break;
                        }
                    }
                }

                // 3) 충돌 처리
                if (hitSomething)
                {
                    // 피격 대상이 있으면 HitEvent 발행
                    if (!hitTarget.IsNone && w.HasComponent<Health>(hitTarget))
                    {
                        var evt = cmd.SpawnEntity();
                        cmd.AddComponent(evt, new HitEvent
                        {
                            Target = hitTarget,
                            Source = projEntity,
                            Damage = proj.Damage
                        });
                    }

                    // Impact 정보 기록 (이펙트/파티클 등에서 사용)
                    cmd.ReplaceComponent(projEntity, new ProjectileImpactInfo
                    {
                        Point = impactPoint,
                        Normal = math.lengthsq(impactNormal) > 0.5f
                            ? math.normalize(impactNormal)
                            : float2.zero
                    });

                    // 발사체 소멸
                    cmd.AddComponent(projEntity, new DeadTag());
                    continue;
                }

                // 4) 충돌이 없으면 이동 + 이동 거리 누적
                var newPos = pos;
                newPos.x = targetX;
                newPos.y = targetY;
                cmd.ReplaceComponent(projEntity, newPos);

                var newProj = proj;
                int stepDist = (int)math.round(math.sqrt(dx * dx + dy * dy));
                newProj.Traveled += stepDist;
                cmd.ReplaceComponent(projEntity, newProj);

                if (w.TryReadComponent<FixedRotation2D>(projEntity, out var frot))
                {
                    var fwd2d = frot.Forward();
                    var fwd = new Vector3(fwd2d.x, 0, fwd2d.y);
                    float sx = newPos.x / 1000.0f;
                    float sy = newPos.y / 1000.0f;
                    var origin = new Vector3(sx, 0, sy);
                    Debug.DrawRay(origin, fwd, Color.red);
                }
            }
        }
    }
}
