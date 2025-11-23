// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Physics2D Components (Fixed 2D movement + projectiles)
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using Unity.Mathematics;
using ZenECS.Core;

namespace ZenECS.Physics.Unity.Simulation.Components
{
    /// <summary>
    /// Fixed-step 기준 2D 위치 (그리드/정수 좌표).
    /// </summary>
    public struct FixedPosition2D
    {
        public int x;
        public int y;

        public FixedPosition2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// Presentation / View 레이어에서 사용하는 float 기반 위치.
    /// </summary>
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

    /// <summary>
    /// Fixed-step 기준 속도 (틱당 이동량, 정수).
    /// </summary>
    public struct Velocity2D
    {
        public int vx;
        public int vy;

        public Velocity2D(int vx, int vy)
        {
            this.vx = vx;
            this.vy = vy;
        }
    }

    /// <summary>
    /// FrameInput 단계에서 기록되는 360도 이동 입력 상태.
    /// </summary>
    public struct MoveInput2DState : IWorldSingletonComponent
    {
        /// <summary>정규화된 2D 방향 벡터 (길이 0이면 입력 없음).</summary>
        public float2 Dir;

        /// <summary>조이스틱 기울기(0~1) 또는 축 길이.</summary>
        public float Magnitude;

        public static MoveInput2DState None => new MoveInput2DState
        {
            Dir = float2.zero,
            Magnitude = 0f
        };
    }

    /// <summary>
    /// FrameInput 단계에서 기록되는 발사 버튼 입력 상태.
    /// </summary>
    public struct FireInputState : IWorldSingletonComponent
    {
        /// <summary>이번 프레임에 발사 입력이 들어왔는지 여부.</summary>
        public bool Pressed;
    }

    /// <summary>
    /// 이동/보간/스케일 정보를 담는 2D 이동 스탯.
    /// </summary>
    public struct MovementStats2D
    {
        /// <summary>유니티 월드 단위 기준 초당 이동 속도.</summary>
        public float MoveSpeedUnityPerSecond;

        /// <summary>1 유니티 단위를 몇 fixed 단위로 볼지.</summary>
        public int UnitsPerUnity;

        /// <summary>고정 틱 간격(초).</summary>
        public float FixedDeltaTime;

        /// <summary>보간 알파 (0=이전 fixed, 1=현재 fixed).</summary>
        public float InterpolationAlpha;

        /// <summary>보간 기준이 되는 마지막 fixed 위치.</summary>
        public int LastFixedX;
        public int LastFixedY;

        /// <summary>디버그/통계용 입력 기록 (스케일된 방향값).</summary>
        public int MoveInputX;
        public int MoveInputY;

        public MovementStats2D(float moveSpeedUnityPerSecond, int unitsPerUnity, float fixedDeltaTime)
        {
            MoveSpeedUnityPerSecond = moveSpeedUnityPerSecond;
            UnitsPerUnity = unitsPerUnity;
            FixedDeltaTime = fixedDeltaTime;

            InterpolationAlpha = 0f;
            LastFixedX = 0;
            LastFixedY = 0;

            MoveInputX = 0;
            MoveInputY = 0;
        }

        public static readonly MovementStats2D Default =
            new MovementStats2D(3f, 1000, 1f / 30f);

        /// <summary>
        /// 현재 설정 기준 한 틱당 fixed 단위 속도를 계산한다.
        /// </summary>
        public int GetSpeedPerTick(float tickDeltaOverride = -1f)
        {
            float dt = tickDeltaOverride > 0f ? tickDeltaOverride : FixedDeltaTime;
            float unitsPerSecond = MoveSpeedUnityPerSecond * UnitsPerUnity;
            float unitsPerTick = unitsPerSecond * dt;
            return (int)math.round(unitsPerTick);
        }
    }

    /// <summary>
    /// Fixed 2D 회전 값 (도 단위, 0~359).
    /// </summary>
    public struct FixedRotation2D
    {
        public int AngleDeg;

        public FixedRotation2D(int angleDeg)
        {
            AngleDeg = NormalizeAngle(angleDeg);
        }

        public static int NormalizeAngle(int angle)
        {
            int a = angle % 360;
            if (a < 0) a += 360;
            return a;
        }

        /// <summary>forward 벡터로부터 AngleDeg를 설정.</summary>
        public void SetFromForward(float2 forward)
        {
            if (math.lengthsq(forward) < 1e-6f)
                return;

            float rad = math.atan2(forward.y, forward.x);
            float deg = math.degrees(rad);
            AngleDeg = NormalizeAngle((int)math.round(deg));
        }

        /// <summary>현재 회전 기준 forward 벡터를 반환.</summary>
        public float2 Forward()
        {
            float rad = math.radians(AngleDeg);
            return new float2(math.cos(rad), math.sin(rad));
        }
    }

    /// <summary>
    /// 원형 콜라이더(고정 좌표 기반).
    /// </summary>
    public struct CircleCollider2D
    {
        /// <summary>fixed 단위 반지름.</summary>
        public int radius;

        /// <summary>레이어 마스크 (타일/월드 필터용).</summary>
        public int layerMask;

        /// <summary>true이면 트리거 취급.</summary>
        public bool isTrigger;
        
        public  CircleCollider2D(int radius, int layerMask, bool isTrigger)
        {
            this.radius = radius;
            this.layerMask = layerMask;
            this.isTrigger = isTrigger;
        }
    }

    /// <summary>
    /// 피격 대상용 원형 히트박스 (Presentation과 다르게 단순 반지름만 사용).
    /// </summary>
    public struct HitboxCircle
    {
        /// <summary>fixed 단위 반지름.</summary>
        public int Radius;
    }

    /// <summary>
    /// 키네마틱 바디 태그 (캐릭터/발사체 등 "움직이는 애들" 필터용).
    /// </summary>
    public struct KinematicBodyTag2D
    {
    }

    /// <summary>
    /// 플레이어 식별용 태그.
    /// </summary>
    public struct PlayerTag
    {
    }

    /// <summary>
    /// 발사체 상태 및 파라미터.
    /// </summary>
    public struct Projectile
    {
        public Entity Owner;
        public int Damage;
        public int Radius;
        public int MaxDistance;
        public int Traveled;

        public Projectile(Entity owner, int damage, int radius, int maxDistance, int traveled = 0)
        {
            Owner = owner;
            Damage = damage;
            Radius = radius;
            MaxDistance = maxDistance;
            Traveled = traveled;
        }
    }

    /// <summary>
    /// 월드 내부에서 사용하는 피격 이벤트 (이벤트 엔티티에 붙음).
    /// </summary>
    public struct HitEvent
    {
        public Entity Target;
        public Entity Source;
        public int Damage;
    }

    /// <summary>
    /// 피격 이펙트용 충돌 위치/법선 정보.
    /// </summary>
    public struct ProjectileImpactInfo
    {
        public float2 Point;
        public float2 Normal;
    }

    /// <summary>
    /// 체력.
    /// </summary>
    public struct Health
    {
        public int Current;
        public int Max;
    }

    /// <summary>
    /// 엔티티가 제거 대상임을 표시하는 태그.
    /// </summary>
    public struct DeadTag
    {
    }
}
