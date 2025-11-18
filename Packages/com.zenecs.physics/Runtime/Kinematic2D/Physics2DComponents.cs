namespace ZenECS.Physics.Kinematic2D
{
    public struct Position2D  { public int x, y; }
    public struct Velocity2D  { public int vx, vy; }
    public struct MoveInput2D { public int dx, dy; }
    public struct MovementStats2D { public int speedPerTick; }

    public struct CircleCollider2D
    {
        public int radius;
        public int layerMask;
        public bool isTrigger;
    }

    public struct KinematicBodyTag2D { }
}