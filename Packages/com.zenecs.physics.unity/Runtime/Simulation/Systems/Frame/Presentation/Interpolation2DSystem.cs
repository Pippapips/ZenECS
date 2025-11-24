using Unity.Mathematics;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    [ZenSystemWatch(typeof(FixedPosition2D), typeof(Position2D), typeof(KinematicBodyTag2D))]
    [PresentationGroup]
    public sealed class Interpolation2DSystem : IPresentationSystem
    {
        private static readonly Filter _f = new Filter.Builder()
            .With<KinematicBodyTag2D>()
            .Without<DeadTag>()
            .Build();

        public void Run(IWorld w, float dt, float alpha)
        {
            using var cmd = w.BeginWrite();
            
            foreach (var (e, fpos, pos, stats, frot) in
                     w.Query<FixedPosition2D, Position2D, MovementStats2D, FixedRotation2D>(_f))
            {
                var newStats = stats;

                // Fixed 타겟이 새로 바뀐 경우라면, 강제로 알파 리셋
                if (stats.LastFixedX != fpos.x || stats.LastFixedY != fpos.y)
                {
                    newStats.InterpolationAlpha = 0f;
                    newStats.LastFixedX = fpos.x;
                    newStats.LastFixedY = fpos.y;
                }
                
                // FixedDeltaTime 방어코드 (0이면 보간 불가 → 즉시 스냅)
                if (newStats.FixedDeltaTime <= 0f)
                {
                    var snapped = new Position2D(
                        (float)fpos.x / (float)newStats.UnitsPerUnity,
                        (float)fpos.y / (float)newStats.UnitsPerUnity);

                    cmd.ReplaceComponent(e, snapped);
                    newStats.InterpolationAlpha = 1f;
                    cmd.ReplaceComponent(e, newStats);
                    continue;
                }
                
                // 보간 알파 증가
                newStats.InterpolationAlpha = Mathf.Clamp01(
                    newStats.InterpolationAlpha + dt / newStats.FixedDeltaTime);

                cmd.ReplaceComponent(e, newStats);

                // ---- 여기서부터는 "항상 Unity 단위"에서 Lerp ----
                float fromX = pos.x;
                float fromY = pos.y;

                float toX = (float)fpos.x / (float)newStats.UnitsPerUnity;
                float toY = (float)fpos.y / (float)newStats.UnitsPerUnity;

                float x = Mathf.Lerp(fromX, toX, newStats.InterpolationAlpha);
                float y = Mathf.Lerp(fromY, toY, newStats.InterpolationAlpha);

                var interp = new Position2D(x, y);
                cmd.ReplaceComponent(e, interp);
            }
        }
    }
}