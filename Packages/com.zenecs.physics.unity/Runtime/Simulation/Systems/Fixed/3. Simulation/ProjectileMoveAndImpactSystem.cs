// #nullable enable
// using System.Collections.Generic;
// using Unity.Mathematics;
// using ZenECS.Core;
// using ZenECS.Core.Attributes;
// using ZenECS.Core.Systems;
// using ZenECS.Physics.Unity.Simulation.Components;
//
// namespace ZenECS.Physics.Unity.Simulation.Systems
// {
//     /// <summary>
//     /// FixedSimulation:
//     /// - 발사체를 이동시키고 타일/피격 대상과 충돌 시 HitEvent + ProjectileImpactInfo 생성.
//     /// </summary>
//     [FixedSimulationGroup]
//     [OrderAfter(typeof(ProjectileSpawnSystem))]
//     public sealed class ProjectileMoveAndImpactSystem : IFixedRunSystem
//     {
//         private static readonly Filter _projectileFilter = new Filter.Builder()
//             .With<Projectile>()
//             .With<FixedPosition2D>()
//             .With<Velocity2D>()
//             .With<CircleCollider2D>()
//             .Without<DeadTag>()
//             .Build();
//
//         private static readonly Filter _hitboxFilter = new Filter.Builder()
//             .With<HitboxCircle>()
//             .With<FixedPosition2D>()
//             .With<Health>()
//             .Without<DeadTag>()
//             .Build();
//
//         public void Run(IWorld w, float dt)
//         {
//             using var cmd = w.BeginWrite();
//             var map = w.GetSingleton<MapGrid2D>();
//
//             var targets = new List<(Entity e, HitboxCircle hb, FixedPosition2D pos)>();
//             foreach (var (e, hb, pos) in w.Query<HitboxCircle, FixedPosition2D>(_hitboxFilter))
//             {
//                 targets.Add((e, hb, pos));
//             }
//
//             foreach (var (projEntity, proj, pos, vel, col)
//                      in w.Query<Projectile, FixedPosition2D, Velocity2D, CircleCollider2D>(_projectileFilter))
//             {
//                 int startX = pos.x;
//                 int startY = pos.y;
//                 int dx = vel.vx;
//                 int dy = vel.vy;
//
//                 if (dx == 0 && dy == 0)
//                     continue;
//
//                 int targetX = startX + dx;
//                 int targetY = startY + dy;
//
//                 bool hitSomething = false;
//                 float2 impactPoint = float2.zero;
//                 float2 impactNormal = float2.zero;
//                 Entity hitTarget = Entity.None;
//
//                 // 1) 타일 충돌
//                 bool blockedByTile = TileCollision2D.CheckCircle(
//                     in map, targetX, targetY, col.radius, col.layerMask);
//
//                 if (blockedByTile)
//                 {
//                     hitSomething = true;
//                     impactNormal = TileNormalUtil2D.EstimateWallNormal(
//                         map, in col, startX, startY, targetX, targetY);
//
//                     float2 start = new float2(startX, startY);
//                     float2 move = new float2(dx, dy);
//                     float2 center = start + move;
//                     float2 n = math.lengthsq(impactNormal) > 0.5f
//                         ? math.normalize(impactNormal)
//                         : math.normalize(-move);
//
//                     impactPoint = center - n * col.radius;
//                 }
//
//                 // 2) 피격 대상 충돌
//                 if (!blockedByTile)
//                 {
//                     float2 projCenter = new float2(targetX, targetY);
//
//                     foreach (var (t, hb, tPos) in targets)
//                     {
//                         if (t.Equals(proj.Owner))
//                             continue;
//
//                         float2 targetCenter = new float2(tPos.x, tPos.y);
//                         float2 diff = projCenter - targetCenter;
//                         float distSq = math.lengthsq(diff);
//                         float radiusSum = col.radius + hb.Radius;
//                         float radiusSq = radiusSum * radiusSum;
//
//                         if (distSq <= radiusSq)
//                         {
//                             hitSomething = true;
//                             hitTarget = t;
//
//                             float2 dir = math.normalize(diff);
//                             impactNormal = -dir;
//                             impactPoint = targetCenter + dir * hb.Radius;
//                             break;
//                         }
//                     }
//                 }
//
//                 if (hitSomething)
//                 {
//                     if (!hitTarget.IsNone && w.HasComponent<Health>(hitTarget))
//                     {
//                         var evt = cmd.SpawnEntity();
//                         cmd.AddComponent(evt, new HitEvent
//                         {
//                             Target = hitTarget,
//                             Source = projEntity,
//                             Damage = proj.Damage
//                         });
//                     }
//
//                     cmd.ReplaceComponent(projEntity, new ProjectileImpactInfo
//                     {
//                         Point = impactPoint,
//                         Normal = math.lengthsq(impactNormal) > 0.5f
//                             ? math.normalize(impactNormal)
//                             : float2.zero
//                     });
//
//                     cmd.AddComponent(projEntity, new DeadTag());
//                     continue;
//                 }
//
//                 // 3) 충돌이 없으면 이동 + 이동 거리 누적
//                 var newPos = pos;
//                 newPos.x = targetX;
//                 newPos.y = targetY;
//                 cmd.ReplaceComponent(projEntity, newPos);
//
//                 var newProj = proj;
//                 int stepDist = (int)math.round(math.sqrt(dx * dx + dy * dy));
//                 newProj.Traveled += stepDist;
//
//                 cmd.ReplaceComponent(projEntity, newProj);
//             }
//         }
//     }
// }
