# ZenECS Unity Adapter 코드 리뷰

## 📋 개요

ZenECS Unity Adapter는 ZenECS Core를 Unity 엔진과 통합하는 어댑터 레이어입니다. 이 리뷰는 주요 컴포넌트들의 설계, 구현 품질, 잠재적 개선 사항을 다룹니다.

---

## ✅ 강점 (Strengths)

### 1. **명확한 아키텍처 분리**
- **EcsDriver**: Unity 생명주기와 ECS 커널을 연결하는 단일 책임 컴포넌트
- **KernelLocator**: 전역 커널 접근 패턴을 제공하면서도 자동 생성 기능 포함
- **ZenEcsUnityBridge**: 정적 브리지로 런타임/에디터 코드 간 통신 지원

### 2. **우수한 문서화**
- 모든 주요 클래스와 메서드에 상세한 XML 문서 주석
- 목적, 사용법, 주의사항이 명확히 기술됨
- 코드 자체가 문서 역할을 잘 수행

### 3. **유연한 DI 지원**
- Zenject와 비-Zenject 모드를 모두 지원 (`#if ZENECS_ZENJECT`)
- `ProjectInstaller`가 두 모드를 깔끔하게 처리
- 의존성 주입이 선택적이면서도 강력함

### 4. **안전한 생명주기 관리**
- `EcsDriver`에서 중복 인스턴스 자동 제거
- `OnDestroy`에서 이벤트 구독 해제 및 리소스 정리
- `EntityViewRegistry`에서 `ConditionalWeakTable` 사용으로 메모리 누수 방지

### 5. **에디터 안전성**
- `OnValidate`에서 컴포넌트를 숨겨 실수로 수정하는 것 방지
- 에디터 전용 확장 메서드가 `#if UNITY_EDITOR`로 보호됨

---

## ⚠️ 개선 필요 사항 (Issues & Recommendations)

### 1. **EcsDriver.cs - 중복된 XML 주석**

**위치**: `EcsDriver.cs` 라인 169-201

**문제**:
```csharp
/// <summary>
/// Unity lifecycle callback invoked when the component or its GameObject
/// is being destroyed.
/// </summary>
/// <remarks>
/// If a kernel instance is owned by this driver, it is:
/// ...
/// </remarks>
/// <summary>  // ← 중복된 <summary> 태그
/// Handles world destruction events to clean up EntityViewRegistry.
/// </summary>
```

**해결책**:
- `OnWorldDestroyed` 메서드의 XML 주석을 별도로 분리하거나, 첫 번째 `<summary>`를 제거하고 `OnWorldDestroyed`에만 주석을 남김

**권장 수정**:
```csharp
/// <summary>
/// Unity lifecycle callback invoked when the component or its GameObject
/// is being destroyed.
/// </summary>
/// <remarks>
/// If a kernel instance is owned by this driver, it is:
/// <list type="bullet">
/// <item><description>Unsubscribed from world destruction events.</description></item>
/// <item><description>Detached from <see cref="KernelLocator"/>.</description></item>
/// <item><description>Disposed to release any managed resources.</description></item>
/// <item><description>Cleared from <see cref="Kernel"/> to avoid reuse.</description></item>
/// </list>
/// </remarks>
private void OnDestroy() { ... }

/// <summary>
/// Handles world destruction events to clean up EntityViewRegistry.
/// </summary>
/// <param name="world">The world that was destroyed.</param>
private void OnWorldDestroyed(IWorld world) { ... }
```

---

### 2. **KernelLocator.cs - 예외 처리 일관성**

**위치**: `KernelLocator.cs` 라인 308-321

**문제**:
- `SetCurrentWorld`에서 예외를 잡아 `false`를 반환하지만, 다른 메서드들(`Current`, `CurrentWorld`)은 예외를 그대로 전파
- 일관성 없는 예외 처리 패턴

