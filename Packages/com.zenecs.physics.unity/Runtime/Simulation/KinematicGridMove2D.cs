using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation
{
    public enum EHitType
    {
        None,
        MapWall
    }
    
    public static class KinematicGridMove2D
    {
        public static MoveResult MoveWithTileCollision(ref FixedPosition2D pos, in Velocity2D vel, in CircleCollider2D col, in MapGrid2D map)
        {
            bool moved = false;
            bool hitWall = false;

            int newX = pos.x + vel.vx;
            if (!TileCollision2D.CheckCircle(map, newX, pos.y, col.radius))
            {
                pos.x = newX;
                moved = true;
            }
            else
            {
                hitWall = true;
            }

            int newY = pos.y + vel.vy;
            if (!TileCollision2D.CheckCircle(map, pos.x, newY, col.radius))
            {
                pos.y = newY;
                moved = true;
            }
            else hitWall = true;
            
            return new MoveResult
            {
                moved = moved,
                hitWall = hitWall
            };
        }

        public struct MoveResult
        {
            public bool moved;
            public bool hitWall;
        }
    }
}