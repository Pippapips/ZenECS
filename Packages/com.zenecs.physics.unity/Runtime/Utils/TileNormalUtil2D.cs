﻿// ──────────────────────────────────────────────────────────────────────────────
// TileNormalUtil2D: 타일맵 기반 벽 법선 추정
// KinematicGridMove2D: 브롤스타즈 스타일 슬라이딩 이동
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using Unity.Mathematics;
using ZenECS.Physics.Unity.Simulation;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    public static class TileNormalUtil2D
    {
        /// <summary>
        /// 타일맵 기반 Circle 이동이 막혔을 때 대략적인 벽 법선을 추정한다.
        ///
        /// 구현 아이디어:
        ///  1) targetX, targetY, radius 를 기준으로 Circle이 실제로 겹칠 수 있는
        ///     타일 인덱스 범위(minTileX~maxTileX, minTileY~maxTileY)를 계산한다.
        ///  2) 그 범위 안에서 Circle과 실제로 겹치는 타일만 골라내고,
        ///     타일 중심 → 원 중심 방향 벡터를 normal 에 누적한다.
        ///  3) 누적된 normal 을 정규화해서 사용한다.
        ///
        /// 이렇게 하면 코너/모서리 충돌에서도 실제로 겹친 타일들의 기여로
        /// 자연스럽게 대각선 법선(예: (-1,-1))이 나와서
        /// "위로 밀면서 왼쪽으로 미끄러지는" 슬라이딩이 잘 나온다.
        /// </summary>
        public static float2 EstimateWallNormal(
            MapGrid2D map,
            in CircleCollider2D col,
            int startX, int startY,
            int targetX, int targetY)
        {
            float2 normal = float2.zero;
            int radius = col.radius;

            if (map.collision == null || map.width <= 0 || map.height <= 0 || map.tileSize <= 0)
                return normal;

            int tileSize = map.tileSize;

            // Circle(targetX, targetY, radius)가 겹칠 수 있는 타일 범위
            int minTileX = (targetX - radius - map.originX) / tileSize;
            int maxTileX = (targetX + radius - map.originX) / tileSize;
            int minTileY = (targetY - radius - map.originY) / tileSize;
            int maxTileY = (targetY + radius - map.originY) / tileSize;

            if (minTileX < 0) minTileX = 0;
            if (minTileY < 0) minTileY = 0;
            if (maxTileX >= map.width)  maxTileX = map.width  - 1;
            if (maxTileY >= map.height) maxTileY = map.height - 1;

            if (minTileX > maxTileX || minTileY > maxTileY)
                return normal;

            long r2 = (long)radius * radius;

            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                int rowOffset = ty * map.width;

                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    int tileFlags = map.collision[rowOffset + tx];
                    if (tileFlags == 0)
                        continue; // 빈 타일

                    // 이 타일의 world AABB
                    int tileMinX = map.originX + tx * tileSize;
                    int tileMinY = map.originY + ty * tileSize;
                    int tileMaxX = tileMinX + tileSize;
                    int tileMaxY = tileMinY + tileSize;

                    // Circle vs AABB 최근접점
                    int closestX = ClampInt(targetX, tileMinX, tileMaxX);
                    int closestY = ClampInt(targetY, tileMinY, tileMaxY);
                    int dx = targetX - closestX;
                    int dy = targetY - closestY;
                    long dist2 = (long)dx * dx + (long)dy * dy;

                    // 실제로 Circle과 이 타일이 겹쳤는지 확인
                    if (dist2 > r2)
                        continue;

                    // 타일 중심 기준으로 원 중심이 어느 쪽에 있는지
                    int tileCenterX = tileMinX + tileSize / 2;
                    int tileCenterY = tileMinY + tileSize / 2;
                    float2 toCircle = new float2(targetX - tileCenterX, targetY - tileCenterY);

                    if (math.lengthsq(toCircle) < 1e-4f)
                        continue;

                    normal += math.normalize(toCircle);
                }
            }

            // 누적된 normal을 정규화
            if (math.lengthsq(normal) > 0.5f)
            {
                normal = math.normalize(normal);
            }
            else
            {
                // 혹시라도 아무 타일도 안 잡혔거나 너무 애매하면
                // 이동 반대 방향을 fallback 법선으로 사용
                int dx = targetX - startX;
                int dy = targetY - startY;

                if (dx != 0 || dy != 0)
                {
                    normal = math.normalize(new float2(-dx, -dy));
                }
            }

            return normal;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public enum KinematicMoveOptions2D
    {
        Default,
        CharacterStrong,
        Projectile
    }

    public struct KinematicMoveResult2D
    {
        public bool DidMove;
        public bool HitWall;
        public bool Slided;

        public static implicit operator bool(KinematicMoveResult2D r) => r.DidMove;
    }

    public static class KinematicGridMove2D
    {
        /// <summary>
        /// 브롤스타즈 라이크 2D 키네마틱 이동/충돌 + 슬라이딩 처리.
        ///
        /// 물리 단계:
        ///  1) 예측(Prediction)
        ///     - P_next = P_current + V
        ///
        ///  2) 충돌 감지(Detection)
        ///     - Circle(P_next, radius)가 타일과 겹치는지 확인
        ///
        ///  3) 충돌 해결(Resolution & Sliding)
        ///     - 법선 N 계산 (EstimateWallNormal)
        ///     - 침투 성분 V_pen = dot(V,N) * N 제거
        ///     - 슬라이딩 벡터 V_slide = V - V_pen
        ///
        ///  4) 위치 갱신(Position Update)
        ///     - P_current + V_slide 에 대해 다시 충돌 검사
        ///     - 살짝 작은 반지름으로 검사해서 "벽에 딱 붙어 미끄러지는" 상태를 허용
        /// </summary>
        public static KinematicMoveResult2D MoveWithTileCollision(
            ref FixedPosition2D pos,
            in Velocity2D vel,
            in CircleCollider2D col,
            in MapGrid2D map,
            KinematicMoveOptions2D options)
        {
            var result = new KinematicMoveResult2D();

            // 0) 속도가 없으면 아무 것도 안 함
            if (vel.vx == 0 && vel.vy == 0)
                return result;

            int startX = pos.x;
            int startY = pos.y;
            int dx = vel.vx;
            int dy = vel.vy;

            // 1) 예측 위치
            int targetX = startX + dx;
            int targetY = startY + dy;

            // 2) 정면 이동 시도 (정면은 "원래 반지름"으로 강하게 체크)
            if (!TileCollision2D.CheckCircle(in map, targetX, targetY, col.radius, col.layerMask))
            {
                pos.x = targetX;
                pos.y = targetY;
                result.DidMove = true;
                return result;
            }

            // 정면 막힘
            result.HitWall = true;

            // 3) 벽 법선 추정
            float2 normal = TileNormalUtil2D.EstimateWallNormal(
                map, in col, startX, startY, targetX, targetY);

            if (math.lengthsq(normal) < 0.5f)
                return result;

            // CharacterStrong 옵션에서 "진짜 정면 박치기"면 슬라이딩 끄기
            if (options == KinematicMoveOptions2D.CharacterStrong)
            {
                float2 vNorm = math.normalize(new float2(dx, dy));
                float cosTheta = math.dot(vNorm, normal); // 1이면 거의 정면

                if (cosTheta > 0.98f)
                    return result;
            }

            // 3-2) 침투 성분 제거 → 슬라이딩 벡터
            float2 v = new float2(dx, dy);
            float d = math.dot(v, normal);
            float2 vPenetration = d * normal;
            float2 vSlide = v - vPenetration;

// 슬라이딩 성분이 너무 작으면 그냥 정지
            if (math.lengthsq(vSlide) < 0.5f)
                return result;

            int sdx = (int)math.round(vSlide.x);
            int sdy = (int)math.round(vSlide.y);

// 아무 것도 안 나오면 이동 안 함
            if (sdx == 0 && sdy == 0)
                return result;

// 4) 슬라이딩 경로를 따라 "한 칸씩" 이동하며, 충돌 직전까지 전진
//    (큰 점프 한 번이 아니라, 선분을 따라 최대한 멀리 이동)

            int slideRadius = col.radius;

// 슬라이딩일 때는 radius를 살짝 줄여서
// "벽에 딱 붙어 타는" 상태를 허용해준다.
            const int minMargin = 1;
            int margin = math.max(minMargin, col.radius / 5);
            if (slideRadius > margin)
                slideRadius -= margin;

// DDA/Bresenham-style stepping
            int stepCount = math.max(math.abs(sdx), math.abs(sdy));
            if (stepCount <= 0)
                return result;

            int x = startX;
            int y = startY;
            int lastSafeX = startX;
            int lastSafeY = startY;
            bool anyMoved = false;

            int absSdx = math.abs(sdx);
            int absSdy = math.abs(sdy);
            int signX = sdx > 0 ? 1 : (sdx < 0 ? -1 : 0);
            int signY = sdy > 0 ? 1 : (sdy < 0 ? -1 : 0);

// 누적 오차로 비율 맞추는 간단 DDA
            int accX = 0;
            int accY = 0;

            for (int i = 0; i < stepCount; i++)
            {
                // X 쪽 증가 여부
                accX += absSdx;
                if (accX >= stepCount)
                {
                    accX -= stepCount;
                    x += signX;
                }

                // Y 쪽 증가 여부
                accY += absSdy;
                if (accY >= stepCount)
                {
                    accY -= stepCount;
                    y += signY;
                }

                // 이 intermediate 지점이 슬라이딩해서 가고자 하는 후보 위치
                if (!TileCollision2D.CheckCircle(in map, x, y, slideRadius, col.layerMask))
                {
                    lastSafeX = x;
                    lastSafeY = y;
                    anyMoved = true;
                }
                else
                {
                    // 여기서부터는 벽으로 들어가는 영역이므로 루프 중단
                    break;
                }
            }

// stepCount 동안 한 칸도 못 갔으면 정지
            if (!anyMoved)
                return result;

// 최대한 갈 수 있는 마지막 안전 위치로 이동
            pos.x = lastSafeX;
            pos.y = lastSafeY;
            result.DidMove = true;
            result.Slided = true;
            return result;

        }
    }
}
