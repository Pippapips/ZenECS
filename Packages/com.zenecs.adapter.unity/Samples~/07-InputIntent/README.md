# ZenECS Adapter Unity — Sample 07: Input → Intent 패턴

Unity **Input**을 ECS **Intent** 컴포넌트로 변환하는 패턴을 보여주는 샘플입니다.

* **Intent 컴포넌트** — 입력 의도를 나타내는 컴포넌트
* **Input System** — Unity Input을 Intent로 변환
* **Intent 처리 시스템** — Intent를 읽어 게임 로직 실행
* **메시지 기반 입력** — View → Message → System 흐름

---

## 이 샘플이 보여주는 것

1. **Input 수집**
   Unity의 Input System (또는 Legacy Input)을 사용하여 입력을 수집합니다.

2. **Intent 생성**
   수집된 입력을 Intent 컴포넌트로 변환하여 엔티티에 추가합니다.

3. **Intent 처리**
   FixedGroup 시스템에서 Intent를 읽어 게임 로직을 실행합니다.

4. **Intent 소비**
   처리된 Intent는 제거되어 다음 프레임에 다시 생성됩니다.

---

## TL;DR 흐름

```
[Unity Input]
  └─ Input 수집 (Update)

[Input System (FrameViewGroup)]
  └─ Intent 컴포넌트 추가/업데이트

[Intent Processing System (FixedGroup)]
  └─ Intent 읽기 → 게임 로직 실행
  └─ Intent 제거 (소비)
```

---

## 파일 구조

```
07-InputIntent/
├── README.md
├── InputIntentSample.cs         # 샘플 스크립트
├── MoveIntent.cs                # 이동 Intent 컴포넌트
└── MovementIntentSystem.cs      # Intent 처리 시스템
```

---

## 사용 방법

### 1. Intent 컴포넌트 정의

```csharp
using ZenECS.Core;

/// <summary>
/// 이동 Intent - 플레이어의 이동 의도를 나타냅니다.
/// </summary>
public readonly struct MoveIntent
{
    public readonly float X, Y, Z;
    public MoveIntent(float x, float y, float z) { X = x; Y = y; Z = z; }
}
```

### 2. Input 수집 시스템 (FrameViewGroup)

```csharp
[FrameViewGroup]
public sealed class InputCollectionSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // 플레이어 엔티티 찾기 (예: Player 태그가 있는 엔티티)
        foreach (var entity in w.Query<Entity>())
        {
            if (!w.HasTag(entity, "Player")) continue;

            // Unity Input 수집
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // Intent 컴포넌트 추가/업데이트
            using var cmd = w.BeginWrite();
            if (w.HasComponent<MoveIntent>(entity))
            {
                cmd.ReplaceComponent(entity, new MoveIntent(x, 0, z));
            }
            else
            {
                cmd.AddComponent(entity, new MoveIntent(x, 0, z));
            }
        }
    }
}
```

### 3. Intent 처리 시스템 (FixedGroup)

```csharp
[FixedGroup]
public sealed class MovementIntentSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, intent, pos) in w.Query<MoveIntent, Position>())
        {
            // Intent를 기반으로 Position 업데이트
            var newPos = new Position(
                pos.X + intent.X * dt * 5f, // 속도 5
                pos.Y,
                pos.Z + intent.Z * dt * 5f
            );
            cmd.ReplaceComponent(e, newPos);

            // Intent 소비 (제거)
            cmd.RemoveComponent<MoveIntent>(e);
        }
    }
}
```

---

## 주요 API

* **Intent 컴포넌트**: 입력 의도를 나타내는 컴포넌트
* **Input 수집**: Unity Input System 또는 Legacy Input 사용
* **Intent 처리**: FixedGroup에서 Intent를 읽어 로직 실행
* **Intent 소비**: 처리 후 Intent 제거

---

## 주의사항 및 모범 사례

* Input 수집은 **FrameViewGroup**에서 수행합니다 (읽기 전용, Intent만 추가).
* Intent 처리는 **FixedGroup**에서 수행합니다 (게임 로직 실행).
* Intent는 **한 프레임에 한 번만** 처리되도록 처리 후 제거합니다.
* 여러 Intent 타입을 사용하여 입력을 분리할 수 있습니다 (MoveIntent, JumpIntent 등).
* Input을 직접 게임 로직에 연결하지 말고 Intent를 통해 간접적으로 연결합니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
