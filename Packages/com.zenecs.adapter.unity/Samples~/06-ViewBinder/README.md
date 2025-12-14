# ZenECS Adapter Unity — Sample 06: View Binder 패턴

**View Binder** 패턴을 사용하여 ECS 데이터를 Unity 뷰에 바인딩하는 방법을 보여주는 샘플입니다.

* **IBinder** — 뷰 바인딩 인터페이스
* **IUnityViewBinder** — Unity 특화 뷰 바인더
* **ApplyOrder / AttachOrder** — 바인더 실행 순서 제어
* **읽기 전용 업데이트** — FrameViewGroup에서 데이터를 읽어 뷰에 반영

---

## 이 샘플이 보여주는 것

1. **Binder 구현**
   `IBinder` 또는 `IUnityViewBinder`를 구현하여 ECS 컴포넌트를 Unity GameObject에 바인딩합니다.

2. **바인더 등록**
   `IWorld.AttachBinder()`를 사용하여 엔티티에 바인더를 연결합니다.

3. **읽기 전용 업데이트**
   FrameViewGroup 시스템에서 컴포넌트를 읽어 뷰를 업데이트합니다 (쓰기 금지).

---

## TL;DR 흐름

```
[Entity]
  └─ AttachBinder(binder)
      └─ binder.OnAttach(world, entity)
          └─ 뷰 초기화

[FrameViewGroup System]
  └─ binder.Apply(world, entity, alpha)
      └─ 컴포넌트 읽기 → 뷰 업데이트 (읽기 전용)
```

---

## 파일 구조

```
06-ViewBinder/
├── README.md
├── ViewBinderSample.cs          # 샘플 스크립트
├── PositionBinder.cs            # Position → Transform 바인더
└── HealthBinder.cs              # Health → UI 바인더
```

---

## 사용 방법

### 1. Binder 구현

```csharp
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;

public class PositionBinder : IBinder
{
    private Transform? _transform;
    public int ApplyOrder => 0;
    public int AttachOrder => 0;

    public void OnAttach(IWorld w, Entity e)
    {
        // EntityLink를 통해 GameObject 찾기
        var registry = EntityViewRegistry.For(w);
        if (registry.TryGetView(e, out var link))
        {
            _transform = link.transform;
        }
    }

    public void OnDetach(IWorld w, Entity e)
    {
        _transform = null;
    }

    public void Apply(IWorld w, Entity e, float alpha)
    {
        if (_transform == null || !w.HasComponent<Position>(e)) return;

        var pos = w.ReadComponent<Position>(e);
        _transform.position = new Vector3(pos.X, pos.Y, pos.Z);
    }
}
```

### 2. Binder 등록

```csharp
var world = KernelLocator.CurrentWorld;

Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0, 0));
}

// Binder 등록
var binder = new PositionBinder();
world.AttachBinder(entity, binder);
```

### 3. FrameViewGroup에서 바인더 적용

```csharp
[FrameViewGroup]
public sealed class ViewUpdateSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // 모든 엔티티의 바인더 적용
        foreach (var entity in w.Query<Entity>())
        {
            w.ApplyBinders(entity, alpha: 1f);
        }
    }
}
```

---

## 주요 API

* **IBinder**: 뷰 바인딩 인터페이스
* **IUnityViewBinder**: Unity 특화 뷰 바인더 인터페이스
* **IWorld.AttachBinder()**: 바인더 등록
* **IWorld.ApplyBinders()**: 바인더 적용
* **IWorld.DetachBinder()**: 바인더 제거
* **ApplyOrder / AttachOrder**: 바인더 실행 순서

---

## 주의사항 및 모범 사례

* Binder는 **읽기 전용**으로 동작해야 합니다. ECS 데이터를 수정하지 마세요.
* `Apply()`는 **FrameViewGroup**에서만 호출되어야 합니다.
* `OnAttach()`에서 EntityLink를 통해 GameObject를 찾을 수 있습니다.
* 여러 바인더의 실행 순서는 `ApplyOrder`로 제어할 수 있습니다.
* Binder는 엔티티가 파괴될 때 자동으로 `OnDetach()`가 호출됩니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
