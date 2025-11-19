#nullable enable
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Components;  // FixedPosition2D, Velocity2D 등

namespace ZenECS.Physics.Systems
{
    /// <summary>
    /// Consumes FireProjectileRequest on owner entities
    /// and spawns projectile entities with initial position/velocity.
    /// </summary>
    [ZenSystemWatch(typeof(FireProjectileRequest))]
    [SimulationGroup]
    public sealed class ProjectileSpawnSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            
            foreach (var (owner, req) in w.Query<FireProjectileRequest>())
            {
                // 1) 엔티티 예약 + SpawnOp 기록
                var projectile = cmd.SpawnEntity();

                // 2) 컴포넌트 추가는 전부 버퍼에 기록
                cmd.AddComponent(projectile, req.SpawnPos);
                cmd.AddComponent(projectile, req.InitialVelocity);

                // 투사체 데이터 세팅
                var proj = new Projectile
                {
                    Owner       = owner,
                    Damage      = req.Damage,
                    Radius      = req.Radius,
                    MaxDistance = req.MaxDistance,
                    Traveled    = 0
                };
                cmd.AddComponent(projectile, proj);
                
                // 3) 발사 요청 제거도 버퍼에 기록
                cmd.RemoveComponent<FireProjectileRequest>(owner);
            }
            
            // using scope 종료 → Dispose → Schedule → 프레임 배리어에서 Spawn+Add/Remove 모두 적용
        }
    }
}