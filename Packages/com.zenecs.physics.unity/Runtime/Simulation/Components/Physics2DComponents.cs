using Unity.Mathematics;
using ZenECS.Core;

namespace ZenECS.Physics.Unity.Simulation.Components
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
        public float InterpolationAlpha;
        // 마지막으로 보간을 시작한 FixedPosition 저장
        public int LastFixedX;
        public int LastFixedY;
        
        public MovementStats2D(float moveSpeedUnityPerSecond,
            int unitsPerUnity,
            float fixedDeltaTime,
            float interpolationAlpha = 0,
            int lastFixedX = 0,
            int lastFixedY = 0)
        {
            MoveSpeedUnityPerSecond = moveSpeedUnityPerSecond;
            UnitsPerUnity = unitsPerUnity;
            FixedDeltaTime = fixedDeltaTime;
            InterpolationAlpha = interpolationAlpha;
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
        public Entity Shooter;
        public float2 Direction;
        public float Speed;
        public float Damage;
        public float MaxDistance;
        public float Radius;

        public FireProjectileRequest(Entity shooter, float2 direction, float speed, float damage, float maxDistance, float radius)
        {
            Shooter = shooter;
            Direction = direction;
            Speed = speed;
            Damage = damage;
            MaxDistance = maxDistance;
            Radius = radius;
        }
    }

    public struct HitEvent
    {
        public EHitType HitType;
        public Entity Target;
        public Entity Source;
        public int Damage;
    }

    public struct DeadTag
    {
    }
}