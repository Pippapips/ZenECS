# ZenECS Adapter Unity — Sample 01: EcsDriver 기본 설정

Unity에서 **EcsDriver**를 사용하여 ZenECS Kernel을 초기화하고 Unity의 생명주기와 연동하는 방법을 보여주는 샘플입니다.

* **EcsDriver** — MonoBehaviour 기반 Kernel 생명주기 관리
* **KernelLocator** — 전역 Kernel 접근
* Unity 프레임 콜백과 ECS 프레임 구조 연동

---

## 이 샘플이 보여주는 것

1. **EcsDriver 설정**
   씬에 `EcsDriver` 컴포넌트를 추가하여 Kernel을 자동으로 생성하고 관리합니다.

2. **World 생성 및 시스템 등록**
   `KernelLocator.Current`를 통해 Kernel에 접근하고, World를 생성하여 시스템을 등록합니다.

3. **Unity 생명주기 연동**
   `EcsDriver`가 Unity의 `Update`, `FixedUpdate`, `LateUpdate`를 ECS의 `BeginFrame`, `FixedStep`, `LateFrame`으로 자동 변환합니다.

---

## TL;DR 흐름

```
[Unity Scene]
  └─ EcsDriver (MonoBehaviour)
      └─ Kernel 생성 및 KernelLocator에 등록
          └─ Update() → BeginFrame()
          └─ FixedUpdate() → FixedStep()
          └─ LateUpdate() → LateFrame()

[Bootstrap Script]
  └─ KernelLocator.Current로 Kernel 접근
      └─ CreateWorld()로 World 생성
      └─ AddSystems()로 시스템 등록
```

---

## 파일 구조

```
01-EcsDriver/
├── README.md
├── EcsDriverSample.cs          # Bootstrap 스크립트
└── MovementSystem.cs            # 예제 시스템
```

---

## 사용 방법

### 1. 씬 설정

1. Unity 씬을 엽니다.
2. 빈 GameObject를 생성하고 이름을 "EcsDriver"로 설정합니다.
3. `EcsDriver` 컴포넌트를 추가합니다 (자동으로 추가되거나 수동으로 추가).

### 2. Bootstrap 스크립트 추가

새 GameObject를 생성하고 `EcsDriverSample` 스크립트를 추가합니다:

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class EcsDriverSample : MonoBehaviour
{
    private void Awake()
    {
        // Kernel은 EcsDriver가 자동으로 생성합니다
        var kernel = KernelLocator.Current;
        if (kernel == null)
        {
            Debug.LogError("EcsDriver가 씬에 없습니다!");
            return;
        }

        // World 생성
        var world = kernel.CreateWorld("GameWorld", setAsCurrent: true);
        
        // 시스템 등록
        world.AddSystems([new MovementSystem()]);
        
        // 테스트 엔티티 생성
        using (var cmd = world.BeginWrite())
        {
            var entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(0, 0));
            cmd.AddComponent(entity, new Velocity(1, 0));
        }
        
        Debug.Log("EcsDriver 샘플이 초기화되었습니다!");
    }
}
```

### 3. 실행

씬을 실행하면 `EcsDriver`가 자동으로 Kernel을 생성하고, Unity의 프레임 콜백이 ECS 프레임 구조로 변환됩니다.

---

## 주요 API

* **EcsDriver**: Unity MonoBehaviour 기반 Kernel 드라이버
* **KernelLocator.Current**: 전역 Kernel 인스턴스 접근
* **KernelLocator.CurrentWorld**: 현재 활성화된 World 접근
* **IKernel.CreateWorld()**: 새 World 생성
* **IWorld.AddSystems()**: 시스템 등록

---

## 주의사항 및 모범 사례

* 씬에는 **하나의 EcsDriver만** 존재해야 합니다 (중복 시 자동으로 제거됨).
* `EcsDriver`는 `DefaultExecutionOrder(-32000)`으로 설정되어 다른 스크립트보다 먼저 실행됩니다.
* Kernel은 `EcsDriver`의 생명주기에 따라 자동으로 생성/소멸됩니다.
* World 생성은 일반적으로 `Awake()` 또는 `Start()`에서 수행합니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
