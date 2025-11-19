using ZenECS.Core;

namespace ZenECS.Physics.Components
{
    public struct FixedPosition2D
    {
        public int x, y;

        public FixedPosition2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public struct Velocity2D
    {
        public int vx, vy;
        public Velocity2D(int vx, int vy)
        {
            this.vx = vx;
            this.vy = vy;
        }
    }
    public struct MoveInput2D : IMessage
    {
        public int dx, dy;
        public MoveInput2D(int dx, int dy)
        {
            this.dx = dx;
            this.dy = dy;
        }
    }
    public struct MovementStats2D
    {
        public static readonly MovementStats2D Default = new MovementStats2D(3, 1000, 1.0f/30.0f);
        
        public float MoveSpeedUnityPerSecond;
        public int UnitsPerUnity;
        public float FixedDeltaTime;
        public float InterpolationTime;
        // 마지막으로 보간을 시작한 FixedPosition 저장
        public int LastFixedX;
        public int LastFixedY;
        
        public MovementStats2D(float moveSpeedUnityPerSecond,
            int unitsPerUnity,
            float fixedDeltaTime,
            float interpolationTime = 0,
            int lastFixedX = 0,
            int lastFixedY = 0)
        {
            MoveSpeedUnityPerSecond = moveSpeedUnityPerSecond;
            UnitsPerUnity = unitsPerUnity;
            FixedDeltaTime = fixedDeltaTime;
            InterpolationTime = interpolationTime;
            LastFixedX = lastFixedX;
            LastFixedY = lastFixedY;
        }

        public int GetSpeedPerTick(float fixedDeltaTime)
        {
            FixedDeltaTime = fixedDeltaTime;
            // Convert speed from units/second to units/tick (fixedDeltaSeconds)
            float speedUnitsPerSecond = MoveSpeedUnityPerSecond * UnitsPerUnity;
            return (int)(speedUnitsPerSecond * FixedDeltaTime);
        }
    }

    public struct CircleCollider2D
    {
        public int radius;
        public int layerMask;
        public bool isTrigger;
        public CircleCollider2D(int radius, int layerMask, bool isTrigger)
        {
            this.radius = radius;
            this.layerMask = layerMask;
            this.isTrigger = isTrigger;
        }
    }

    public struct KinematicBodyTag2D { }
    
    public struct Position2D
    {
        public float x;
        public float y;

        public Position2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Projectile
    {
        public Entity Owner;
        public int Damage;
        public int Radius;
        public int MaxDistance;
        public int Traveled;

        public Projectile(Entity owner, int damage, int radius, int maxDistance, int traveled)
        {
            Owner = owner;
            Damage = damage;
            Radius = radius;
            MaxDistance = maxDistance;
            Traveled = traveled;
        }
    }
    
    public struct FireProjectileRequest
    {
        public FixedPosition2D SpawnPos;
        public Velocity2D InitialVelocity;

        public int Damage;
        public int Radius;
        public int MaxDistance;

        public FireProjectileRequest(FixedPosition2D spawnPos, Velocity2D initialVelocity, int damage, int radius,
            int maxDistance)
        {
            SpawnPos = spawnPos;
            InitialVelocity = initialVelocity;
            Damage = damage;
            Radius = radius;
            MaxDistance = maxDistance;
        }
    }

    public struct HitEvent
    {
        public Entity Target;
        public Entity Source;
        public int Damage;
    }
}