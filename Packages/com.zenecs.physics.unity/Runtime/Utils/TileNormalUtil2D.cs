// ──────────────────────────────────────────────────────────────────────────────
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
}
