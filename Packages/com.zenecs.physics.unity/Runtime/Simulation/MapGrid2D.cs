using ZenECS.Core;

namespace ZenECS.Physics.Unity.Simulation
{
    public struct MapGrid2D : IWorldSingletonComponent
    {
        public int width, height;
        public int tileSize;
        public int originX, originY;
        public byte[] collision;    // 0=Empty,1=Wall,...
    }
}