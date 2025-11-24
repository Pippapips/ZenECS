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
    /// - 발사체를 "그리드 OnGrid2D" 방식으로 이동시키고,
    ///   타일/피격 대상과 충돌 시 HitEvent + ProjectileImpactInfo 생성.
    /// - 코너 슬라이딩 없이, 벽과 겹치기 직전까지 이동 후 그 자리에서 터지게 처리.
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

                // 🔹 1) OnGrid2D 미로식 이동 (코너 슬라이딩 없이)
                var newPos = pos;
                var moveResult = KinematicGridMove2D.MoveMazeStyleNoSlide(
                    ref newPos,
                    in vel,
                    in col,
                    in map
                );

                int targetX = newPos.x;
                int targetY = newPos.y;

                bool hitSomething = false;
                float2 impactPoint = float2.zero;
                float2 impactNormal = float2.zero;
                Entity hitTarget = Entity.None;

                // 🔹 2) 타일 충돌: MoveMazeStyleNoSlide 중 벽에 막혔는지 여부
                bool blockedByTile = moveResult.HitWall;

                if (blockedByTile && moveResult.LastHitTileX >= 0 && moveResult.LastHitTileY >= 0)
                {
                    hitSomething = true;

                    // 간단한 벽 법선 계산 (히트 타일 기준)
                    int tileMinX = map.originX + moveResult.LastHitTileX * map.tileSize;
                    int tileMaxX = tileMinX + map.tileSize;
                    int tileMinY = map.originY + moveResult.LastHitTileY * map.tileSize;
                    int tileMaxY = tileMinY + map.tileSize;

                    int cx = targetX;
                    int cy = targetY;

                    int closestX = math.clamp(cx, tileMinX, tileMaxX);
                    int closestY = math.clamp(cy, tileMinY, tileMaxY);

                    float2 diff = new float2(cx - closestX, cy - closestY);
                    if (math.lengthsq(diff) > 0.5f)
                    {
                        impactNormal = math.normalize(diff);
                    }
                    else
                    {
                        // fallback: 이동 방향의 반대
                        float2 moveDir = math.normalize(new float2(dx, dy));
                        impactNormal = -moveDir;
                    }

                    float2 center = new float2(targetX, targetY);
                    impactPoint = center - impactNormal * col.radius;
                }

                // 🔹 3) 피격 대상 충돌 (타일에 막히지 않았을 때만)
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

                // 🔹 4) 충돌 처리
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

                    // 🔸 여기서 고정 좌표 → 렌더 좌표 스냅 + 보간 정리
                    // newPos: MoveMazeStyleNoSlide 이후의 FixedPosition2D
                    if (w.TryReadComponent<MovementStats2D>(projEntity, out var statsIn))
                    {
                        Interpolation2DUtils.SnapToFixedAndStopInterpolation(
                            in newPos,    // 이번 틱의 최종 FixedPosition2D
                            in statsIn,
                            out var statsOut,
                            out var snappedPos);

                        // 보간 상태 업데이트
                        cmd.ReplaceComponent(projEntity, statsOut);

                        // Position2D 스냅 (있으면 교체, 없으면 추가)
                        if (w.TryReadComponent<Position2D>(projEntity, out var _))
                        {
                            cmd.ReplaceComponent(projEntity, snappedPos);
                        }
                        else
                        {
                            cmd.AddComponent(projEntity, snappedPos);
                        }
                    }
                    
                    // 발사체 소멸 (w.Tick 기준 수명 옵션 유지)
                    cmd.AddComponent(projEntity, new DeadTag((int)w.Tick, 1000));
                    continue;
                }

                // 🔹 5) 충돌이 없으면 이동 + 이동 거리 누적
                cmd.ReplaceComponent(projEntity, newPos);

                var newProj = proj;
                int movedX = newPos.x - startX;
                int movedY = newPos.y - startY;
                int stepDist = (int)math.round(math.sqrt(movedX * movedX + movedY * movedY));
                newProj.Traveled += stepDist;
                cmd.ReplaceComponent(projEntity, newProj);

                // 디버그용 Ray
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
