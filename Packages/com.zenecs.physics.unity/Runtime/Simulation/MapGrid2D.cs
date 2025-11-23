using ZenECS.Core;

namespace ZenECS.Physics.Unity.Simulation
{
    public struct MapGrid2D : IWorldSingletonComponent
    {
        public int width, height;
        public int tileSize;
        public int originX, originY;
        public byte[] collision;    // 0=Empty,1=Wall,...

        public bool IsWall(int tx, int ty)
        {
            if (collision == null) return false;
            if (tx < 0 || ty < 0 || tx >= width || ty >= height) return false;
            return collision[tx + ty * width] != 0;
        }
        
        /// <summary>
        /// 월드 고정 좌표 (worldX, worldY)를 받아서
        /// 내부 타일 인덱스로 변환한 뒤 벽인지 검사한다.
        /// </summary>
        public bool IsWallAtWorld(int worldX, int worldY)
        {
            if (collision == null) return false;
            if (width <= 0 || height <= 0 || tileSize <= 0) return false;

            // 타일 인덱스 = (world - origin) / tileSize
            int tx = (worldX - originX) / tileSize;
            int ty = (worldY - originY) / tileSize;

            if (tx < 0 || ty < 0 || tx >= width || ty >= height) return false;
            var idx = tx + ty * width;
            return collision[idx] != 0;
        }
    }
}