# ZenECS Unity Adapter 코드 리뷰

## 📋 개요

이 문서는 ZenECS Unity Adapter 패키지의 코드 리뷰 결과를 정리한 것입니다. 주요 Runtime 및 Editor 코드를 분석하여 코드 품질, 성능, 안정성, Unity 통합 관점에서 평가합니다.

---

## ✅ 강점 (Strengths)

### 1. **우수한 문서화**
- 모든 파일에 상세한 XML 문서 주석이 포함되어 있음
- 파일 헤더에 목적, 주요 개념, 저작권 정보가 명확히 명시됨
- 코드의 의도와 사용법을 이해하기 쉬움

### 2. **명확한 아키텍처**
- `EcsDriver`: Unity 생명주기와 ZenECS 커널을 연결하는 명확한 진입점
- `KernelLocator`: 글로벌 커널 접근을 위한 일관된 패턴
- `ZenEcsUnityBridge`: 정적 브릿지로 런타임/에디터 간 공유 서비스 제공
- 책임 분리가 잘 되어 있음

### 3. **유연한 DI 통합**
- Zenject 지원과 비-Zenject 모드를 모두 지원하는 조건부 컴파일
- `ProjectInstaller`가 두 모드를 모두 처리
- 의존성 주입 없이도 사용 가능한 경량 구현

### 4. **안전한 에러 처리**
- 대부분의 메서드에서 null 체크 수행
- 예외 발생 시 로그 경고로 처리하여 크래시 방지
- `TryGet` 패턴을 적절히 활용

### 5. **Unity 생명주기 통합**
- `DefaultExecutionOrder`로 초기화 순서 보장
- `DisallowMultipleComponent`로 중복 컴포넌트 방지
- `OnDestroy`에서 적절한 정리 작업 수행

---

## ⚠️ 개선 사항 (Areas for Improvement)

### 1. **EcsDriver.cs**

#### 🔴 심각도: 중간

**문제점:**
```csharp
// Line 180-187: 중복된 XML 주석
/// <summary>
/// Unity lifecycle callback invoked when the component or its GameObject
/// is being destroyed.
/// </summary>
/// <remarks>...</remarks>
/// <summary>  // ← 중복!
/// Handles world destruction events to clean up EntityViewRegistry.
/// </summary>
private void OnWorldDestroyed(IWorld world)
```

**권장 사항:**
- 중복된 XML 주석 제거
- `OnWorldDestroyed` 메서드의 주석을 단일 블록으로 통합

**문제점:**
```csharp
// Line 223: LateUpdate에서 interpolation alpha를 전달하지 않음
private void LateUpdate() => Kernel?.LateFrame();
```

**권장 사항:**
- `LateFrame(float interpolationAlpha)` 시그니처가 있다면 적절한 값을 전달
- Unity의 `Time.time` 기반 interpolation alpha 계산 고려

**문제점:**
```csharp
// Line 146-163: Awake에서 중복 드라이버 검색 로직
private void Awake()
{
#if UNITY_2022_2_OR_NEWER
    var first = FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
    var first = FindObjectOfType<EcsDriver>(true);
#endif
    if (first != null && first != this)
    {
        // 중복 처리
    }
}
```

**권장 사항:**
- `DisallowMultipleComponent`가 이미 있으므로, 이 검사는 중복일 수 있음
- 하지만 에디터에서 수동으로 추가된 경우를 대비한 방어적 코딩으로는 유용
- 주석으로 의도 명확화 권장

---

### 2. **KernelLocator.cs**

#### 🔴 심각도: 낮음

**문제점:**
```csharp
// Line 91-128: TryGetCurrent에서 캐시 무효화 시나리오 미처리
public static bool TryGetCurrent(out IKernel? kernel)
{
    if (_cached != null)
    {
        kernel = _cached;
        return true;  // ← 캐시된 커널이 dispose되었을 수 있음
    }
    // ...
}
```

**권장 사항:**
- 캐시된 커널의 유효성 검증 추가 (예: `IDisposable` 체크 또는 상태 플래그)
- 또는 `_cached`를 `WeakReference`로 변경하여 자동 정리