**현재 코드**:
```csharp
public static bool SetCurrentWorld(IWorld w)
{
    if (w == null) return false;
    try
    {
        Current.SetCurrentWorld(w);
        return true;
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[KernelLocator] Failed to set current world: {ex.Message}");
        return false;
    }
}
```

**권장 개선**:
- `SetCurrentWorld`는 현재처럼 안전하게 처리하는 것이 합리적
- 하지만 `Current` 프로퍼티가 예외를 던지는 것과의 일관성을 위해, `TrySetCurrentWorld`와 `SetCurrentWorld`를 분리하는 것을 고려

---

### 3. **KernelLocator.cs - FindByAllTags 성능**

**위치**: `KernelLocator.cs` 라인 400-426

**문제**:
- `FindByAllTags`에서 매 태그마다 `FindByTag`를 호출하고 딕셔너리를 생성
- 태그가 많을 경우 비효율적일 수 있음

**현재 구현**:
```csharp
public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
{
    if (tags == null || tags.Length == 0) return Enumerable.Empty<IWorld>();
    
    HashSet<WorldId>? acc = null;
    Dictionary<WorldId, IWorld>? lastMap = null;
    
    foreach (var t in tags)
    {
        var list = Current.FindByTag(t) ?? Enumerable.Empty<IWorld>();
        var map = list.Where(w => w != null).ToDictionary(w => w.Id, w => w);
        // ...
    }
}
```

**권장 개선**:
- 첫 번째 태그 결과를 기준으로 필터링하고, 이후 태그들은 `acc`에 있는 ID만 확인
- 불필요한 딕셔너리 생성 최소화

**개선안**:
```csharp
public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
{
    if (tags == null || tags.Length == 0) return Enumerable.Empty<IWorld>();
    
    HashSet<WorldId>? candidateIds = null;
    Dictionary<WorldId, IWorld>? worldMap = null;
    
    foreach (var tag in tags)
    {
        var worlds = Current.FindByTag(tag) ?? Enumerable.Empty<IWorld>();
        var currentIds = new HashSet<WorldId>();
        var currentMap = new Dictionary<WorldId, IWorld>();
        
        foreach (var w in worlds)
        {
            if (w == null) continue;
            currentIds.Add(w.Id);
            currentMap[w.Id] = w;
        }
        
        if (candidateIds == null)
        {
            // 첫 번째 태그: 전체를 후보로 설정
            candidateIds = currentIds;
            worldMap = currentMap;
        }
        else
        {
            // 교집합만 유지
            candidateIds.IntersectWith(currentIds);
            // worldMap도 업데이트 (교집합에 있는 것만)
            var newMap = new Dictionary<WorldId, IWorld>();
            foreach (var id in candidateIds)
            {
                if (currentMap.TryGetValue(id, out var w))
                    newMap[id] = w;
            }
            worldMap = newMap;
        }
        
        // 조기 종료: 후보가 없으면 더 이상 진행할 필요 없음
        if (candidateIds.Count == 0)
            return Enumerable.Empty<IWorld>();
    }
    
    if (candidateIds == null || worldMap == null)
        return Enumerable.Empty<IWorld>();
    
    return candidateIds.Where(worldMap.ContainsKey).Select(id => worldMap[id]);
}
```

---

### 4. **WorldSystemCreator.cs - 에러 처리 개선**

**위치**: `WorldSystemCreator.cs` 라인 231-249

**문제**:
- 에러가 발생해도 경고만 로그하고 계속 진행
- 사용자가 에러를 놓치기 쉬움

**현재 코드**:
```csharp
var types = CollectDistinctTypes(out var errors);
if (errors.Count > 0)
{
    Debug.LogWarning(
        $"[WorldSystemCreator] Encountered {errors.Count} error(s) while collecting system types. " +
        "Some systems may not be registered. Check the console for details.");
}
```

