#nullable enable
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
    /// 결정론적인 그리드 기반 2D 키네마틱 이동 시스템.
    ///
    /// FixedPosition2D / Velocity2D / CircleCollider2D / MovementStats2D / KinematicBodyTag2D
    /// 를 가진 엔티티를 대상으로 동작한다.
    ///
    /// 이동 규칙 (요약):
    /// - 입력 벡터(vx, vy)를 기준으로 primary/secondary 축을 결정한다.
    /// - primary 축을 먼저 "미로 스타일"로 이동: 벽 직전까지 한 칸씩 전진.
    /// - primary 축에서 막혔고, 순수한 primary 입력일 때만 코너 슬라이드(모서리 돌기)를 시도.
    /// - 그 후 secondary 축도 같은 방식으로 이동.
    /// - 그럼에도 불구하고 (대각선 입력 + 충돌) 때문에 한 칸도 못 움직인 경우,
    ///   타일과의 관계를 보고 한 축만 살려서 다시 한 번 이동을 시도한다.
    ///
    /// 회전(FixedRotation2D)은 "실제로 이동한 방향"을 기준으로 갱신한다.
    /// 이동이 전혀 없고 입력만 있는 경우에는 입력 방향을 기준으로 회전을 갱신한다.
    /// </summary>
    [ZenSystemWatch(typeof(FixedPosition2D), typeof(Velocity2D), typeof(CircleCollider2D), typeof(KinematicBodyTag2D))]
    [OrderAfter(typeof(ProjectileSpawnSystem))]
    public sealed class KinematicMoveOnGrid2DSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            if (!w.TryGetSingleton<MapGrid2D>(out var map))
                return;

            using var cmd = w.BeginWrite();

            foreach (var (e, pos, vel, col, stats, rot) in w
                         .Query<FixedPosition2D, Velocity2D, CircleCollider2D, MovementStats2D, FixedRotation2D>())
            {
                // Kinematic이 아닌 애들은 스킵 (안전장치 – Tag가 없으면 넘어감)
                if (!w.HasComponent<KinematicBodyTag2D>(e))
                    continue;

                var newPos = pos;
                var newStats = stats;

                int axisLock = newStats.AxisLock;
                
                // 🔹 미로 스타일 + 코너 슬라이드 + 대각선 축 선택 이동
                var moveResult = KinematicGridMove2D.MoveMazeStyleWithCornerSlideAndAxisResolve(
                    ref newPos,
                    in vel,
                    in col,
                    in map,
                    ref axisLock 
                );

#if UNITY_EDITOR
                // 디버그용: 충돌 타일 기준 노란 법선 표시
                if (moveResult.HitWall && moveResult.LastHitTileX >= 0)
                {
                    // 히트된 타일 센터 (fixed 좌표)
                    int tileCenterX = map.originX + moveResult.LastHitTileX * map.tileSize + map.tileSize / 2;
                    int tileCenterY = map.originY + moveResult.LastHitTileY * map.tileSize + map.tileSize / 2;

                    // 플레이어 중심과 타일 중심의 차이 (fixed → unity로 변환)
                    float unitsPerUnityDbg = stats.UnitsPerUnity > 0 ? stats.UnitsPerUnity : 1000f;
                    float dx = (pos.x - tileCenterX) / unitsPerUnityDbg;
                    float dy = (pos.y - tileCenterY) / unitsPerUnityDbg;

                    var normal2d = math.normalizesafe(new float2(dx, dy), float2.zero);
                    var origin = new Vector3(tileCenterX / unitsPerUnityDbg, 0f, tileCenterY / unitsPerUnityDbg);
                    var dir3 = new Vector3(normal2d.x, 0f, normal2d.y);

                    Debug.DrawRay(origin, dir3, Color.yellow, 0f, false);
                }
#endif

                // FixedPosition2D 갱신
                cmd.ReplaceComponent(e, newPos);

                // 인터폴레이션 상태 갱신 (이전 위치 저장)
                newStats.InterpolationAlpha = 0f;
                newStats.LastFixedX = pos.x;
                newStats.LastFixedY = pos.y;
                newStats.AxisLock = axisLock;
                cmd.ReplaceComponent(e, newStats);

                // 렌더링 좌표(Position2D) 갱신
                float unitsPerUnity = stats.UnitsPerUnity > 0 ? stats.UnitsPerUnity : 1000f;
                float px = (float)newPos.x / unitsPerUnity;
                float py = (float)newPos.y / unitsPerUnity;

                if (w.TryReadComponent<Position2D>(e, out var renderPos))
                {
                    renderPos.x = px;
                    renderPos.y = py;
                    cmd.ReplaceComponent(e, renderPos);
                }
                else
                {
                    cmd.AddComponent(e, new Position2D
                    {
                        x = px,
                        y = py
                    });
                }

                // --- 회전 갱신: 실제 이동 방향 기준 ---
                int moveX = newPos.x - pos.x;
                int moveY = newPos.y - pos.y;

                float2? dirOpt = null;

                // 1) 실제 이동이 있었다면 그 방향 사용
                if (moveX != 0 || moveY != 0)
                {
                    dirOpt = math.normalize(new float2(moveX, moveY));
                }
                else
                {
                    // 2) 이동은 안 했지만 입력은 있는 경우 → 입력 방향 기준으로 회전 허용
                    if (stats.MoveInputX != 0 || stats.MoveInputY != 0)
                    {
                        dirOpt = math.normalize(new float2(stats.MoveInputX, stats.MoveInputY));
                    }
                }

                // dirOpt가 유효할 때만 회전 갱신
                if (dirOpt.HasValue)
                {
                    var dir = dirOpt.Value;
                    var newRot = rot;
                    newRot.SetFromForward(dir);
                    cmd.ReplaceComponent(e, newRot);
                }
            }
        }
    }
}
