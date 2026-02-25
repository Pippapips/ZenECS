# ZenECS 코드 품질 평가

> 평가일: 2026-02-25
> 대상: ZenECS Core v1.1.0 / Adapter Unity v1.0.2
> 평가 도구: .NET SDK 8.0.418, xUnit 2.9.0

---

## 종합 등급: A- (우수)

---

## 1. 코드 규모

| 영역 | 파일 수 | 코드 라인 |
|------|--------|----------|
| Core Runtime | 90 | 16,990 |
| Core Tests | 19 | 3,982 |
| Core Samples | 48 | 3,003 |
| Unity Adapter | 23 | 4,311 |
| **합계** | **180+** | **28,286** |

Runtime:Test 비율 약 **4.3:1** — 라이브러리 프로젝트로서 적절한 수준.

---

## 2. 아키텍처 (A)

### 강점

- **Partial class 분리**: `World.cs`가 12개 파일(`WorldEntityApi.cs`, `WorldQueryApi.cs`, `WorldComponentApi.cs` 등)로 관심사별 분리되어 유지보수성이 매우 높음
- **인터페이스 분리 원칙(ISP)**: `IWorld`가 11개 세분화된 인터페이스를 합성 (`IWorldQueryApi`, `IWorldComponentApi`, `IWorldEntityApi` 등)
- **DI 컨테이너**: 자체 `ServiceContainer`로 World별 서비스 스코프 관리
- **결정론적 실행**: `BeginFrame → FixedStep×N → LateFrame` 패턴으로 시뮬레이션 재현성 보장
- **엔진 무관**: Core가 Unity/Godot에 의존하지 않음 (.NET Standard 2.1)

### 개선 가능

- `CommandBuffer`에서 `_world is not World world` 다운캐스팅이 3곳에서 반복됨 — 내부 인터페이스(`IWorldInternal`) 추출 고려

---

## 3. 코드 스타일 (A+)

### 일관성

- `#nullable enable` — 모든 90+ 파일에 적용
- `readonly struct` — `Entity`, `WorldId`, `Filter`, `QueryEnumerable<T>` 등 20+ 타입
- `internal sealed` — 구현 클래스는 모두 내부+봉인, public API는 인터페이스와 값 타입만 노출
- `_camelCase` 필드, `PascalCase` 공개 API — 예외 없음
- `[MethodImpl(AggressiveInlining)]` — `BitSet.Get/Set` 같은 핫패스에 적절히 적용

### 예시 — Entity.cs

```csharp
public readonly struct Entity : IEquatable<Entity>
{
    public readonly ulong Handle;
    public int Id  => (int)(Handle & IdMask);
    public int Gen => (int)(Handle >> GenShift);
}
```

64비트 핸들에 Generation|ID를 팩킹하는 설계가 간결하고 효율적.

---

## 4. 문서화 (A)

- **XML 주석**: public API 전체에 `<summary>`, `<remarks>`, `<param>` 태그 적용
- **파일 헤더**: 모든 파일에 목적, 핵심 개념, 라이선스 명시
- **81개 문서 페이지**: `Docs/` 아래 아키텍처, 가이드, 레퍼런스, 샘플 설명 완비
- **DocFX 연동**: API 레퍼런스 자동 생성 파이프라인 구축

---

## 5. 테스트 (B+)

### 강점

- 147개 테스트 **전수 통과**
- 기능별 테스트 파일 분리 (Query, Component, Snapshot, Messages, Hooks 등 17개 파일)
- `TestWorldHost` — 깔끔한 테스트 인프라 (Kernel/World 셋업, `TickFrame` 헬퍼)

### 개선 가능

- **에러 케이스 부족**: null 인자, disposed 객체, 잘못된 Entity 접근 등의 방어 테스트 부족
- **스레드 안전성 테스트 없음**: `ConcurrentDictionary`를 쓰는 Kernel의 동시성 검증 부재
- **엣지 케이스**: 빈 World, 용량 한계, 대량 Entity 스트레스 테스트 부재
- **코드 커버리지 도구 미설정**: `coverlet` 패키지 없어 정량적 커버리지 측정 불가

---

## 6. 성능 설계 (A-)

### 강점

- `BitSet` — 비트 연산 기반 O(1) Entity 활성 여부 확인
- **Smallest-pool seeding** — Query 시 가장 작은 풀부터 스캔하여 반복 최소화
- **struct Enumerable** — `QueryEnumerable<T>` 값 타입으로 힙 할당 제로
- **ConcurrentQueue** 기반 CommandBuffer — lock-free 명령 큐잉
- **BenchmarkDotNet 벤치마크 프로젝트** 포함

### 개선 가능

- `BitSet`에 스레드 안전 주의사항만 있고 `Interlocked` 대안 없음
- `CommandBuffer.IOp` 구현체가 class(힙 할당) — 구조체 기반 커맨드 풀링 고려 가능

---

## 7. 보안/안정성 (A-)

- Generation 기반 Entity 핸들 — stale 참조 방지
- CommandBuffer 적용 시 `if (!w.IsAlive(_e)) return` — 죽은 Entity 안전 무시
- `Dispose` 패턴 준수 — 중복 호출 방어 (`if (_disposed) return`)
- `ArgumentNullException` 적절히 사용

---

## 종합 평가표

| 항목 | 등급 | 요약 |
|------|------|------|
| 아키텍처 | **A** | ISP, DI, 결정론적 실행, 엔진 무관 설계 |
| 코드 스타일 | **A+** | nullable, readonly struct, 네이밍 100% 일관 |
| 문서화 | **A** | XML 주석 + 81페이지 문서 + DocFX |
| 테스트 | **B+** | 147개 전수 통과, 에러/동시성 케이스 부족 |
| 성능 설계 | **A-** | 제로 할당 Query, BitSet, 벤치마크 포함 |
| 보안/안정성 | **A-** | Generation 핸들, safe no-op, Dispose 방어 |
| **종합** | **A-** | **프로덕션 레디 수준의 라이브러리** |

---

## 핵심 개선 권장사항 (우선순위순)

1. **테스트 커버리지 확대** — 에러 케이스, 동시성, 엣지 케이스 추가 + `coverlet` 도입
2. **CommandBuffer 다운캐스팅 제거** — `IWorldInternal` 내부 인터페이스 추출
3. **CommandBuffer IOp를 struct 기반으로** — 대량 명령 시 GC 압력 감소
4. **CI에 `dotnet format` 추가** — 코드 스타일 자동 강제
