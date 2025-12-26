# ZenECS Core 개발자 가이드

> ZenECS Core를 처음 사용하는 개발자를 위한 종합 가이드

이 가이드는 ZenECS Core를 처음 접하는 개발자가 체계적으로 학습할 수 있도록 구성되었습니다. 기본 개념부터 실전 예제까지 단계별로 설명합니다.

## 목차

1. [ZenECS Core란?](#zenecs-core란)
2. [설치 및 설정](#설치-및-설정)
3. [핵심 개념 이해하기](#핵심-개념-이해하기)
4. [첫 번째 프로그램 작성하기](#첫-번째-프로그램-작성하기)
5. [실전 예제](#실전-예제)
6. [고급 기능](#고급-기능)
7. [베스트 프랙티스](#베스트-프랙티스)
8. [문제 해결](#문제-해결)

---

## ZenECS Core란?

**ZenECS Core**는 순수 C#으로 작성된 **Entity-Component-System (ECS)** 프레임워크입니다. Unity나 다른 게임 엔진에 의존하지 않으며, 어떤 .NET 환경에서도 사용할 수 있습니다.

### ECS 패러다임

ECS는 게임 개발에서 널리 사용되는 아키텍처 패턴입니다:

- **Entity (엔티티)**: 게임 내 객체를 나타내는 고유한 ID
- **Component (컴포넌트)**: 데이터를 저장하는 구조체 (예: Position, Health)
- **System (시스템)**: 컴포넌트를 조회하고 로직을 실행하는 클래스 (예: MoveSystem, RenderSystem)

### ZenECS의 주요 특징

- ✅ **멀티 월드 지원**: 여러 독립적인 시뮬레이션 공간을 동시에 관리
- ✅ **성능 최적화**: 타입별 컴포넌트 풀링, 효율적인 쿼리 시스템
- ✅ **결정론적 실행**: Fixed-step 시뮬레이션으로 재현 가능한 결과
- ✅ **의존성 없음**: 외부 프레임워크 없이 동작하는 경량 런타임
- ✅ **스레드 안전**: 멀티스레드 환경에서 안전하게 사용 가능

### 언제 사용하면 좋을까?

- 🎮 게임 개발 (Unity, Godot, 커스텀 엔진)
- 🤖 시뮬레이션 및 AI 엔진
- 🔬 데이터 지향 프로그래밍이 필요한 프로젝트
- ⚡ 성능이 중요한 애플리케이션

---

## 설치 및 설정

### Unity 프로젝트 (UPM)

Unity 프로젝트의 `Packages/manifest.json`에 다음을 추가합니다:

```json
{
  "dependencies": {
    "com.zenecs.core": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0"
  }
}
```

### .NET 프로젝트 (NuGet)

```bash
dotnet add package ZenECS.Core --version 1.0.0
```

또는 `.csproj` 파일에 직접 추가:

```xml
<ItemGroup>
  <PackageReference Include="ZenECS.Core" Version="1.0.0" />
</ItemGroup>
```

### 네임스페이스

주요 네임스페이스를 import합니다:

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Core.Config;
```

---

## 핵심 개념 이해하기

### 1. Kernel (커널)

**Kernel**은 여러 월드를 관리하는 최상위 관리자입니다. 게임 루프의 틱을 조율하고, 월드를 생성/삭제합니다.

```csharp
// 커널 생성
var kernel = new Kernel();

// 또는 옵션을 사용하여 생성
var kernel = new Kernel(new KernelOptions
{
    AutoSelectNewWorld = true,  // 새 월드를 자동으로 선택
    StepOnlyCurrentWhenSelected = false
});
```

### 2. World (월드)

**World**는 하나의 시뮬레이션 공간입니다. 모든 ECS 기능(엔티티, 컴포넌트, 시스템 등)을 제공하는 단일 API입니다.

```csharp
// 월드 생성
var world = kernel.CreateWorld(null, "GameWorld");

// 현재 월드 설정
kernel.SetCurrentWorld(world);
```

### 3. Entity (엔티티)

**Entity**는 컴포넌트를 담는 컨테이너입니다. 단순한 ID로 표현되며, 자체 데이터는 포함하지 않습니다.

```csharp
// 엔티티 생성
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
}

// 엔티티가 살아있는지 확인
if (world.IsAlive(player))
{
    // 엔티티가 살아있음
}

// 엔티티 삭제
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(player);
}
```

### 4. Component (컴포넌트)

**Component**는 데이터를 저장하는 구조체입니다. 반드시 `struct`로 정의해야 하며, 불변성(`readonly struct`)을 권장합니다.

```csharp
// 컴포넌트 정의
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// 태그 컴포넌트 (데이터 없음, 상태 표시용)
public readonly struct Paused { }
public readonly struct Player { }
```

### 5. System (시스템)

**System**은 게임 로직을 실행하는 클래스입니다. 특정 컴포넌트를 가진 엔티티를 조회하고 처리합니다.

시스템은 실행 시점에 따라 여러 인터페이스를 구현할 수 있습니다:

- `IFrameSetupSystem`: 프레임당 한 번 초기화
- `IFixedSetupSystem`: 고정 스텝당 한 번 초기화
- `IVariableRunSystem`: 변수 타임스텝 실행 (BeginFrame)
- `IFixedRunSystem`: 고정 타임스텝 실행 (FixedStep)
- `IPresentationSystem`: 프레젠테이션 단계 (LateFrame)

시스템은 그룹으로 분류됩니다:

- `[SimulationGroup]`: 시뮬레이션 로직 (물리, 이동, AI 등)
- `[PresentationGroup]`: 렌더링, 사운드 등 프레젠테이션

### 6. Query (쿼리)

**Query**는 특정 컴포넌트를 가진 엔티티를 효율적으로 조회하는 방법입니다.

```csharp
// 단일 컴포넌트 조회
foreach (var entity in world.Query<Position>())
{
    var pos = world.Get<Position>(entity);
}

// 여러 컴포넌트 조회
foreach (var entity in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Ref<Position>(entity);
    var vel = world.Get<Velocity>(entity);
}
```

---

## 첫 번째 프로그램 작성하기

이제 실제로 동작하는 프로그램을 만들어보겠습니다. 엔티티가 이동하는 간단한 시뮬레이션을 구현합니다.

### 1단계: 프로젝트 설정

```csharp
using System;
using System.Diagnostics;
using ZenECS.Core;
using ZenECS.Core.Config;
using ZenECS.Core.Systems;
```

### 2단계: 컴포넌트 정의

```csharp
// 위치 컴포넌트
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
    public override string ToString() => $"({X:0.##}, {Y:0.##})";
}

// 속도 컴포넌트
public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}
```

### 3단계: 시스템 작성

```csharp
// 이동 시스템: Position += Velocity * dt
[SimulationGroup]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Ref<Position>(entity);
            var vel = world.Get<Velocity>(entity);
            
            pos = new Position(
                pos.X + vel.X * fixedDelta,
                pos.Y + vel.Y * fixedDelta
            );
        }
    }
}

// 프레젠테이션 시스템: 위치 출력
[PresentationGroup]
public sealed class PrintPositionsSystem : IPresentationSystem
{
    public void Run(IWorld world, float dt, float alpha)
    {
        foreach (var entity in world.Query<Position>())
        {
            var pos = world.ReadComponent<Position>(entity); // 읽기 전용
            Console.WriteLine($"Entity {entity.Id}: {pos}");
        }
    }
}
```

### 4단계: 메인 프로그램

```csharp
class Program
{
    static void Main()
    {
        Console.WriteLine("=== ZenECS Core 예제 ===");

        // 1. 커널 및 월드 생성
        var kernel = new Kernel();
        var world = kernel.CreateWorld(null, "GameWorld");
        kernel.SetCurrentWorld(world);

        // 2. 시스템 등록
        world.AddSystems([
            new MoveSystem(),
            new PrintPositionsSystem()
        ]);

        // 3. 엔티티 및 컴포넌트 생성
        Entity entity1, entity2;
        using (var cmd = world.BeginWrite())
        {
            entity1 = cmd.CreateEntity();
            cmd.AddComponent(entity1, new Position(0, 0));
            cmd.AddComponent(entity1, new Velocity(1, 0)); // X축으로 초당 1 이동
            
            entity2 = cmd.CreateEntity();
            cmd.AddComponent(entity2, new Position(2, 1));
            cmd.AddComponent(entity2, new Velocity(0, -0.5f)); // Y축으로 초당 -0.5 이동
        }

        // 4. 게임 루프
        const float fixedDelta = 1f / 60f; // 60Hz 시뮬레이션
        var stopwatch = Stopwatch.StartNew();
        double previousTime = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine("실행 중... 아무 키나 누르면 종료됩니다.");
        
        while (true)
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(intercept: true);
                break;
            }

            double currentTime = stopwatch.Elapsed.TotalSeconds;
            float deltaTime = (float)(currentTime - previousTime);
            previousTime = currentTime;

            // 프레임 업데이트: BeginFrame → FixedStep×N → LateFrame
            const int maxSubStepsPerFrame = 4;
            kernel.PumpAndLateFrame(deltaTime, fixedDelta, maxSubStepsPerFrame);

            Thread.Sleep(1); // CPU 사용량 감소
        }

        // 5. 정리
        Console.WriteLine("종료 중...");
        kernel.Dispose();
        Console.WriteLine("완료.");
    }
}
```

### 실행 결과

```
=== ZenECS Core 예제 ===
실행 중... 아무 키나 누르면 종료됩니다.
Entity   1: (0.017, 0.000)
Entity   2: (2.000, 0.992)
Entity   1: (0.033, 0.000)
Entity   2: (2.000, 0.983)
...
```

---

## 실전 예제

### 예제 1: 생명력 시스템

플레이어와 적의 생명력을 관리하는 시스템을 만들어봅시다.

```csharp
// 컴포넌트 정의
public readonly struct Health
{
    public readonly int Current;
    public readonly int Max;
    public Health(int current, int max)
    {
        Current = current;
        Max = max;
    }
    public Health TakeDamage(int amount)
    {
        return new Health(Math.Max(0, Current - amount), Max);
    }
}