**문제점:**
```csharp
// Line 400-426: FindByAllTags의 복잡한 로직
public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
{
    // HashSet과 Dictionary를 사용한 교집합 계산
    // 가독성이 떨어짐
}
```

**권장 사항:**
- LINQ를 활용하여 더 간결하게 작성 가능:
```csharp
public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
{
    if (tags == null || tags.Length == 0) return Enumerable.Empty<IWorld>();
    
    return tags
        .Select(tag => Current.FindByTag(tag).ToHashSet())
        .Aggregate((acc, next) => acc.Intersect(next).ToHashSet())
        .SelectMany(id => Current.TryGet(id, out var world) ? new[] { world } : Enumerable.Empty<IWorld>())
        .Where(w => w != null);
}
```

---

### 3. **EntityViewRegistry.cs**

#### 🔴 심각도: 낮음

**문제점:**
```csharp
// Line 97-108: CleanupDeadWorlds가 실제로 아무 작업도 하지 않음
public static void CleanupDeadWorlds()
{
    // ConditionalWeakTable doesn't expose a way to enumerate keys,
    // so we can't directly clean up dead entries. The GC will handle it.
    // This method is kept for API consistency and future extensibility.
}
```

**권장 사항:**
- 메서드가 실제로 아무 작업도 하지 않는다면, 주석으로 명확히 표시하거나
- Obsolete 속성 추가 또는 제거 고려
- 또는 실제로 유용한 정리 로직이 있다면 구현

**문제점:**
```csharp
// Line 188-197: TryGet에서 null 체크가 중복됨
public bool TryGet(Entity e, out EntityLink? link)
{
    link = null;
    if (_map.TryGetValue(e, out var stored) && stored != null && stored.IsAlive)
    {
        link = stored;
        return true;
    }
    return false;
}
```

**권장 사항:**
- `_map`에 null이 저장되지 않도록 `Register`에서 보장한다면, `stored != null` 체크는 불필요할 수 있음
- 하지만 방어적 코딩 관점에서는 유지하는 것이 안전

---

### 4. **EntityBlueprint.cs**

#### 🔴 심각도: 중간

**문제점:**
```csharp
// Line 250-288: ShallowCopy에서 ICloneable 사용 시 예외 처리
if (source is ICloneable cloneable)
{
    try
    {
        return cloneable.Clone();
    }
    catch (Exception ex)
    {
        // Fallback to reflection
    }
}
```

**권장 사항:**
- `ICloneable.Clone()`이 실패하는 경우는 드물지만, fallback 로직이 적절함
- 하지만 `Clone()`이 `object?`를 반환하므로 null 체크 추가 권장:
```csharp
var cloned = cloneable.Clone();
if (cloned != null) return cloned;
```

**문제점:**
```csharp
// Line 192-196: ApplyBinders에서 shallow copy 후 SetApplyOrderAndAttachOrder 호출
var inst = (IBinder)ShallowCopy(b, b.GetType());
inst.SetApplyOrderAndAttachOrder(inst.ApplyOrder, b.AttachOrder);
```

**권장 사항:**
- `inst.ApplyOrder`와 `b.AttachOrder`를 사용하는데, `inst`는 복사본이므로 원본 값 사용이 의도된 것인지 확인 필요
- 주석으로 의도 명확화 권장

**문제점:**
```csharp
// Line 201-218: GetCachedFields에서 thread-safety 미고려
private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
```

**권장 사항:**
- 멀티스레드 환경에서의 동시 접근을 고려하여 `ConcurrentDictionary` 사용 또는 lock 추가
- Unity는 주로 단일 스레드이지만, 향후 확장성을 고려

---

### 5. **SystemPresetResolver.cs**

#### 🔴 심각도: 낮음

**문제점:**
```csharp
// Line 132-154: ZENECS_ZENJECT가 아닐 때와 있을 때 코드 중복
#if !ZENECS_ZENJECT
    // ... 로직
#else
    // 거의 동일한 로직
#endif
```

