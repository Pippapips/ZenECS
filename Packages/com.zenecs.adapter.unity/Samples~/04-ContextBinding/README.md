# ZenECS Adapter Unity — Sample 04: Context Binding 시스템

**Context Binding** 시스템을 사용하여 Shared Context와 Per-Entity Context를 관리하는 방법을 보여주는 샘플입니다.

* **Shared Context** — 여러 엔티티가 공유하는 Context
* **Per-Entity Context** — 엔티티별로 독립적인 Context
* **ISharedContextResolver** — Shared Context 해석 인터페이스
* **Context Assets** — ScriptableObject 기반 Context 설정

---

## 이 샘플이 보여주는 것

1. **Shared Context**
   여러 엔티티가 공유하는 Context를 생성하고 해석하는 방법을 보여줍니다.

2. **Per-Entity Context**
   각 엔티티마다 독립적인 Context를 생성하고 관리합니다.

3. **Context Assets**
   ScriptableObject를 사용하여 Context를 에셋으로 관리합니다.

4. **Context Resolver**
   `ISharedContextResolver`를 구현하여 Shared Context를 해석합니다.

---

## TL;DR 흐름

```
[SharedContextAsset]
  └─ ISharedContextResolver.Resolve()
      └─ IContext 반환
          └─ world.RegisterContext(entity, context)

[PerEntityContextAsset]
  └─ Create()
      └─ IContext 반환
          └─ world.RegisterContext(entity, context)

[Binder]
  └─ OnAttach(world, entity)
      └─ world.GetContext<T>(entity) 사용
```

---

## 파일 구조

```
04-ContextBinding/
├── README.md
├── ContextBindingSample.cs     # 샘플 스크립트
├── GameConfigContext.cs        # 예제 Shared Context
├── PlayerDataContext.cs        # 예제 Per-Entity Context
└── CustomContextResolver.cs    # 커스텀 Context Resolver
```

---

## 사용 방법

### 1. Context 인터페이스 정의

```csharp
using ZenECS.Core.Binding;

// Shared Context 예제
public interface IGameConfig : IContext
{
    float GameSpeed { get; }
    int MaxPlayers { get; }
}

// Per-Entity Context 예제
public interface IPlayerData : IContext
{
    string PlayerName { get; }
    int Score { get; set; }
}
```

### 2. Context 구현

```csharp
public class GameConfig : IGameConfig
{
    public float GameSpeed { get; set; } = 1f;
    public int MaxPlayers { get; set; } = 4;
}

public class PlayerData : IPlayerData
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
}
```

### 3. Shared Context Resolver 구현

```csharp
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;

public class CustomContextResolver : ISharedContextResolver
{
    private readonly Dictionary<SharedContextAsset, IContext> _cache = new();

    public IContext? Resolve(SharedContextAsset asset)
    {
        if (_cache.TryGetValue(asset, out var ctx))
            return ctx;

        // Shared Context 생성 및 캐싱
        if (asset.name == "GameConfig")
        {
            var config = new GameConfig { GameSpeed = 1.5f, MaxPlayers = 8 };
            _cache[asset] = config;
            return config;
        }

        return null;
    }
}
```

### 4. Context 사용

```csharp
var world = KernelLocator.CurrentWorld;

// Shared Context 등록
var gameConfig = new GameConfig { GameSpeed = 1.5f };
world.RegisterContext(entity1, gameConfig);
world.RegisterContext(entity2, gameConfig); // 같은 인스턴스 공유

// Per-Entity Context 등록
var playerData1 = new PlayerData { PlayerName = "Player1" };
var playerData2 = new PlayerData { PlayerName = "Player2" };
world.RegisterContext(entity1, playerData1);
world.RegisterContext(entity2, playerData2); // 다른 인스턴스

// Context 조회
var config = world.GetContext<IGameConfig>(entity1);
var player = world.GetContext<IPlayerData>(entity1);
```

---

## 주요 API

* **IContext**: Context 인터페이스
* **IWorld.RegisterContext()**: Context 등록
* **IWorld.GetContext<T>()**: Context 조회
* **ISharedContextResolver**: Shared Context 해석 인터페이스
* **SharedContextAsset**: Shared Context 마커 에셋
* **PerEntityContextAsset**: Per-Entity Context 팩토리 에셋

---

## 주의사항 및 모범 사례

* Shared Context는 **같은 인스턴스를 여러 엔티티가 공유**합니다.
* Per-Entity Context는 **각 엔티티마다 독립적인 인스턴스**를 가집니다.
* Context는 Binder의 `OnAttach()`에서 주로 사용됩니다.
* Shared Context는 `ISharedContextResolver`를 통해 해석되어야 합니다.
* Context는 엔티티가 파괴될 때 자동으로 정리됩니다.

---

## 라이선스

MIT © 2026 Pippapips Limited.
