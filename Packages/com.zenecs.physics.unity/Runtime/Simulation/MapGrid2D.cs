using ZenECS.Core;

namespace ZenECS.Physics.Unity.Simulation
{
    public struct MapGrid2D : IWorldSingletonComponent
    {
        public int width, height;
        public int tileSize;
        public int originX, originY;
        public byte[] collision;    // 0=Empty,1=Wall,...

        public bool IsSolidTile(int tx, int ty)
        {
            if (collision == null) return false;
            if (tx < 0 || ty < 0 || tx >= width || ty >= height) return false;
            return collision[tx + ty * width] != 0;
        }
    }
}