**권장 개선**:
- 에러 메시지를 더 구체적으로 로그 (어떤 시스템이 실패했는지)
- 옵션으로 에러 발생 시 시스템 등록을 중단하는 모드 제공

---

### 5. **EntityBlueprint.cs - ShallowCopy 성능**

**위치**: `EntityBlueprint.cs` 라인 250-288

**문제**:
- 리플렉션 기반 복사가 빈번히 호출될 경우 성능 이슈 가능
- `ICloneable`을 우선 사용하지만, 그렇지 않은 경우 리플렉션 사용

**현재 구현**:
- 필드 정보 캐싱으로 일부 최적화됨
- 하지만 여전히 매번 `GetValue`/`SetValue` 호출

**권장 개선**:
- 고성능 시나리오를 위해 `MemberwiseClone` 사용 고려 (단, 모든 필드가 복사되어야 하는 경우 제한적)
- 또는 `System.Text.Json`이나 `Newtonsoft.Json`을 사용한 직렬화/역직렬화 방식 고려 (더 느릴 수 있지만 안전함)

---

### 6. **EntityViewRegistry.cs - CleanupDeadWorlds 구현**

**위치**: `EntityViewRegistry.cs` 라인 97-108

**문제**:
- `CleanupDeadWorlds` 메서드가 실제로 아무 작업도 하지 않음
- 주석에 "GC가 자동으로 처리한다"고 명시되어 있지만, API 일관성을 위해 빈 메서드로 남아있음

**현재 코드**:
```csharp
public static void CleanupDeadWorlds()
{
    // ConditionalWeakTable automatically removes entries when the key is GC'd,
    // so we don't need to manually iterate...
    // This method is kept for API consistency and future extensibility.
}
```

**권장 개선**:
- 메서드를 제거하거나 `[Obsolete]`로 표시
- 또는 실제로 유용한 정리 작업이 있다면 구현 (예: 명시적으로 null 체크 후 제거)

---

### 7. **SystemPresetResolver.cs - 중복 코드**

**위치**: `SystemPresetResolver.cs` 라인 130-181

**문제**:
- `#if !ZENECS_ZENJECT`와 `#else` 블록에서 거의 동일한 로직이 반복됨
- 유지보수 시 두 곳을 모두 수정해야 함

**현재 코드**:
```csharp
#if !ZENECS_ZENJECT
    var kernel = ZenEcsUnityBridge.Kernel;
    var list = new List<ISystem>(types.Count);
    
    foreach (var t in types)
    {
        if (kernel is { CurrentWorld: not null })
        {
            if (kernel.CurrentWorld.TryGetSystem(t, out ISystem? system))
                continue;
        }
        
        try
        {
            list.Add((ISystem)Activator.CreateInstance(t));
        }
        catch (Exception ex) { ... }
    }
    return list;
#else
    // 거의 동일한 코드, 단지 _container.Instantiate(t)만 다름
#endif
```

**권장 개선**:
- 공통 로직을 별도 메서드로 추출
- 인스턴스 생성 부분만 분기

**개선안**:
```csharp
public List<ISystem> InstantiateSystems(List<Type> types)
{
    var kernel = ZenEcsUnityBridge.Kernel;
    var list = new List<ISystem>(types.Count);
    
    foreach (var t in types)
    {
        if (kernel is { CurrentWorld: not null })
        {
            if (kernel.CurrentWorld.TryGetSystem(t, out ISystem? system))
                continue;
        }
        
        try
        {
            var instance = CreateSystemInstance(t);
            if (instance != null)
                list.Add(instance);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SystemPresetResolver] instantiate failed: {t?.Name} — {ex.Message}");
        }
    }
    
    return list;
}

private ISystem CreateSystemInstance(Type t)
{
#if ZENECS_ZENJECT
    if (_container == null)
        throw new InvalidOperationException("Container is null in Zenject mode");
    return (ISystem)_container.Instantiate(t);
#else
    return (ISystem)Activator.CreateInstance(t);
#endif
}
```