**권장 사항:**
- 공통 로직을 메서드로 추출하여 중복 제거:
```csharp
private List<ISystem> InstantiateSystemsInternal(List<Type> types, Func<Type, ISystem> factory)
{
    var kernel = ZenEcsUnityBridge.Kernel;
    var list = new List<ISystem>(types.Count);
    
    foreach (var t in types)
    {
        if (kernel is { CurrentWorld: not null } && kernel.CurrentWorld.TryGetSystem(t, out _))
            continue;
            
        try
        {
            list.Add(factory(t));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SystemPresetResolver] instantiate failed: {t?.Name} — {ex.Message}");
        }
    }
    
    return list;
}
```

---

### 6. **WorldSystemCreator.cs**

#### 🔴 심각도: 낮음

**문제점:**
```csharp
// Line 144: ctor 메서드명이 일반적이지 않음
[Inject]
void ctor([InjectOptional] ISystemPresetResolver worldPresetResolver)
```

**권장 사항:**
- Zenject의 `[Inject]` 메서드는 일반적으로 `Construct` 또는 명시적인 이름 사용
- `ctor`는 생성자처럼 보이지만 실제로는 메서드이므로 혼란 가능
- `Initialize` 또는 `InjectDependencies` 같은 이름 권장

**문제점:**
```csharp
// Line 352-362: AddDistinct가 중첩 메서드로 정의됨
static void AddDistinct(Type? t, HashSet<string> visited, List<Type> dst)
{
    // ...
}
```

**권장 사항:**
- 중첩 메서드는 가독성을 해칠 수 있음
- private static 메서드로 클래스 레벨로 이동 고려

---

## 🐛 잠재적 버그 (Potential Bugs)

### 1. **EcsDriver.cs - WorldDestroyed 이벤트 구독 해제 누락 가능성**

```csharp
// Line 98: 이벤트 구독
Kernel.WorldDestroyed += OnWorldDestroyed;

// Line 207: OnDestroy에서만 해제
Kernel.WorldDestroyed -= OnWorldDestroyed;
```

**문제:**
- `CreateKernel`이 여러 번 호출되면 (비록 idempotent하지만) 이벤트가 중복 구독될 수 있음
- 하지만 `Kernel != null` 체크로 방지되고 있으므로 실제 문제는 없을 가능성이 높음

**권장 사항:**
- 이벤트 구독 전에 기존 구독 해제:
```csharp
if (Kernel != null)
{
    Kernel.WorldDestroyed -= OnWorldDestroyed; // 안전하게 해제
}
Kernel.WorldDestroyed += OnWorldDestroyed;
```

### 2. **KernelLocator.cs - 캐시 무효화 타이밍**

```csharp
// Line 232-233: 도메인 리로드 시에만 캐시 초기화
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void ResetOnDomainReload() => _cached = null;
```

**문제:**
- 씬 전환 시 캐시가 유지될 수 있음
- 하지만 `EcsDriver`가 씬에 있으면 `TryGetCurrent`에서 다시 찾을 수 있으므로 큰 문제는 아님

**권장 사항:**
- 씬 전환 시에도 캐시 초기화 고려 (필요한 경우)

---

## 🚀 성능 개선 제안

### 1. **EntityViewRegistry - Dictionary 초기 용량**

```csharp
// Line 118: 초기 용량 미지정
private readonly Dictionary<Entity, EntityLink?> _map = new();
```

**권장 사항:**
- 예상 엔티티 수에 따라 초기 용량 지정:
```csharp
private readonly Dictionary<Entity, EntityLink?> _map = new(128);
```

### 2. **EntityBlueprint - FieldInfo 캐싱**

```csharp
// Line 201: 이미 캐싱이 구현되어 있음 - 좋음!
private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
```

**현재 상태:** 이미 최적화되어 있음 ✅

### 3. **KernelLocator - FindByAllTags 최적화**

현재 구현은 여러 번의 LINQ 쿼리를 수행하므로, 대량의 월드가 있을 때 성능 저하 가능

**권장 사항:**
- 캐싱 또는 인덱싱 고려 (필요한 경우)

---

## 📝 코드 스타일 및 일관성

