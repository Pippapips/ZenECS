namespace ZenECS.Physics.Unity.Simulation
{
    /// <summary>
    /// 타일맵 기반 2D 충돌 검사 유틸리티.
    /// </summary>
    public static class TileCollision2D
    {
        /// <summary>
        /// (구버전 시그니처)
        /// 원 중심 (cx, cy), 반지름 radius가
        /// 맵의 어떤 "solid" 타일과도 충돌하는지 검사한다.
        /// 
        /// collision 배열의 값이 0이 아니면 모두 충돌 대상으로 본다.
        /// layerMask 없이 전체 레이어를 다 보고 싶을 때 사용.
        /// </summary>
        public static bool CheckCircle(
            in MapGrid2D map,
            int cx, int cy,
            int radius)
        {
            // 모든 레이어 활성 (~0)
            return CheckCircle(in map, cx, cy, radius, ~0);
        }

        /// <summary>
        /// (신규 시그니처)
        /// 원 중심 (cx, cy), 반지름 radius가
        /// 맵의 어떤 "solid" 타일과도 충돌하는지 검사한다.
        /// 
        /// - map.collision[tx + ty * width] 값은 "타일의 레이어 플래그"라고 가정한다.
        ///   (예: bit0 = Ground, bit1 = Wall, bit2 = Water ...)
        /// - layerMask 와 AND 연산을 해서 0이 아니면 충돌 대상으로 간주한다.
        /// - layerMask 를 ~0 로 주면 모든 레이어를 대상으로 검사한다.
        /// 
        /// 충돌 조건:
        /// - 맵이 비어있으면 false
        /// - 타일 bounding box와 원의 최근접점만 계산 (solid 타일에만)
        /// - dist^2 <= radius^2 인 경우 충돌
        /// </summary>
        public static bool CheckCircle(
            in MapGrid2D map,
            int cx, int cy,
            int radius,
            int layerMask)
        {
            if (map.collision == null || map.width <= 0 || map.height <= 0)
                return false;

            int tileSize = map.tileSize;
            if (tileSize <= 0)
                return false;

            // 원의 AABB가 덮는 타일 범위 계산 (예측된 P_next의 영향 영역)
            int minTileX = (cx - radius - map.originX) / tileSize;
            int maxTileX = (cx + radius - map.originX) / tileSize;
            int minTileY = (cy - radius - map.originY) / tileSize;
            int maxTileY = (cy + radius - map.originY) / tileSize;

            // 맵 범위로 클램프
            if (minTileX < 0) minTileX = 0;
            if (minTileY < 0) minTileY = 0;
            if (maxTileX >= map.width) maxTileX = map.width - 1;
            if (maxTileY >= map.height) maxTileY = map.height - 1;

            if (minTileX > maxTileX || minTileY > maxTileY)
                return false;

            long r2 = (long)radius * radius;

            // 바깥 for 루프를 Y 기준으로 두는 건 캐시 지역성 측면에서 무난한 선택.
            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                int rowOffset = ty * map.width;

                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    int tileFlags = map.collision[rowOffset + tx];

                    // 완전 빈 타일은 스킵
                    if (tileFlags == 0)
                        continue;

                    // layerMask와 AND 했을 때 0이면 이 레이어에선 충돌 대상 아님
                    if ((tileFlags & layerMask) == 0)
                        continue;

                    // 타일의 world 좌표 AABB
                    int tileMinX = map.originX + tx * tileSize;
                    int tileMinY = map.originY + ty * tileSize;
                    int tileMaxX = tileMinX + tileSize;
                    int tileMaxY = tileMinY + tileSize;

                    // 원 중심에서 타일 AABB에 대한 최근접점
                    int closestX = ClampInt(cx, tileMinX, tileMaxX);
                    int closestY = ClampInt(cy, tileMinY, tileMaxY);

                    int dx = cx - closestX;
                    int dy = cy - closestY;

                    long dist2 = (long)dx * dx + (long)dy * dy;

                    // 딱 닿는 것도 충돌로 간주 (<=)
                    if (dist2 <= r2)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Simple integer clamp, to avoid depending on UnityEngine.Mathf.
        /// </summary>
        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