public readonly struct Player { }
public readonly struct Enemy { }

// 데미지 메시지
public readonly struct DamageMessage : IMessage
{
    public readonly Entity Target;
    public readonly int Amount;
    public DamageMessage(Entity target, int amount)
    {
        Target = target;
        Amount = amount;
    }
}

// 데미지 처리 시스템
[SimulationGroup]
public sealed class DamageSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // 메시지 버스를 통해 데미지 처리
        world.PumpMessages<DamageMessage>(message =>
        {
            if (world.IsAlive(message.Target) && 
                world.HasComponent<Health>(message.Target))
            {
                var currentHealth = world.ReadComponent<Health>(message.Target);
                var newHealth = currentHealth.TakeDamage(message.Amount);
                using (var cmd = world.BeginWrite())
                {
                    cmd.ReplaceComponent(message.Target, newHealth);
                }

                if (newHealth.Current <= 0)
                {
                    using (var cmd = world.BeginWrite())
                    {
                        cmd.DestroyEntity(message.Target);
                    }
                    Console.WriteLine($"Entity {message.Target.Id} destroyed!");
                }
            }
        });
    }
}

// 사용 예시
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
}
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(player, new Health(100, 100));
    cmd.AddComponent(player, new Player());
}

// 데미지 주기
world.Publish(new DamageMessage(player, 25));
```

### 예제 2: 필터링 쿼리

특정 조건을 만족하는 엔티티만 조회하는 방법입니다.

```csharp
using ZenECS.Core.Filters;