### 1. **네이밍 컨벤션**
- 대부분 일관성 있게 `PascalCase` 사용 ✅
- `_cached`, `_map` 같은 private 필드 네이밍 일관성 ✅

### 2. **null 체크 패턴**
- `#nullable enable` 사용으로 null 안전성 향상 ✅
- `?.` 연산자 적절히 활용 ✅

### 3. **조건부 컴파일**
- `#if ZENECS_ZENJECT` 패턴이 일관되게 사용됨 ✅
- `#if UNITY_EDITOR` 적절히 사용됨 ✅

---

## 🔒 안전성 및 예외 처리

### 강점:
- 대부분의 public 메서드에서 null 체크 수행
- 예외 발생 시 로그 경고로 처리
- `TryGet` 패턴 적절히 활용

### 개선 필요:
- 일부 메서드에서 예외를 catch하고 무시하는 경우가 있음
- 예외 정보를 더 상세히 로깅하면 디버깅에 도움

---

## 🎯 Unity 통합 품질

### 강점:
- `DefaultExecutionOrder`로 초기화 순서 보장 ✅
- `DisallowMultipleComponent`로 중복 방지 ✅
- Unity 생명주기 메서드 적절히 활용 ✅
- Editor-only 코드 분리 ✅

### 개선 필요:
- `LateUpdate`에서 interpolation alpha 전달 고려
- 씬 전환 시 리소스 정리 로직 검토

---

## 📊 종합 평가

### 점수 (10점 만점)

| 항목 | 점수 | 비고 |
|------|------|------|
| 코드 품질 | 8.5/10 | 전반적으로 우수, 일부 중복 코드 존재 |
| 문서화 | 9.5/10 | 매우 상세하고 명확한 XML 주석 |
| 아키텍처 | 9.0/10 | 명확한 책임 분리와 모듈화 |
| 성능 | 8.0/10 | 대부분 최적화되어 있으나 일부 개선 여지 |
| 안정성 | 8.5/10 | 방어적 코딩이 잘 되어 있으나 일부 엣지 케이스 고려 필요 |
| Unity 통합 | 9.0/10 | Unity 생명주기와 잘 통합됨 |

**종합 점수: 8.8/10** ⭐⭐⭐⭐⭐

---

## 🎯 우선순위별 개선 권장 사항

### 높은 우선순위
1. ✅ **EcsDriver.cs**: 중복 XML 주석 제거
2. ✅ **EntityBlueprint.cs**: `ShallowCopy`에서 null 체크 추가
3. ✅ **SystemPresetResolver.cs**: 코드 중복 제거

### 중간 우선순위
4. ✅ **KernelLocator.cs**: 캐시 유효성 검증 추가
5. ✅ **WorldSystemCreator.cs**: `ctor` 메서드명 변경
6. ✅ **EntityViewRegistry.cs**: `CleanupDeadWorlds` 메서드 정리

### 낮은 우선순위
7. ✅ **KernelLocator.cs**: `FindByAllTags` LINQ 최적화
8. ✅ **EntityViewRegistry.cs**: Dictionary 초기 용량 지정
9. ✅ **EcsDriver.cs**: `LateUpdate` interpolation alpha 전달

---

## ✅ 결론

ZenECS Unity Adapter는 전반적으로 **매우 우수한 코드 품질**을 보여줍니다. 특히:

- 📚 **뛰어난 문서화**: 모든 코드가 상세히 문서화되어 있어 유지보수가 용이
- 🏗️ **명확한 아키텍처**: 책임 분리가 잘 되어 있고 모듈화가 우수
- 🔧 **유연한 설계**: Zenject 지원과 비-Zenject 모드를 모두 지원하는 유연한 구조
- 🛡️ **안전한 구현**: 방어적 코딩과 예외 처리가 잘 되어 있음

제시된 개선 사항들은 대부분 **코드 품질 향상**과 **유지보수성 개선**을 위한 것이며, 현재 코드도 프로덕션에서 사용하기에 충분히 안정적입니다.

---

**리뷰 일자**: 2026년  
**리뷰어**: AI Code Reviewer  
**버전**: 1.0.0
