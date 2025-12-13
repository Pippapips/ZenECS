# 코드 스타일 가이드 (Style Guide)

이 문서는 ZenECS 프로젝트의 C# 코드 스타일 가이드입니다. 모든 코드는 이 가이드를 따라야 합니다.

## 목차

- [일반 원칙](#일반-원칙)
- [파일 구조](#파일-구조)
- [네이밍 규칙](#네이밍-규칙)
- [코드 포맷팅](#코드-포맷팅)
- [타입 정의](#타입-정의)
- [문서화](#문서화)
- [Null 안전성](#null-안전성)
- [성능 고려사항](#성능-고려사항)
- [예외 처리](#예외-처리)
- [예제](#예제)

---

## 일반 원칙

### 핵심 원칙

1. **명확성 우선**: 코드는 읽기 쉬워야 합니다
2. **일관성**: 기존 코드 스타일을 따릅니다
3. **간결성**: 불필요한 복잡성을 피합니다
4. **안전성**: Null 안전성과 타입 안전성을 보장합니다

### C# 버전

- **.NET Standard 2.1** 이상
- C# 8.0 이상 기능 사용 가능
- Nullable reference types 활성화

---

## 파일 구조

### 파일 헤더

모든 `.cs` 파일은 다음 형식의 헤더를 포함해야 합니다:

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — [모듈명]
// File: [파일명].cs
// Purpose: [파일의 목적과 역할을 한 줄로 설명]
// Key concepts:
//   • [주요 개념 1]
//   • [주요 개념 2]
// Copyright (c) [연도] Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
```

### 예시

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World API
// File: Entity.cs
// Purpose: Lightweight 64-bit entity handle (generation|id) and helpers.
// Key concepts:
//   • Upper 32 bits: generation; lower 32 bits: id.
//   • Value semantics: equality/hash by packed handle; explicit casts.
//   • Safety: use world APIs to validate liveness/generation before access.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
```

### 파일 내용 순서

1. 파일 헤더 주석
2. `#nullable enable` 지시문
3. `using` 문 (알파벳 순서)
4. 네임스페이스 선언
5. 타입 정의

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// [헤더]
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    // 타입 정의
}
```

---

## 네이밍 규칙

### 일반 규칙

- **PascalCase**: 타입, 메서드, 속성, 이벤트, 상수
- **camelCase**: 지역 변수, 매개 변수, private 필드
- **UPPER_CASE**: 상수 (필드가 아닌 경우)
- **_camelCase**: private 인스턴스 필드 (선택적, 일관성 유지)

### 타입

```csharp
// ✅ 좋은 예
public class Kernel { }
public interface IWorld { }
public readonly struct Entity { }
public enum WorldState { }

// ❌ 나쁜 예
public class kernel { }
public interface world { }
```

### 메서드 및 속성

```csharp
// ✅ 좋은 예
public Entity CreateEntity() { }
public bool IsAlive(Entity entity) { }
public int FrameCount { get; }

// ❌ 나쁜 예
public Entity create_entity() { }
public bool is_alive(Entity entity) { }
```

### 필드 및 변수

```csharp
// ✅ 좋은 예
private readonly ConcurrentDictionary<WorldId, IWorld> _byId = new();
private int _frameCount;
var entity = world.CreateEntity();

// ❌ 나쁜 예
private readonly ConcurrentDictionary<WorldId, IWorld> ById = new();
private int FrameCount;
var Entity = world.CreateEntity();
```

### 상수

```csharp
// ✅ 좋은 예
public const int GenShift = 32;
public const ulong IdMask = 0x00000000_FFFFFFFFUL;
public static readonly Entity None = default;

// ❌ 나쁜 예
public const int genShift = 32;
public const ulong idMask = 0x00000000_FFFFFFFFUL;
```

### 약어

- 약어는 일반적으로 대문자로 표기: `ID`, `API`, `ECS`
- 예외: 일반적으로 소문자로 사용되는 약어는 그대로 유지: `id`, `api`

---

## 코드 포맷팅

### 들여쓰기

- **4개의 공백** 사용 (탭 사용 금지)
- Visual Studio 기본 설정 사용

### 중괄호

**K&R 스타일** (중괄호를 같은 줄에):

```csharp
// ✅ 좋은 예
public void Method()
{
    if (condition)
    {
        // ...
    }
}

// ❌ 나쁜 예
public void Method() 
{
    if (condition) {
        // ...
    }
}
```

### 줄 길이

- 최대 **120자** (가독성을 위해)
- 긴 줄은 논리적 위치에서 줄바꿈

```csharp
// ✅ 좋은 예
public Entity CreateEntity(
    string? name = null,
    IReadOnlyCollection<string>? tags = null)
{
    // ...
}

// 긴 메서드 호출
var result = world.Query<Position, Velocity>()
    .Where(e => world.IsAlive(e))
    .ToList();
```

### 공백

```csharp
// ✅ 좋은 예
if (condition)
{
    DoSomething();
}

for (int i = 0; i < count; i++)
{
    Process(i);
}

// 연산자 주변 공백
var sum = a + b;
var product = x * y;

// ❌ 나쁜 예
if(condition){
    DoSomething();
}

for(int i=0;i<count;i++){
    Process(i);
}
```

### 줄바꿈

- 클래스/인터페이스 멤버 사이에 빈 줄 하나
- 논리적 그룹 사이에 빈 줄 사용

```csharp
public class Example
{
    // 필드 그룹
    private int _field1;
    private int _field2;

    // 속성 그룹
    public int Property1 { get; }
    public int Property2 { get; }

    // 메서드 그룹
    public void Method1() { }

    public void Method2() { }
}
```

---

## 타입 정의

### 클래스

- 가능한 경우 `sealed` 사용
- 상속이 필요한 경우에만 `sealed` 생략

```csharp
// ✅ 좋은 예
public sealed class Kernel : IKernel
{
    // ...
}

// ❌ 나쁜 예 (상속이 필요하지 않은 경우)
public class Kernel : IKernel
{
    // ...
}
```

### 구조체

- 컴포넌트는 `readonly struct` 사용
- 값 타입 의미론이 적절한 경우에만 구조체 사용

```csharp
// ✅ 좋은 예
public readonly struct Position
{
    public readonly float X;
    public readonly float Y;

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// ❌ 나쁜 예 (가변 구조체)
public struct Position
{
    public float X;
    public float Y;
}
```

### 인터페이스

- 인터페이스 이름은 `I`로 시작
- 명확하고 간결한 이름 사용

```csharp
// ✅ 좋은 예
public interface IWorld { }
public interface IComponent { }

// ❌ 나쁜 예
public interface World { }
public interface ComponentInterface { }
```

### 열거형

```csharp
// ✅ 좋은 예
public enum WorldState
{
    Initializing,
    Running,
    Paused,
    Disposing
}

// ❌ 나쁜 예
public enum WorldState
{
    INITIALIZING,
    RUNNING,
    PAUSED
}
```

---

## 문서화

### XML 문서 주석

모든 public API는 XML 문서 주석을 포함해야 합니다:

```csharp
/// <summary>
/// Creates a new entity in this world.
/// </summary>
/// <returns>A new entity handle.</returns>
/// <remarks>
/// The entity is immediately available for component operations.
/// Use <see cref="IsAlive(Entity)"/> to verify entity validity.
/// </remarks>
public Entity CreateEntity()
{
    // ...
}
```

### 매개 변수 및 반환 값

```csharp
/// <summary>
/// Gets the component of type <typeparamref name="T"/> for the specified entity.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="entity">The entity to query.</param>
/// <returns>
/// The component value, or <see langword="default"/> if not found.
/// </returns>
/// <exception cref="ArgumentException">
/// Thrown when the entity is not alive.
/// </exception>
public T Get<T>(Entity entity) where T : struct
{
    // ...
}
```

### 주석 규칙

- **`<summary>`**: 필수, 한 줄 또는 여러 줄
- **`<remarks>`**: 추가 설명이 필요한 경우
- **`<param>`**: 모든 매개 변수
- **`<returns>`**: 반환 값이 있는 경우
- **`<exception>`**: 예외를 던지는 경우
- **`<see cref="..."/>`**: 다른 타입/멤버 참조
- **`<see langword="..."/>`**: C# 키워드 참조 (`null`, `true`, `default` 등)

### 인라인 주석

- 복잡한 로직에만 주석 사용
- "무엇을" 하는지 설명 (코드 자체가 "어떻게"를 보여줌)
- TODO 주석은 이슈 번호 포함

```csharp
// ✅ 좋은 예
// Generation-based validation prevents stale entity references
if (entity.Gen != world.GenerationOf(entity.Id))
{
    return false;
}

// ❌ 나쁜 예
// i를 증가시킴
i++;

// TODO: 이 부분 수정 필요
// TODO(#123): 이 부분 수정 필요
```

---

## Null 안전성

### Nullable Reference Types

모든 파일은 `#nullable enable`로 시작해야 합니다:

```csharp
#nullable enable
using System;

namespace ZenECS.Core
{
    // ...
}
```

### Null 가능성 표시

```csharp
// ✅ 좋은 예
public string? Name { get; set; }  // null 가능
public string Description { get; } // null 불가능

public Entity? FindEntity(WorldId id)  // null 반환 가능
{
    return _byId.TryGetValue(id, out var world) ? world : null;
}

// ❌ 나쁜 예 (null 가능성을 명시하지 않음)
public string Name { get; set; }  // null 가능한지 불명확
```

### Null 체크

```csharp
// ✅ 좋은 예
if (name is null)
{
    throw new ArgumentNullException(nameof(name));
}

// 또는
ArgumentNullException.ThrowIfNull(name);

// null 조건 연산자
var length = name?.Length ?? 0;
```

---

## 성능 고려사항

### 구조체 사용

- 작은 데이터 타입은 구조체로 정의
- 힙 할당 최소화

```csharp
// ✅ 좋은 예 (구조체)
public readonly struct Position
{
    public readonly float X, Y;
}

// ❌ 나쁜 예 (클래스 - 불필요한 힙 할당)
public class Position
{
    public float X, Y;
}
```

### Span 및 Memory

- 가능한 경우 `Span<T>` 또는 `Memory<T>` 사용
- 배열 복사 최소화

```csharp
// ✅ 좋은 예
public void ProcessEntities(Span<Entity> entities)
{
    foreach (ref var entity in entities)
    {
        // ...
    }
}
```

### LINQ 주의

- 성능이 중요한 경로에서는 LINQ 사용 최소화
- 직접 반복이 더 빠를 수 있음

```csharp
// ✅ 좋은 예 (성능 중요)
foreach (var entity in world.Query<Position>())
{
    // 직접 처리
}

// ⚠️ 주의 (성능 덜 중요)
var entities = world.Query<Position>().Where(e => e.Id > 100).ToList();
```

---

## 예외 처리

### 예외 타입

- 적절한 예외 타입 사용
- `ArgumentException`, `ArgumentNullException`, `InvalidOperationException` 등

```csharp
// ✅ 좋은 예
if (entity.IsNone)
{
    throw new ArgumentException("Entity cannot be None.", nameof(entity));
}

if (name is null)
{
    throw new ArgumentNullException(nameof(name));
}

if (_isDisposed)
{
    throw new ObjectDisposedException(nameof(Kernel));
}

// ❌ 나쁜 예
throw new Exception("Error occurred");
```

### 예외 메시지

- 명확하고 구체적인 메시지
- 매개 변수 이름 포함

```csharp
// ✅ 좋은 예
throw new ArgumentException(
    $"Entity {entity.Id} is not alive.",
    nameof(entity));

// ❌ 나쁜 예
throw new ArgumentException("Invalid entity");
```

---

## 예제

### 완전한 예제

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World API
// File: ExampleComponent.cs
// Purpose: Example component demonstrating coding standards.
// Key concepts:
//   • Readonly struct for value semantics
//   • Immutability for safety
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Represents a position in 2D space.
    /// </summary>
    /// <remarks>
    /// This component uses value semantics and is immutable.
    /// To update a position, create a new instance.
    /// </remarks>
    public readonly struct Position
    {
        /// <summary>
        /// Gets the X coordinate.
        /// </summary>
        public readonly float X;

        /// <summary>
        /// Gets the Y coordinate.
        /// </summary>
        public readonly float Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> struct.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns a string representation of this position.
        /// </summary>
        /// <returns>A string in the format "(X, Y)".</returns>
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }
}
```

### 클래스 예제

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Example
// File: ExampleSystem.cs
// Purpose: Example system demonstrating coding standards.
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Example system that processes entities with position and velocity.
    /// </summary>
    [SimulationGroup]
    public sealed class MoveSystem : IFixedRunSystem
    {
        /// <summary>
        /// Runs the system logic for a fixed timestep.
        /// </summary>
        /// <param name="world">The world to operate on.</param>
        /// <param name="fixedDelta">The fixed timestep in seconds.</param>
        public void Run(IWorld world, float fixedDelta)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

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
}
```

---

## 도구 및 설정

### EditorConfig

프로젝트 루트에 `.editorconfig` 파일을 추가하여 일관된 포맷팅을 유지할 수 있습니다.

### Visual Studio 설정

- **코드 스타일**: C# 코딩 컨벤션 사용
- **포맷팅**: 저장 시 자동 포맷팅 활성화
- **XML 문서**: 경고 수준 설정

---

## 요약 체크리스트

코드를 제출하기 전에 다음을 확인하세요:

- [ ] 파일 헤더 주석 포함
- [ ] `#nullable enable` 사용
- [ ] 모든 public API에 XML 문서 주석
- [ ] 네이밍 규칙 준수
- [ ] 들여쓰기 4칸 (탭 사용 안 함)
- [ ] K&R 스타일 중괄호
- [ ] 적절한 예외 타입 사용
- [ ] Null 안전성 고려
- [ ] 성능 고려사항 적용

---

**이 가이드를 따르면 코드베이스의 일관성과 가독성을 유지할 수 있습니다!**

