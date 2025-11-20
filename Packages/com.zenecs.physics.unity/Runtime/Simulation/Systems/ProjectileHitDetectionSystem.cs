#nullable enable
using System.Collections.Generic;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// Detects hits between projectiles and circular targets, emits HitEvent, and despawns projectiles.
    /// - int 그리드 기반 원형 충돌 (dx*dx + dy*dy vs (r1+r2)^2) 를 사용한다.
    /// </summary>
    [ZenSystemWatch(typeof(Projectile))]
    [OrderAfter(typeof(ProjectileLifetimeSystem))]
    [SimulationGroup]
    public sealed class ProjectileHitDetectionSystem : IFixedRunSystem
    {
        // Health + CircleCollider2D 가진 애들만 피격 후보
        private static readonly Filter VictimFilter = new Filter.Builder()
            .With<FixedPosition2D>()
            .With<CircleCollider2D>()
            .Build();

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            // --- 1) Victim 후보 캐싱 ---
            var victims = new List<(Entity e, FixedPosition2D pos, CircleCollider2D col)>();

            foreach (var (e, pos, col) in
                     w.Query<FixedPosition2D, CircleCollider2D>(VictimFilter))
            {
                // Projectile 자신은 Victim 에서 제외
                if (w.HasComponent<Projectile>(e))
                    continue;

                victims.Add((e, pos, col));
            }

            // --- 2) 각 발사체에 대해 원형 충돌 체크 ---
            foreach (var (projEntity, proj, projPos, projCol) in
                     w.Query<Projectile, FixedPosition2D, CircleCollider2D>())
            {
                int px = projPos.x;
                int py = projPos.y;

                // Projectile.Radius 와 Collider.radius 둘 중 유효한 값 사용
                int pRadius = projCol.radius > 0 ? projCol.radius : proj.Radius;
                if (pRadius <= 0) pRadius = proj.Radius;

                bool hit = false;

                for (int i = 0; i < victims.Count; i++)
                {
                    var (victim, vPos, vCol) = victims[i];

                    if (!w.IsAlive(victim))
                        continue;

                    // 자기 Owner 에게는 맞지 않도록 (필요 없으면 제거)
                    if (victim.Equals(proj.Owner))
                        continue;

                    int dx = vPos.x - px;
                    int dy = vPos.y - py;

                    long distSq = (long)dx * dx + (long)dy * dy;

                    int radiusSum = pRadius + vCol.radius;
                    long radiusSq = (long)radiusSum * radiusSum;

                    if (distSq <= radiusSq)
                    {
                        // ---- HitEvent 생성 ----
                        var hitEvent = cmd.SpawnEntity();
                        cmd.AddComponent(hitEvent, new HitEvent
                        {
                            Target = victim,
                            // Source 를 Owner 로 볼지, Projectile Entity 로 볼지는 규칙에 따라
                            Source = proj.Owner,
                            Damage = proj.Damage
                        });

                        // 기본: 첫 히트에서 발사체 제거 (관통탄이면 여기서 제거 안하면 됨)
                        cmd.DespawnEntity(projEntity);
                        hit = true;
                        break;
                    }
                }

                // 관통탄/멀티히트 구현 시 hit == true 이후에도 계속 검사하는 식으로 확장 가능
            }
        }
    }
}