---

### 8. **EntityLink.cs - Attach 메서드 안전성**

**위치**: `EntityLink.cs` 라인 67-77

**문제**:
- `Attach`에서 이전 월드의 언레지스터는 하지만, 새 월드가 `null`일 수 있는 경우 처리 부족

**현재 코드**:
```csharp
public void Attach(IWorld w, in Entity e)
{
    if (World != null)
        EntityViewRegistry.For(World).Unregister(Entity, this);
    
    World = w;
    Entity = e;
    
    if (World != null)
        EntityViewRegistry.For(World).Register(Entity, this);
}
```

**권장 개선**:
- `w`가 `null`인 경우 명시적으로 처리하거나 예외 발생
- 또는 `w`가 `null`이면 `Detach`만 수행

**개선안**:
```csharp
public void Attach(IWorld w, in Entity e)
{
    // 이전 링크 해제
    if (World != null)
        EntityViewRegistry.For(World).Unregister(Entity, this);
    
    // 새 링크 설정
    if (w == null)
    {
        World = null;
        Entity = default;
        return;
    }
    
    World = w;
    Entity = e;
    EntityViewRegistry.For(World).Register(Entity, this);
}
```

---

## 🔍 추가 검토 사항

### 1. **스레드 안전성**
- 대부분의 정적 클래스들(`KernelLocator`, `ZenEcsUnityBridge`, `EntityViewRegistry`)이 스레드 안전하지 않음
- Unity는 기본적으로 단일 스레드이지만, 향후 멀티스레드 지원 시 문제가 될 수 있음
- **권장**: 필요 시 `lock` 또는 `ConcurrentDictionary` 사용 고려

### 2. **널 안전성**
- `#nullable enable`을 사용하고 있지만, 일부 메서드에서 널 체크가 부족할 수 있음
- 예: `KernelLocator.Current`에서 `TryGetCurrent`가 실패하면 예외 발생

### 3. **성능 최적화 기회**
- `KernelLocator.AllWorlds`에서 `FindByNamePrefix(string.Empty)` 사용 - 빈 문자열로 모든 월드 검색이 효율적인지 확인 필요
- `EntityBlueprint.ShallowCopy`의 리플렉션 사용 - 고빈도 호출 시 성능 병목 가능

### 4. **테스트 커버리지**
- Unity Adapter 코드에 대한 단위 테스트가 보이지 않음
- 특히 `KernelLocator`, `EntityViewRegistry`, `WorldSystemCreator` 등 핵심 컴포넌트의 테스트 필요

---

## 📊 종합 평가

### 코드 품질: ⭐⭐⭐⭐ (4/5)

**강점**:
- 명확한 아키텍처와 책임 분리
- 우수한 문서화
- 유연한 DI 지원
- 안전한 생명주기 관리

**개선 필요**:
- 일부 중복 코드 제거
- 예외 처리 일관성 향상
- 성능 최적화 기회
- 테스트 커버리지 확대

### 권장 우선순위

1. **높음**: `EcsDriver` XML 주석 중복 수정, `SystemPresetResolver` 중복 코드 제거
2. **중간**: `KernelLocator.FindByAllTags` 성능 개선, `EntityLink.Attach` 안전성 강화
3. **낮음**: `EntityViewRegistry.CleanupDeadWorlds` 정리, 전체적인 테스트 추가

---

## 🎯 결론

ZenECS Unity Adapter는 전반적으로 잘 설계되고 구현된 코드입니다. 명확한 아키텍처, 우수한 문서화, 유연한 DI 지원이 인상적입니다. 몇 가지 작은 개선 사항들이 있지만, 전체적으로 프로덕션 사용에 적합한 수준입니다.

주요 개선 사항들을 적용하면 코드 품질과 유지보수성이 더욱 향상될 것입니다.
