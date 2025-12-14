# ZenECS Adapter Unity — Sample 05: System Presets

**System Presets**를 사용하여 ScriptableObject 기반으로 시스템을 설정하고 관리하는 방법을 보여주는 샘플입니다.

* **SystemPreset** — ScriptableObject 기반 시스템 설정
* **SystemPresetResolver** — 시스템 프리셋 해석 및 등록
* **SystemTypeRef** — 타입 안전한 시스템 참조
* **SystemTypeFilterAttribute** — 시스템 타입 필터링

---

## 이 샘플이 보여주는 것

1. **SystemPreset 생성**
   Unity 에디터에서 SystemPreset 에셋을 생성하고 시스템 타입을 설정합니다.

2. **시스템 자동 등록**
   `SystemPresetResolver`를 사용하여 프리셋에 정의된 시스템을 자동으로 등록합니다.

3. **타입 필터링**
   `SystemTypeFilterAttribute`를 사용하여 특정 타입의 시스템만 선택할 수 있습니다.

---

## TL;DR 흐름

```
[SystemPreset ScriptableObject]
  └─ SystemTypeRef[] (시스템 타입 참조)

[SystemPresetResolver]
  └─ Resolve(preset)
      └─ 시스템 인스턴스 생성
          └─ world.AddSystems(systems)
```

---

## 파일 구조

```
05-SystemPresets/
├── README.md
├── SystemPresetSample.cs       # 샘플 스크립트
├── MovementSystem.cs            # 예제 시스템
└── RenderSystem.cs              # 예제 시스템
```

---

## 사용 방법

### 1. SystemPreset 에셋 생성

1. Unity 에디터에서 **Project 창** → 우클릭 → **Create** → **ZenECS** → **System Preset**
2. 생성된 Preset 에셋을 선택
3. Inspector에서 시스템 타입 추가:
   - **Systems** 배열에 `SystemTypeRef` 추가
   - 각 `SystemTypeRef`에서 시스템 타입 선택

### 2. 런타임에서 사용

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;

public class SystemPresetSample : MonoBehaviour
{
    [SerializeField] private SystemPreset _preset;

    private void Start()
    {
        var world = KernelLocator.CurrentWorld;
        if (world == null || _preset == null) return;

        // SystemPresetResolver를 사용하여 시스템 등록
        var resolver = new SystemPresetResolver();
        var systems = resolver.Resolve(_preset);
        
        world.AddSystems(systems);
        Debug.Log($"{systems.Count}개의 시스템이 등록되었습니다.");
    }
}
```

### 3. 커스텀 SystemPresetResolver

```csharp
public class CustomSystemPresetResolver : ISystemPresetResolver
{
    public IReadOnlyList<ISystem> Resolve(SystemPreset preset)
    {
        var systems = new List<ISystem>();
        
        foreach (var systemRef in preset.Systems)
        {
            if (systemRef.Type != null)
            {
                var system = Activator.CreateInstance(systemRef.Type) as ISystem;
                if (system != null)
                    systems.Add(system);
            }
        }
        
        return systems;
    }
}
```

---

## 주요 API

* **SystemPreset**: ScriptableObject 기반 시스템 프리셋
* **SystemPresetResolver**: 시스템 프리셋 해석 및 인스턴스 생성
* **ISystemPresetResolver**: 커스텀 Resolver 인터페이스
* **SystemTypeRef**: 타입 안전한 시스템 타입 참조
* **SystemTypeFilterAttribute**: 시스템 타입 필터링 속성

---

## 주의사항 및 모범 사례

* SystemPreset은 **타입 참조만** 저장하며, 시스템 인스턴스는 런타임에 생성됩니다.
* 시스템 타입은 `ISystem`을 구현해야 합니다.
* `SystemTypeFilterAttribute`를 사용하여 특정 조건의 시스템만 선택할 수 있습니다.
* SystemPreset을 사용하면 시스템 구성을 에셋으로 관리할 수 있어 유연성이 높아집니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
