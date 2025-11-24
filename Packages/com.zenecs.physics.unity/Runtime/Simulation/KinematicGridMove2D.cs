#nullable enable
using Unity.Mathematics;
using ZenECS.Physics.Unity.Simulation;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// "미로 스타일 + 코너 슬라이드 + 대각선 축 선택 + 축 고정(AxisLock)" 2D 키네마틱 이동 유틸.
    ///
    /// axisLock:
    /// - 0: 축 고정 없음 (기본)
    /// - 1: 수직(Y축)만 이동 허용 (Vertical only)
    /// - 2: 수평(X축)만 이동 허용 (Horizontal only)
    ///
    /// axisLockCornerY / axisLockCornerX:
    /// - AxisLock 걸릴 때 기준이 된 타일 모서리 좌표를 저장한다.
    /// - Vertical only일 때: 플레이어 중심 y가 이 값을 넘어서는 순간 Lock 해제.
    /// - Horizontal only일 때: 플레이어 중심 x가 이 값을 넘어서는 순간 Lock 해제.
    /// </summary>
    public static class KinematicGridMove2D
    {
        public static KinematicMoveResult2D MoveMazeStyleWithCornerSlideAndAxisResolve(
            ref FixedPosition2D pos,
            in Velocity2D vel,
            in CircleCollider2D col,
            in MapGrid2D map,
            ref int axisLock,          // 0: none, 1: vertical only, 2: horizontal only
            ref int axisLockCornerY,   // vertical-only 기준 Y
            ref int axisLockCornerX)   // horizontal-only 기준 X
        {
            var result = new KinematicMoveResult2D
            {
                LastHitTileX = -1,
                LastHitTileY = -1
            };

            int startX = pos.x;
            int startY = pos.y;

            int x = startX;
            int y = startY;

            int dx = vel.vx;
            int dy = vel.vy;

            if (dx == 0 && dy == 0)
            {
                // 입력이 없으면 축 고정도 해제
                axisLock = 0;
                return result;
            }

            int stepsX = math.abs(dx);
            int stepsY = math.abs(dy);
            int signX  = dx > 0 ? 1 : (dx < 0 ? -1 : 0);
            int signY  = dy > 0 ? 1 : (dy < 0 ? -1 : 0);

            bool diagonalInput = (stepsX > 0 && stepsY > 0);

            // 🔹 대각선이 아니면 축 고정 해제 (평범한 직선 이동)
            if (!diagonalInput)
            {
                axisLock        = 0;
                axisLockCornerY = 0;
                axisLockCornerX = 0;
            }
            else
            {
                // 🔹 대각선 입력 + axisLock 이 이미 설정되어 있다면,
                //     → 해당 축만 사용해서 이동을 시도하고, 나머지 로직은 건너뛴다.
                if (axisLock == 1) // Vertical only (Y축만)
                {
                    int r = col.radius;

                    // 대각선 속도 길이를 유지하기 위한 스칼라 step
                    int effectiveSteps = (int)math.round(math.sqrt(dx * dx + dy * dy));
                    if (effectiveSteps <= 0)
                        effectiveSteps = math.max(stepsX, stepsY);

                    int beforeY = y;

                    if (effectiveSteps > 0 && signY != 0)
                    {
                        MoveAxis(ref x, ref y, signY, effectiveSteps, vertical: true, r, in map, ref result);
                    }

                    int movedY     = math.abs(y - beforeY);
                    int remainingY = effectiveSteps - movedY;

                    // 코너 슬라이드 (남은 step으로 좌우 슬라이드)
                    if (result.HitWall && remainingY > 0)
                    {
                        bool slid = TryCornerSlideVertical(
                            ref x,
                            ref y,
                            remainingY,
                            r,
                            in map,
                            ref result
                        );

                        if (slid)
                        {
                            result.DidMove      = true;
                            result.Slided       = true;
                            result.CornerAssist = true;
                        }
                    }

                    // 🔸 플레이어 중심 Y가 저장된 모서리 Y를 넘어서면 Lock 해제
                    //     - 위로 이동(signY > 0): y > cornerY
                    //     - 아래로 이동(signY < 0): y < cornerY
                    if (axisLockCornerY != 0)
                    {
                        if (signY > 0 && y > axisLockCornerY)
                        {
                            axisLock        = 0;
                            axisLockCornerY = 0;
                        }
                        else if (signY < 0 && y < axisLockCornerY)
                        {
                            axisLock        = 0;
                            axisLockCornerY = 0;
                        }
                    }

                    pos.x = x;
                    pos.y = y;
                    return result;
                }

                if (axisLock == 2) // Horizontal only (X축만)
                {
                    int r = col.radius;

                    int effectiveSteps = (int)math.round(math.sqrt(dx * dx + dy * dy));
                    if (effectiveSteps <= 0)
                        effectiveSteps = math.max(stepsX, stepsY);

                    int beforeX = x;

                    if (effectiveSteps > 0 && signX != 0)
                    {
                        MoveAxis(ref x, ref y, signX, effectiveSteps, vertical: false, r, in map, ref result);
                    }

                    int movedX     = math.abs(x - beforeX);
                    int remainingX = effectiveSteps - movedX;

                    if (result.HitWall && remainingX > 0)
                    {
                        bool slid = TryCornerSlideHorizontal(
                            ref x,
                            ref y,
                            remainingX,
                            r,
                            in map,
                            ref result
                        );

                        if (slid)
                        {
                            result.DidMove      = true;
                            result.Slided       = true;
                            result.CornerAssist = true;
                        }
                    }

                    // 🔸 플레이어 중심 X가 저장된 모서리 X를 넘어서면 Lock 해제
                    if (axisLockCornerX != 0)
                    {
                        if (signX > 0 && x > axisLockCornerX)
                        {
                            axisLock        = 0;
                            axisLockCornerX = 0;
                        }
                        else if (signX < 0 && x < axisLockCornerX)
                        {
                            axisLock        = 0;
                            axisLockCornerX = 0;
                        }
                    }

                    pos.x = x;
                    pos.y = y;
                    return result;
                }
            }

            // 여기부터는 axisLock == 0 이거나 (축 고정 없음),
            // 대각선이 아닌 경우(축 고정 해제 후) 일반 미로 + 코너 슬라이드 로직.

            bool primaryVertical = math.abs(dy) >= math.abs(dx);

            int radius = col.radius;

            // 1) primary 축 이동
            int beforePrimaryX = x;
            int beforePrimaryY = y;

            if (primaryVertical)
            {
                if (stepsY > 0 && signY != 0)
                    MoveAxis(ref x, ref y, signY, stepsY, vertical: true, radius, in map, ref result);
            }
            else
            {
                if (stepsX > 0 && signX != 0)
                    MoveAxis(ref x, ref y, signX, stepsX, vertical: false, radius, in map, ref result);
            }

            int movedPrimary = primaryVertical
                ? math.abs(y - beforePrimaryY)
                : math.abs(x - beforePrimaryX);

            int primarySteps     = primaryVertical ? stepsY : stepsX;
            int remainingPrimary = primarySteps - movedPrimary;

            // 2) 코너 슬라이드 (순수 primary 입력일 때만)
            bool didCornerSlide = false;

            if (result.HitWall && remainingPrimary > 0)
            {
                if (primaryVertical && stepsX == 0 && signY != 0)
                {
                    // 순수 위/아래 입력 → 좌/우 슬라이드 시도
                    didCornerSlide = TryCornerSlideVertical(
                        ref x, ref y,
                        remainingPrimary,
                        radius,
                        in map,
                        ref result);
                }
                else if (!primaryVertical && stepsY == 0 && signX != 0)
                {
                    // 순수 좌/우 입력 → 위/아래 슬라이드 시도
                    didCornerSlide = TryCornerSlideHorizontal(
                        ref x, ref y,
                        remainingPrimary,
                        radius,
                        in map,
                        ref result);
                }
            }

            if (didCornerSlide)
            {
                result.DidMove      = true;
                result.Slided       = true;
                result.CornerAssist = true;
            }
            else
            {
                // 3) secondary 축 이동 (고전적인 미로 스타일)
                if (primaryVertical)
                {
                    if (stepsX > 0 && signX != 0)
                        MoveAxis(ref x, ref y, signX, stepsX, vertical: false, radius, in map, ref result);
                }
                else
                {
                    if (stepsY > 0 && signY != 0)
                        MoveAxis(ref x, ref y, signY, stepsY, vertical: true, radius, in map, ref result);
                }
            }

            // 4) 대각선 입력인데 한 칸도 못 움직인 경우:
            //    - 충돌한 타일과의 관계(법선)를 보고, 한 축만 살려서 다시 한 번 이동 시도 + axisLock 설정
            if (!result.DidMove && result.HitWall && diagonalInput &&
                result.LastHitTileX >= 0 && result.LastHitTileY >= 0)
            {
                // 히트된 타일 AABB (fixed 좌표)
                int tileMinX = map.originX + result.LastHitTileX * map.tileSize;
                int tileMaxX = tileMinX + map.tileSize;
                int tileMinY = map.originY + result.LastHitTileY * map.tileSize;
                int tileMaxY = tileMinY + map.tileSize;

                int cx = startX;
                int cy = startY;

                // 정면 충돌인 경우: 축 선택 재시도 자체를 하지 않고 그대로 멈춘다.
                bool frontHitVertical =
                    primaryVertical &&
                    cx >= tileMinX && cx <= tileMaxX;
                bool frontHitHorizontal =
                    !primaryVertical &&
                    cy >= tileMinY && cy <= tileMaxY;

                if (!frontHitVertical && !frontHitHorizontal)
                {
                    // 🔸 타일 AABB에 대해 최근접점 계산 → 충돌 법선(normal) 추출
                    int closestX = math.clamp(cx, tileMinX, tileMaxX);
                    int closestY = math.clamp(cy, tileMinY, tileMaxY);

                    int nxPen = cx - closestX;
                    int nyPen = cy - closestY;

                    // 혹시라도 완전히 모서리 중앙에 걸렸다면(거의 없음) 축 선택은 primary 기준으로 처리
                    if (nxPen == 0 && nyPen == 0)
                    {
                        bool keepVerticalFallback = !primaryVertical; // primary 막고 반대 축 살리기

                        x = startX;
                        y = startY;
                        result = new KinematicMoveResult2D
                        {
                            LastHitTileX = -1,
                            LastHitTileY = -1
                        };

                        if (keepVerticalFallback)
                        {
                            axisLock = 1; // Y만
                            axisLockCornerY = (dy > 0) ? tileMinY : tileMaxY;

                            if (stepsY > 0 && signY != 0)
                                MoveAxis(ref x, ref y, signY, stepsY, vertical: true, radius, in map, ref result);
                        }
                        else
                        {
                            axisLock = 2; // X만
                            axisLockCornerX = (dx > 0) ? tileMinX : tileMaxX;

                            if (stepsX > 0 && signX != 0)
                                MoveAxis(ref x, ref y, signX, stepsX, vertical: false, radius, in map, ref result);
                        }

                        pos.x = x;
                        pos.y = y;
                        return result;
                    }

                    // 🔸 법선의 절대값 비교로 "어느 축이 더 막혔는지" 판정
                    int absNx = math.abs(nxPen);
                    int absNy = math.abs(nyPen);

                    bool keepVertical; // true면 Y축만, false면 X축만 살림

                    if (absNx > absNy)
                    {
                        // 수평 성분이 더 크다 → X축 쪽으로 더 많이 겹침 → X가 막힌 축 → Y만 살리기
                        keepVertical = true;
                        axisLock     = 1; // 이후 틱에서도 Y만 사용

                        axisLockCornerY = (dy > 0) ? tileMinY : tileMaxY;
                    }
                    else if (absNy > absNx)
                    {
                        // 수직 성분이 더 크다 → Y축 쪽으로 더 많이 겹침 → Y가 막힌 축 → X만 살리기
                        keepVertical = false;
                        axisLock     = 2; // 이후 틱에서도 X만 사용

                        axisLockCornerX = (dx > 0) ? tileMinX : tileMaxX;
                    }
                    else
                    {
                        // 둘 다 비슷하면 primary 축을 막힌 축으로 본다.
                        keepVertical = !primaryVertical;
                        axisLock     = keepVertical ? 1 : 2;

                        if (axisLock == 1)
                            axisLockCornerY = (dy > 0) ? tileMinY : tileMaxY;
                        else
                            axisLockCornerX = (dx > 0) ? tileMinX : tileMaxX;
                    }

                    x = startX;
                    y = startY;
                    result = new KinematicMoveResult2D
                    {
                        LastHitTileX = -1,
                        LastHitTileY = -1
                    };

                    if (keepVertical)
                    {
                        if (stepsY > 0 && signY != 0)
                            MoveAxis(ref x, ref y, signY, stepsY, vertical: true, radius, in map, ref result);
                    }
                    else
                    {
                        if (stepsX > 0 && signX != 0)
                            MoveAxis(ref x, ref y, signX, stepsX, vertical: false, radius, in map, ref result);
                    }
                }
            }

            pos.x = x;
            pos.y = y;
            return result;
        }

        /// <summary>
        /// 한 축에 대해 step 만큼 1칸씩 이동을 시도.
        /// - 중간에 벽을 만나면 그 축 이동은 그 자리에서 종료.
        /// - 한 칸이라도 이동하면 DidMove = true.
        /// - 중간에 막히면 HitWall = true, LastHitTileX/Y 갱신.
        /// </summary>
        private static void MoveAxis(
            ref int x,
            ref int y,
            int sign,
            int steps,
            bool vertical,
            int radius,
            in MapGrid2D map,
            ref KinematicMoveResult2D result)
        {
            if (sign == 0 || steps <= 0)
                return;

            for (int i = 0; i < steps; i++)
            {
                int nx = vertical ? x          : x + sign;
                int ny = vertical ? y + sign   : y;

                if (TileCollision2D.CheckCircle(
                        in map,
                        nx, ny,
                        radius,
                        ~0,
                        out int hitTileX,
                        out int hitTileY))
                {
                    result.HitWall      = true;
                    result.LastHitTileX = hitTileX;
                    result.LastHitTileY = hitTileY;
                    break;
                }

                x = nx;
                y = ny;
                result.DidMove = true;
            }
        }

        /// <summary>
        /// primary가 수직(위/아래)일 때의 코너 슬라이드:
        /// - 순수 위/아래 입력에서, 위/아래로 더 이상 갈 수 없을 때
        /// - 남은 step 을 좌/우로 흘려보내며 코너를 파고 나간다.
        /// - 타일의 가로폭 안에서 정면으로 박힌 경우에는 슬라이드를 하지 않는다.
        /// </summary>
        private static bool TryCornerSlideVertical(
            ref int x,
            ref int y,
            int remainingSteps,
            int radius,
            in MapGrid2D map,
            ref KinematicMoveResult2D result)
        {
            if (!result.HitWall || result.LastHitTileX < 0 || result.LastHitTileY < 0)
                return false;

            int tileMinX = map.originX + result.LastHitTileX * map.tileSize;
            int tileMaxX = tileMinX + map.tileSize;

            int cx = x;

            // 정면 충돌: X가 타일 가로폭 안이면 위/아래 평면에 박힌 것 → 슬라이드하지 않는다.
            if (cx >= tileMinX && cx <= tileMaxX)
                return false;

            int slideSign = (cx < tileMinX) ? -1 : +1;

            bool moved = false;

            for (int i = 0; i < remainingSteps; i++)
            {
                int nx = x + slideSign;
                int ny = y;

                if (TileCollision2D.CheckCircle(
                        in map,
                        nx, ny,
                        radius,
                        ~0,
                        out int _, out int _))
                {
                    break;
                }

                x = nx;
                y = ny;
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// primary가 수평(좌/우)일 때의 코너 슬라이드:
        /// - 순수 좌/우 입력에서, 좌/우로 더 이상 갈 수 없을 때
        /// - 남은 step 을 위/아래로 흘려보내며 코너를 파고 나간다.
        /// - 타일의 세로폭 안에서 정면으로 박힌 경우에는 슬라이드를 하지 않는다.
        /// </summary>
        private static bool TryCornerSlideHorizontal(
            ref int x,
            ref int y,
            int remainingSteps,
            int radius,
            in MapGrid2D map,
            ref KinematicMoveResult2D result)
        {
            if (!result.HitWall || result.LastHitTileX < 0 || result.LastHitTileY < 0)
                return false;

            int tileMinY = map.originY + result.LastHitTileY * map.tileSize;
            int tileMaxY = tileMinY + map.tileSize;

            int cy = y;

            // 정면 충돌: Y가 타일 세로폭 안이면 좌/우 평면에 박힌 것 → 슬라이드하지 않는다.
            if (cy >= tileMinY && cy <= tileMaxY)
                return false;

            int slideSign = (cy < tileMinY) ? -1 : +1;

            bool moved = false;

            for (int i = 0; i < remainingSteps; i++)
            {
                int nx = x;
                int ny = y + slideSign;

                if (TileCollision2D.CheckCircle(
                        in map,
                        nx, ny,
                        radius,
                        ~0,
                        out int _, out int _))
                {
                    break;
                }

                x = nx;
                y = ny;
                moved = true;
            }

            return moved;
        }
    }

    public struct KinematicMoveResult2D
    {
        public bool DidMove;
        public bool HitWall;
        public bool Slided;
        public bool CornerAssist;

        public int LastHitTileX;
        public int LastHitTileY;

        public static implicit operator bool(KinematicMoveResult2D r) => r.DidMove;
    }
}
