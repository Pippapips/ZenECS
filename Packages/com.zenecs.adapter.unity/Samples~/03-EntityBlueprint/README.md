# ZenECS Adapter Unity — Sample 03: EntityBlueprint (엔티티 스폰)

**EntityBlueprint**를 사용하여 ScriptableObject 기반으로 엔티티를 스폰하는 방법을 보여주는 샘플입니다.

* **EntityBlueprint** — ScriptableObject 기반 엔티티 블루프린트
* **EntityBlueprintData** — 컴포넌트 스냅샷 저장
* **Context Assets** — Shared/Per-Entity Context 설정
* **Binders** — 엔티티별 Binder 설정

---

## 이 샘플이 보여주는 것

1. **Blueprint 생성**
   Unity 에디터에서 EntityBlueprint 에셋을 생성하고 컴포넌트 데이터를 설정합니다.

2. **런타임 스폰**
   `EntityBlueprint.Spawn()`을 호출하여 블루프린트에 정의된 엔티티를 생성합니다.

3. **Context 및 Binder 적용**
   블루프린트에 설정된 Context Assets와 Binders가 자동으로 엔티티에 적용됩니다.

---

## TL;DR 흐름

```
[EntityBlueprint ScriptableObject]
  ├─ EntityBlueprintData (컴포넌트 스냅샷)
  ├─ ContextAssets (Shared/Per-Entity)
  └─ Binders (관리 참조)

[Runtime]
  └─ blueprint.Spawn(world, contextResolver)
      └─ ExternalCommand.CreateEntity
          ├─ ApplyComponents (스냅샷 적용)
          ├─ ApplyContexts (Context 등록)
          └─ ApplyBinders (Binder 연결)
```

---

## 파일 구조

```
03-EntityBlueprint/
├── README.md
├── EntityBlueprintSample.cs    # 샘플 스크립트
├── HealthComponent.cs          # 예제 컴포넌트
└── HealthBinder.cs             # 예제 Binder
```

---

## 사용 방법

### 1. EntityBlueprint 에셋 생성

1. Unity 에디터에서 **Project 창** → 우클릭 → **Create** → **ZenECS** → **Entity Blueprint**
2. 생성된 Blueprint 에셋을 선택
3. Inspector에서 컴포넌트 데이터 추가:
   - **Components (snapshot)** 섹션에서 컴포넌트 추가
   - **Contexts** 섹션에서 Context Assets 추가 (선택)
   - **Binders** 섹션에서 Binder 추가 (선택)

### 2. 런타임에서 스폰

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;

public class EntityBlueprintSample : MonoBehaviour
{
    [SerializeField] private EntityBlueprint _blueprint;

    private void Start()
    {
        var world = KernelLocator.CurrentWorld;
        if (world == null || _blueprint == null) return;

        // Blueprint로 엔티티 스폰
        _blueprint.Spawn(
            world,
            ZenEcsUnityBridge.SharedContextResolver,
            onCreated: entity => Debug.Log($"Entity {entity.Id} 스폰 완료!")
        );
    }
}
```

### 3. 컴포넌트 스냅샷 설정

Blueprint Inspector에서 컴포넌트를 추가하려면:
1. **Components (snapshot)** 섹션 확장
2. **Add Component** 버튼 클릭
3. 컴포넌트 타입 선택 및 값 입력

---

## 주요 API

* **EntityBlueprint**: ScriptableObject 기반 엔티티 블루프린트
* **EntityBlueprint.Spawn()**: 블루프린트로 엔티티 스폰
* **EntityBlueprintData**: 컴포넌트 스냅샷 데이터
* **SharedContextAsset**: 공유 Context 마커
* **PerEntityContextAsset**: 엔티티별 Context 팩토리
* **IBinder**: 뷰 바인딩 인터페이스

---

## 주의사항 및 모범 사례

* Blueprint는 **ExternalCommand**를 사용하여 안전하게 엔티티를 생성합니다.
* Binder는 **shallow-clone**되어 각 엔티티마다 독립적인 인스턴스가 생성됩니다.
* Shared Context는 `ISharedContextResolver`를 통해 해석되어야 합니다.
* Blueprint의 컴포넌트 데이터는 JSON으로 직렬화되므로 직렬화 가능한 타입만 사용할 수 있습니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
