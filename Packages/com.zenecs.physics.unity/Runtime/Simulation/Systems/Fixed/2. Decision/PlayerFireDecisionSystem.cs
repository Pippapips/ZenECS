#nullable enable
using Unity.Mathematics;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedDecision:
    /// - FireInputState + 플레이어 상태를 기반으로 FireProjectileRequest를 생성.
    /// </summary>
    [FixedDecisionGroup]
    public sealed class PlayerFireDecisionSystem : ISystemLifecycle, IFixedSetupSystem
    {
        private static readonly Filter _playerFilter = new Filter.Builder()
            .With<PlayerTag>()
            .With<FixedPosition2D>()
            .With<FixedRotation2D>()
            .Build();

        /// <summary>간단한 발사 쿨타임 관리용.</summary>
        public struct FireCooldown
        {
            public int TickUntilReady;
        }

        public void Initialize(IWorld w)
        {
        }
        
        public void Shutdown()
        {
        }

        public void Run(IWorld w, float dt)
        {
            if (!w.TryGetSingleton<FireInputState>(out var fireInput))
                return;

            using var cmd = w.BeginWrite();
            int currentTick = (int)w.Tick;

            foreach (var (e, pos, rot) in w.Query<FixedPosition2D, FixedRotation2D>(_playerFilter))
            {
                if (!fireInput.FireQueued)
                    return;
                
                // FireCooldown cd = default;
                // if (w.HasComponent<FireCooldown>(e))
                //     cd = w.GetComponent<FireCooldown>(e);
                //
                // if (cd.TickUntilReady > currentTick)
                //     continue;

                float2 dir = rot.Forward();
                if (math.lengthsq(dir) < 1e-6f)
                    dir = new float2(1, 0);
                dir = math.normalize(dir);

                const float speedUnityPerSec = 15f;
                const float damage = 10f;
                const float maxDistanceUnity = 20f;
                const float radiusUnity = 0.3f;

                cmd.AddComponent(e, new FireProjectileRequest
                {
                    Shooter = e,
                    Direction = dir,
                    SpeedUnityPerSecond = speedUnityPerSec,
                    Damage = damage,
                    MaxDistanceUnity = maxDistanceUnity,
                    RadiusUnity = radiusUnity
                });

                // cd.TickUntilReady = currentTick + 8; // 예: 8틱 쿨타임
                // cmd.SetOrAddComponent(e, cd);
                
                fireInput.FireQueued = false;
                cmd.SetSingleton<FireInputState>(fireInput);
            }
        }
    }

    /// <summary>
    /// 발사체 생성에 필요한 요청 값들 (결정 단계 → 스폰 단계).
    /// </summary>
    public struct FireProjectileRequest
    {
        public Entity Shooter;
        public float2 Direction;        // 정규화된 360도 방향
        public float SpeedUnityPerSecond;
        public float Damage;
        public float MaxDistanceUnity;
        public float RadiusUnity;
    }
}
