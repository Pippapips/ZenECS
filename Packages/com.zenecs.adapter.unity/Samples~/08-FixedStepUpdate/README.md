# ZenECS Adapter Unity — Sample 08: FixedStep vs Update

Unity의 **Update/FixedUpdate**와 ZenECS의 **BeginFrame/FixedStep/LateFrame** 구조를 비교하고 사용하는 방법을 보여주는 샘플입니다.

* **BeginFrame** — 가변 타임스텝 (Update)
* **FixedStep** — 고정 타임스텝 (FixedUpdate)
* **LateFrame** — 보간 및 프레젠테이션 (LateUpdate)
* **EcsDriver** — Unity 생명주기와 ECS 프레임 구조 자동 연동

---

## 이 샘플이 보여주는 것

1. **프레임 구조 이해**
   Unity의 Update/FixedUpdate/LateUpdate가 ECS의 BeginFrame/FixedStep/LateFrame으로 어떻게 매핑되는지 보여줍니다.

2. **타임스텝 차이**
   가변 타임스텝과 고정 타임스텝의 차이를 시각적으로 확인할 수 있습니다.

3. **시스템 그룹 분리**
   FixedGroup과 FrameViewGroup이 각각 어떤 프레임 단계에서 실행되는지 보여줍니다.

---

## TL;DR 흐름

```
[Unity]
  Update() → EcsDriver.Update() → Kernel.BeginFrame(deltaTime)
  FixedUpdate() → EcsDriver.FixedUpdate() → Kernel.FixedStep(fixedDeltaTime)
  LateUpdate() → EcsDriver.LateUpdate() → Kernel.LateFrame(alpha)

[ECS]
  BeginFrame (가변 타임스텝)
    ↓
  FixedStep × N (고정 타임스텝, 시뮬레이션)
    ↓
  LateFrame (보간, 프레젠테이션)
```

---

## 파일 구조

```
08-FixedStepUpdate/
├── README.md
├── FixedStepUpdateSample.cs    # 샘플 스크립트
└── FrameTimingSystem.cs        # 프레임 타이밍 추적 시스템
```

---

## 사용 방법

### 1. EcsDriver 자동 연동

`EcsDriver`가 자동으로 Unity 생명주기를 ECS 프레임 구조로 변환합니다:

```csharp
// EcsDriver.cs 내부 (자동 실행)
private void Update() => Kernel?.BeginFrame(Time.deltaTime);
private void FixedUpdate() => Kernel?.FixedStep(Time.fixedDeltaTime);
private void LateUpdate() => Kernel?.LateFrame();
```

### 2. 시스템 그룹별 실행 시점

```csharp
[FixedGroup]
public sealed class SimulationSystem : ISystem
{
    // FixedStep에서 실행 (고정 타임스텝)
    public void Run(IWorld w, float dt)
    {
        // dt는 fixedDeltaTime (예: 0.02f for 50Hz)
        // 시뮬레이션 로직 실행
    }
}

[FrameViewGroup]
public sealed class PresentationSystem : ISystem
{
    // LateFrame에서 실행 (가변 타임스텝)
    public void Run(IWorld w, float dt)
    {
        // dt는 deltaTime (가변)
        // 프레젠테이션 로직 실행 (읽기 전용)
    }
}
```

### 3. 수동 프레임 제어 (선택적)

`EcsDriver`를 사용하지 않고 수동으로 프레임을 제어할 수도 있습니다:

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld();

// 수동 프레임 제어
kernel.BeginFrame(Time.deltaTime);
kernel.FixedStep(Time.fixedDeltaTime);
kernel.LateFrame();
```

---

## 주요 API

* **IKernel.BeginFrame()**: 가변 타임스텝 프레임 시작
* **IKernel.FixedStep()**: 고정 타임스텝 시뮬레이션 스텝
* **IKernel.LateFrame()**: 보간 및 프레젠테이션
* **IKernel.PumpAndLateFrame()**: 편의 메서드 (BeginFrame + FixedStep + LateFrame)
* **EcsDriver**: Unity 생명주기 자동 연동

---

## 주의사항 및 모범 사례

* **FixedGroup** 시스템은 **FixedStep**에서 실행되며, **고정 타임스텝**을 사용합니다.
* **FrameViewGroup** 시스템은 **LateFrame**에서 실행되며, **가변 타임스텝**을 사용합니다.
* 시뮬레이션 로직은 항상 **FixedGroup**에서 실행하여 결정론적 동작을 보장합니다.
* 프레젠테이션 로직은 **FrameViewGroup**에서 실행하며, **읽기 전용**이어야 합니다.
* `EcsDriver`를 사용하면 Unity 생명주기가 자동으로 연동되므로 수동 제어가 필요 없습니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
