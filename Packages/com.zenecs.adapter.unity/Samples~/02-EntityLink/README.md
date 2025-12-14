# ZenECS Adapter Unity — Sample 02: EntityLink (GameObject ↔ Entity 연결)

Unity **GameObject**와 ZenECS **Entity**를 연결하는 **EntityLink** 컴포넌트 사용법을 보여주는 샘플입니다.

* **EntityLink** — GameObject와 Entity를 연결하는 MonoBehaviour
* **EntityViewRegistry** — World별 뷰 레지스트리 관리
* 런타임 및 에디터에서 링크 생성/제거

---

## 이 샘플이 보여주는 것

1. **EntityLink 생성**
   `CreateEntityLink()` 확장 메서드를 사용하여 GameObject에 EntityLink를 추가하고 Entity와 연결합니다.

2. **뷰 레지스트리 자동 관리**
   `EntityLink`가 자동으로 `EntityViewRegistry`에 등록/해제되어 World별로 뷰를 관리합니다.

3. **링크 생명주기**
   GameObject가 파괴되면 자동으로 링크가 해제되고 레지스트리에서 제거됩니다.

---

## TL;DR 흐름

```
[GameObject]
  └─ EntityLink 컴포넌트
      └─ Attach(world, entity)
          └─ EntityViewRegistry.For(world).Register(entity, link)

[EntityViewRegistry]
  └─ World별 뷰 맵 관리
      └─ entity → EntityLink 매핑
```

---

## 파일 구조

```
02-EntityLink/
├── README.md
├── EntityLinkSample.cs          # 샘플 스크립트
└── PositionView.cs             # Position 컴포넌트를 Transform에 반영하는 뷰
```

---

## 사용 방법

### 1. 기본 링크 생성

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;

public class EntityLinkSample : MonoBehaviour
{
    private void Start()
    {
        var world = KernelLocator.CurrentWorld;
        if (world == null) return;

        // Entity 생성
        Entity entity;
        using (var cmd = world.BeginWrite())
        {
            entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(0, 0, 0));
        }

        // GameObject와 Entity 연결
        var link = gameObject.CreateEntityLink(world, entity);
        Debug.Log($"Entity {entity.Id}가 {gameObject.name}에 연결되었습니다.");
    }
}
```

### 2. EntityViewRegistry를 통한 뷰 조회

```csharp
var world = KernelLocator.CurrentWorld;
var registry = EntityViewRegistry.For(world);

// Entity로 링크 찾기
if (registry.TryGetView(entity, out var link))
{
    Debug.Log($"Entity {entity.Id}의 GameObject: {link.gameObject.name}");
}

// 모든 뷰 순회
foreach (var (e, view) in registry.EnumerateViews())
{
    Debug.Log($"Entity {e.Id} → {view.gameObject.name}");
}
```

### 3. 링크 해제

```csharp
// 수동 해제
var link = GetComponent<EntityLink>();
if (link != null)
{
    link.Detach();
}

// 또는 GameObject 파괴 시 자동 해제됨
Destroy(gameObject);
```

---

## 주요 API

* **EntityLink**: GameObject와 Entity를 연결하는 MonoBehaviour
* **EntityLink.Attach(world, entity)**: 링크를 특정 World와 Entity에 연결
* **EntityLink.Detach()**: 링크 해제
* **EntityLink.IsAlive**: Entity가 살아있는지 확인
* **EntityViewRegistry.For(world)**: World별 뷰 레지스트리 가져오기
* **GameObject.CreateEntityLink()**: 편의 확장 메서드 (에디터 전용)

---

## 주의사항 및 모범 사례

* `EntityLink`는 GameObject당 **하나만** 존재할 수 있습니다 (`DisallowMultipleComponent`).
* GameObject가 파괴되면 자동으로 링크가 해제됩니다.
* `EntityViewRegistry`는 World별로 관리되며, World가 파괴되면 자동으로 정리됩니다.
* 에디터에서 `CreateEntityLink()`는 에디터 전용이며, 런타임에서는 직접 `EntityLink` 컴포넌트를 추가해야 합니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
