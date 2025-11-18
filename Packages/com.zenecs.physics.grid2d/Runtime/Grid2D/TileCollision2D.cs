namespace ZenECS.Physics.Grid2D
{
    public static class TileCollision2D
    {
        public static bool CheckCircle(
            in MapGrid2D map,
            int cx, int cy,
            int radius)
        {
            if (map.collision == null || map.width <= 0 || map.height <= 0)
                return false;

            int tileSize = map.tileSize;
            int minTileX = (cx - radius - map.originX) / tileSize;
            int maxTileX = (cx + radius - map.originX) / tileSize;
            int minTileY = (cy - radius - map.originY) / tileSize;
            int maxTileY = (cy + radius - map.originY) / tileSize;

            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    if (!map.IsSolidTile(tx, ty))
                        continue;

                    if (CircleVsTile(cx, cy, radius, in map, tx, ty))
                        return true;
                }
            }

            return false;
        }

        static bool CircleVsTile(
            int cx, int cy, int radius,
            in MapGrid2D map,
            int tileX, int tileY)
        {
            int tileSize = map.tileSize;
            int minX = map.originX + tileX * tileSize;
            int minY = map.originY + tileY * tileSize;
            int maxX = minX + tileSize;
            int maxY = minY + tileSize;

            int closestX = ClampInt(cx, minX, maxX);
            int closestY = ClampInt(cy, minY, maxY);
            
            int dx = cx - closestX;
            int dy = cy - closestY;

            long dist2 = (long)dx * dx + (long)dy * dy;
            long r2 = (long)radius * radius;
            return dist2 < r2;
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