// 필터 생성: Position과 Velocity는 있어야 하고, Paused는 없어야 함
var filter = Filter.New
    .With<Position>()
    .With<Velocity>()
    .Without<Paused>()
    .Build();

// 필터를 사용하여 쿼리
foreach (var entity in world.Query<Position, Velocity>(filter))
{
    // 일시정지되지 않은 엔티티만 처리
}
```

### 예제 3: Command Buffer 사용

시스템 실행 중 구조적 변경(엔티티 생성/삭제, 컴포넌트 추가/제거)을 안전하게 처리합니다.

```csharp
[SimulationGroup]
public sealed class SpawnSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // Command Buffer를 사용하여 구조적 변경 버퍼링
        using (var cmd = world.BeginWrite())
        {
            // 엔티티 생성 및 컴포넌트 추가
            var newEntity = cmd.CreateEntity();
            cmd.AddComponent(newEntity, new Position(0, 0));
            cmd.AddComponent(newEntity, new Velocity(1, 0));
        } // 자동으로 적용됨
    }
}
```

---

## 고급 기능

### 1. 시스템 순서 제어

시스템 실행 순서를 명시적으로 지정할 수 있습니다.

```csharp
[SimulationGroup]
[OrderAfter(typeof(PhysicsSystem))]  // PhysicsSystem 이후 실행
[OrderBefore(typeof(RenderSystem))]   // RenderSystem 이전 실행
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // 물리 계산 후, 렌더링 전에 실행됨
    }
}
```

### 2. 월드 리셋

월드를 빠르게 리셋하여 모든 엔티티를 제거할 수 있습니다.

```csharp
// 월드 리셋 (용량은 유지)
world.Reset();

// 또는 완전히 새로운 월드 생성
var newWorld = kernel.CreateWorld(null, "NewGameWorld");
```

### 3. 스냅샷 저장/로드

월드 상태를 저장하고 나중에 복원할 수 있습니다.

```csharp
// 저장
using (var stream = File.Create("save.dat"))
{
    world.Save(stream, new BinaryComponentFormatter());
}

