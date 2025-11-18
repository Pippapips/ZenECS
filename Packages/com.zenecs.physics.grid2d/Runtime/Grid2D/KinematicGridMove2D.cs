namespace ZenECS.Physics.Grid2D
{
    public static class KinematicGridMove2D
    {
        public static void MoveWithTileCollision(
            ref ZenECS.Physics.Kinematic2D.Position2D pos,
            in ZenECS.Physics.Kinematic2D.Velocity2D vel,
            in ZenECS.Physics.Kinematic2D.CircleCollider2D col,
            in MapGrid2D map)
        {
            int newX = pos.x + vel.vx;
            if (!TileCollision2D.CheckCircle(map, newX, pos.y, col.radius))
                pos.x = newX;

            int newY = pos.y + vel.vy;
            if (!TileCollision2D.CheckCircle(map, pos.x, newY, col.radius))
                pos.y = newY;
        }
    }
}