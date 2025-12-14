# ZenECS Adapter Unity — Sample 10: Zenject 통합

**Zenject**를 사용하여 ZenECS Kernel과 시스템을 의존성 주입으로 관리하는 방법을 보여주는 샘플입니다.

* **ProjectInstaller** — Zenject MonoInstaller 기반 프로젝트 설정
* **KernelLocator** — 전역 Kernel 접근
* **ISharedContextResolver** — Zenject를 통한 Context 해석
* **ISystemPresetResolver** — Zenject를 통한 시스템 인스턴스 생성
* **조건부 컴파일** — `ZENECS_ZENJECT` define 필요

---

## 이 샘플이 보여주는 것

1. **ProjectInstaller 설정**
   Zenject MonoInstaller를 사용하여 Kernel과 Resolver를 DI 컨테이너에 바인딩합니다.

2. **의존성 주입**
   Zenject를 통해 시스템과 Context를 자동으로 주입받습니다.

3. **이중 모드**
   Zenject가 있을 때와 없을 때의 동작 차이를 보여줍니다.

---

## TL;DR 흐름

```
[Zenject SceneContext]
  └─ ProjectInstaller (MonoInstaller)
      └─ InstallBindings()
          ├─ Kernel 생성 및 바인딩
          ├─ ISharedContextResolver 바인딩
          └─ ISystemPresetResolver 바인딩

[시스템 생성]
  └─ SystemPresetResolver.Resolve()
      └─ DiContainer.Instantiate<T>() (Zenject 모드)
          └─ 의존성 자동 주입
```

---

## 파일 구조

```
10-Zenject/
├── README.md
├── ZenjectSample.cs             # 샘플 스크립트
├── GameConfig.cs                # DI로 주입되는 Context 예제
└── MovementSystem.cs            # DI를 사용하는 시스템 예제
```

---

## 사용 방법

### 1. ProjectInstaller 설정

1. Unity 씬에 **SceneContext** 추가 (Zenject)
2. **ProjectInstaller** 컴포넌트를 SceneContext에 추가
3. 또는 별도 GameObject에 `ProjectInstaller` 추가

### 2. Zenject를 통한 시스템 생성

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity.SystemPresets;

public class MovementSystem : ISystem
{
    private readonly IGameConfig _config;

    // Zenject가 자동으로 IGameConfig를 주입
    public MovementSystem(IGameConfig config)
    {
        _config = config;
    }

    public void Run(IWorld w, float dt)
    {
        // _config.GameSpeed 사용
    }
}
#endif
```

### 3. Context Resolver 설정

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity.Binding.Contexts;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Context를 Zenject에 바인딩
        Container.Bind<IGameConfig>().To<GameConfig>().AsSingle();
    }
}
#endif
```

### 4. Kernel 접근

```csharp
var kernel = KernelLocator.Current;
var world = kernel.CreateWorld("GameWorld", setAsCurrent: true);

// SystemPresetResolver는 Zenject를 통해 자동으로 주입됨
var presetResolver = ZenEcsUnityBridge.SystemPresetResolver;
var systems = presetResolver.Resolve(preset);
world.AddSystems(systems);
```

---

## 주요 API

* **ProjectInstaller**: Zenject MonoInstaller 기반 프로젝트 설정
* **KernelLocator.Current**: 전역 Kernel 접근
* **ZenEcsUnityBridge.SharedContextResolver**: Zenject를 통한 Context Resolver
* **ZenEcsUnityBridge.SystemPresetResolver**: Zenject를 통한 System Preset Resolver
* **조건부 컴파일**: `#if ZENECS_ZENJECT` 필요

---

## 주의사항 및 모범 사례

* Zenject 통합은 **조건부 컴파일**로 제공되며, `ZENECS_ZENJECT` define이 필요합니다.
* `ProjectInstaller`는 씬당 하나만 존재해야 합니다.
* Zenject가 없으면 `ProjectInstaller`는 MonoBehaviour로 동작하며 수동으로 Kernel을 생성합니다.
* 시스템 생성 시 Zenject가 있으면 `DiContainer.Instantiate<T>()`를 사용하고, 없으면 `Activator.CreateInstance()`를 사용합니다.
* Context Resolver도 Zenject 모드에서는 DI 컨테이너를 사용하고, 비Zenject 모드에서는 수동 레지스트리를 사용합니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