// 로드
using (var stream = File.OpenRead("save.dat"))
{
    var migrations = new List<IPostLoadMigration>();
    world.Load(stream, new BinaryComponentFormatter(), migrations);
}
```

### 4. 훅과 검증기

컴포넌트 쓰기 권한을 제어하거나 값을 검증할 수 있습니다.

```csharp
// 쓰기 권한 체크
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // 특정 엔티티나 컴포넌트 타입에 대한 쓰기 권한 제어
    return componentType != typeof(GodMode);
});

// 값 검증
world.Hooks.AddValidator<Health>(health =>
{
    // Health는 0 이상이어야 함
    return health.Current >= 0 && health.Current <= health.Max;
});
```

---

## 베스트 프랙티스

### 1. 컴포넌트 설계

✅ **좋은 예:**
```csharp
// 작고 명확한 책임
public readonly struct Position { public readonly float X, Y; }
public readonly struct Velocity { public readonly float X, Y; }
public readonly struct Health { public readonly int Current, Max; }
```

❌ **나쁜 예:**
```csharp
// 너무 많은 책임을 가진 컴포넌트
public readonly struct PlayerData 
{ 
    public readonly float X, Y, VelX, VelY; 
    public readonly int Health, Mana, Level; 
    // ... 너무 많은 필드
}
```

### 2. 시스템 설계

✅ **좋은 예:**
```csharp
[SimulationGroup]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            // 단일 책임: 이동만 처리
        }
    }
}
```

❌ **나쁜 예:**
```csharp
[SimulationGroup]
public sealed class EverythingSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // 이동, 물리, AI, 렌더링을 모두 처리 ❌
    }
}
```

### 3. 성능 최적화

- **컴포넌트는 작게 유지**: 큰 구조체보다 작은 구조체 여러 개를 권장
- **읽기 전용 접근 사용**: `ReadComponent<T>()`는 쓰기보다 빠름
- **필터 활용**: 필요한 엔티티만 조회하여 불필요한 반복 방지
- **Command Buffer 사용**: 시스템 실행 중 구조적 변경 시 반드시 사용

### 4. 메모리 관리

```csharp
// 커널과 월드는 사용 후 반드시 Dispose
using (var kernel = new Kernel())
{
    var world = kernel.CreateWorld(null, "Test");
    // ... 작업 ...
} // 자동으로 정리됨
```

---

## 문제 해결

### Q: 컴포넌트를 수정하려고 하면 컴파일 오류가 발생합니다.

**A:** 컴포넌트는 불변(`readonly struct`)으로 설계되었습니다. 수정하려면 `Ref<T>()`를 사용하여 참조로 접근하거나, 새로운 값을 설정해야 합니다.

```csharp
// ❌ 잘못된 방법
var pos = world.Get<Position>(entity);
pos.X += 1; // 컴파일 오류!

// ✅ 올바른 방법
ref var pos = ref world.Ref<Position>(entity);
pos = new Position(pos.X + 1, pos.Y);

// 또는
using (var cmd = world.BeginWrite())
{
    cmd.ReplaceComponent(entity, new Position(pos.X + 1, pos.Y));
}
```

### Q: 시스템이 실행되지 않습니다.

**A:** 다음을 확인하세요:

1. 시스템이 등록되었는지: `world.AddSystems([...])`
2. 시스템 그룹 속성이 올바른지: `[SimulationGroup]` 또는 `[PresentationGroup]`
3. 시스템이 올바른 인터페이스를 구현했는지
4. 월드가 실행 중인지: `kernel.IsRunning`

### Q: 쿼리가 엔티티를 찾지 못합니다.

**A:** 다음을 확인하세요:

1. 엔티티에 필요한 컴포넌트가 모두 추가되었는지
2. 컴포넌트가 제거되지 않았는지
3. 필터 조건이 올바른지

### Q: 성능이 느립니다.

**A:** 다음을 시도해보세요:

1. 불필요한 쿼리 제거
2. 컴포넌트 크기 최소화
3. 필터를 사용하여 필요한 엔티티만 조회
4. 시스템 실행 순서 최적화

---

## 다음 단계

이제 ZenECS Core의 기본을 이해했습니다! 다음 단계로:

1. 📚 [API 레퍼런스](../core/api-reference.md) - 전체 API 문서
2. 🎮 [샘플 코드](../../Packages/com.zenecs.core/Samples~/) - 더 많은 예제
3. 🏗️ [아키텍처 개요](../overview/architecture.md) - 내부 구조 이해
4. 💬 [FAQ](../overview/faq.md) - 자주 묻는 질문

---

**Made with ❤️ by Pippapips Limited**

