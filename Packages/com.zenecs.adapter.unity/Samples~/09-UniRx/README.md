# ZenECS Adapter Unity — Sample 09: UniRx 통합

**UniRx**를 사용하여 ZenECS World 메시지 버스를 `IObservable` 스트림으로 변환하고, 반대로 UniRx 스트림을 ECS 메시지로 발행하는 방법을 보여주는 샘플입니다.

* **WorldRx.Messages<T>()** — ECS 메시지를 IObservable로 변환
* **WorldRx.PublishFrom()** — UniRx 스트림을 ECS 메시지로 발행
* **조건부 컴파일** — `ZENECS_UNIRX` define 필요
* **반응형 프로그래밍** — UniRx 연산자와 ECS 메시지 결합

---

## 이 샘플이 보여주는 것

1. **ECS → UniRx**
   `WorldRx.Messages<T>()`를 사용하여 ECS 메시지를 UniRx Observable로 변환합니다.

2. **UniRx → ECS**
   `WorldRx.PublishFrom()`을 사용하여 UniRx 스트림을 ECS 메시지로 발행합니다.

3. **반응형 파이프라인**
   UniRx 연산자(Throttle, Filter, Select 등)를 사용하여 메시지를 처리합니다.

---

## TL;DR 흐름

```
[ECS Message]
  └─ world.Messages<T>()
      └─ IObservable<T>
          └─ UniRx 연산자 (Throttle, Filter, Select 등)
              └─ Subscribe() → View 업데이트

[UniRx Stream]
  └─ world.PublishFrom(stream)
      └─ world.Publish(message)
          └─ ECS 시스템에서 처리
```

---

## 파일 구조

```
09-UniRx/
├── README.md
├── UniRxSample.cs              # 샘플 스크립트
└── MessageHandlers.cs          # 메시지 핸들러 예제
```

---

## 사용 방법

### 1. ECS 메시지를 UniRx Observable로 변환

```csharp
#if ZENECS_UNIRX
using ZenECS.Adapter.Unity.UniRx;
using ZenECS.Core;

var world = KernelLocator.CurrentWorld;

// ECS 메시지를 Observable로 변환
world.Messages<DamageMessage>()
    .ThrottleFirst(TimeSpan.FromMilliseconds(100))
    .ObserveOnMainThread()
    .Subscribe(msg =>
    {
        Debug.Log($"Damage received: {msg.Amount}");
        // UI 업데이트 등
    })
    .AddTo(this);
#endif
```

### 2. UniRx 스트림을 ECS 메시지로 발행

```csharp
#if ZENECS_UNIRX
using UniRx;
using ZenECS.Adapter.Unity.UniRx;

// Unity Input을 Observable로 변환
this.UpdateAsObservable()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Select(_ => new JumpIntent())
    .PublishFrom(world)  // ECS 메시지로 발행
    .AddTo(this);
#endif
```

### 3. 복합 파이프라인

```csharp
#if ZENECS_UNIRX
// ECS 메시지 → UniRx → 필터링 → ECS 메시지
world.Messages<HealthChanged>()
    .Where(msg => msg.NewHealth <= 0)
    .Select(msg => new DeathEvent(msg.Entity))
    .PublishFrom(world)
    .AddTo(this);
#endif
```

---

## 주요 API

* **WorldRx.Messages<T>()**: ECS 메시지를 IObservable로 변환
* **WorldRx.PublishFrom()**: UniRx 스트림을 ECS 메시지로 발행
* **조건부 컴파일**: `#if ZENECS_UNIRX` 필요
* **UniRx 연산자**: Throttle, Filter, Select, Merge 등 사용 가능

---

## 주의사항 및 모범 사례

* UniRx 통합은 **조건부 컴파일**로 제공되며, `ZENECS_UNIRX` define이 필요합니다.
* `WorldRx.Messages<T>()`는 메시지 타입 `T`가 `IMessage`를 구현해야 합니다.
* UniRx 구독은 적절히 Dispose해야 합니다 (`AddTo()`, `CompositeDisposable` 사용).
* `ObserveOnMainThread()`를 사용하여 UI 업데이트를 메인 스레드에서 수행합니다.
* ECS 메시지 처리는 여전히 FixedGroup에서 수행하고, UniRx는 주로 View 레이어에서 사용합니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
