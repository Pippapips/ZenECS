using ZenECS.Physics.Components;

namespace ZenECS.Physics.Grid2D
{
    public static class KinematicGridMove2D
    {
        public static bool MoveWithTileCollision(
            ref FixedPosition2D pos,
            in Velocity2D vel,
            in CircleCollider2D col,
            in MapGrid2D map)
        {
            bool changed = false;
            
            int newX = pos.x + vel.vx;
            if (!TileCollision2D.CheckCircle(map, newX, pos.y, col.radius))
            {
                pos.x = newX;
                changed = true;
            }

            int newY = pos.y + vel.vy;
            if (!TileCollision2D.CheckCircle(map, pos.x, newY, col.radius))
            {
                pos.y = newY;
                changed = true;
            }

            return changed;
        }
    }
}