#nullable enable
using Unity.Mathematics;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedDecision:
    /// - MoveInput2DState(360도 방향)를 읽어 Velocity2D / FixedRotation2D / MovementStats2D 를 갱신.
    /// </summary>
    [ZenSystemWatch(typeof(MovementStats2D), typeof(Velocity2D), typeof(FixedRotation2D))]
    [FixedDecisionGroup]
    public sealed class ApplyMoveInput2DSystem : IFixedSetupSystem
    {
        private static readonly Filter _f = new Filter.Builder()
            .Without<Projectile>() // 발사체에는 이동 입력 적용 X
            .Build();

        private const float DirectionEpsilon = 1e-4f;

        public void Run(IWorld w, float dt)
        {
            if (!w.TryGetSingleton<MoveInput2DState>(out var inputEntity))
                return;

            var input = w.GetSingleton<MoveInput2DState>();
            var dir = input.Dir;
            float mag = input.Magnitude;

            using var cmd = w.BeginWrite();

            foreach (var (e, stats, vel, rot, pos) in w.Query<MovementStats2D, Velocity2D, FixedRotation2D, FixedPosition2D>(_f))
            {
                var fwd2d = rot.Forward();
                var fwd = new Vector3(fwd2d.x, 0, fwd2d.y);
                float sx = pos.x / 1000.0f;
                float sy = pos.y / 1000.0f;
                var origin = new Vector3(sx, 0, sy);
                Debug.DrawRay(origin, fwd, Color.red);
                
                if (mag < DirectionEpsilon || math.lengthsq(dir) < DirectionEpsilon)
                {
                    var stoppedVel = vel;
                    stoppedVel.vx = 0;
                    stoppedVel.vy = 0;
                    cmd.ReplaceComponent(e, stoppedVel);

                    var clearedStats = stats;
                    clearedStats.MoveInputX = 0;
                    clearedStats.MoveInputY = 0;
                    cmd.ReplaceComponent(e, clearedStats);
                    continue;
                }

                float2 n = math.normalize(dir);

                var newStats = stats;
                newStats.MoveInputX = (int)math.round(n.x * 1000);
                newStats.MoveInputY = (int)math.round(n.y * 1000);
                cmd.ReplaceComponent(e, newStats);

                int speedPerTick = stats.GetSpeedPerTick(dt);
                int vx = (int)math.round(n.x * speedPerTick * mag);
                int vy = (int)math.round(n.y * speedPerTick * mag);

                var newVel = vel;
                newVel.vx = vx;
                newVel.vy = vy;
                cmd.ReplaceComponent(e, newVel);

                // var newRot = rot;
                // newRot.SetFromForward(n);
                // cmd.ReplaceComponent(e, newRot);
            }
        }
    }
}
