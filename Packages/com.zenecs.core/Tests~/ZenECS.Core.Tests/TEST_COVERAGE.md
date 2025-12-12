# ZenECS Core 테스트 커버리지 분석

## 현재 테스트 파일

1. **SystemLifecycleAndEnableTests.cs**
   - ✅ 시스템 추가/제거
   - ✅ 시스템 enable/disable

2. **ExternalCommandTests.cs**
   - ✅ 외부 커맨드 플러시

3. **WorldComponentApiTests.cs**
   - ✅ 컴포넌트 추가/교체/삭제
   - ✅ HasComponent, TryReadComponent, ReadComponent

4. **WorldResetAndGenerationTests.cs**
   - ✅ 엔티티 ID 재사용
   - ✅ Generation bump
   - ✅ World.Reset()

5. **WorldAliveCountTests.cs**
   - ✅ AliveCount 추적
   - ✅ Reset 후 AliveCount

6. **WorldSnapshotTests.cs**
   - ✅ 스냅샷 저장/로드
   - ✅ 엔티티 및 컴포넌트 보존
   - ✅ Generation 보존
   - ✅ FreeIds 보존
   - ✅ NextId 보존
   - ✅ 빈 World 스냅샷
   - ✅ 포맷터 없을 때 예외 처리
   - ✅ Post-load migration

7. **WorldQueryTests.cs**
   - ✅ 기본 쿼리 (Query<T1>, Query<T1, T2>, Query<T1, T2, T3>, Query<T1, T2, T3, T4>)
   - ✅ Filter.With<T>() - 추가 컴포넌트 필터
   - ✅ Filter.Without<T>() - 제외 컴포넌트 필터
   - ✅ Filter.WithAny() - OR 그룹 필터
   - ✅ Filter.WithoutAny() - NOT-OR 그룹 필터
   - ✅ 복합 필터 조합
   - ✅ 다중 컴포넌트 쿼리 (T1..T8 모두 테스트됨)
   - ✅ 빈 결과 쿼리
   - ✅ 컴포넌트 변경 후 쿼리 업데이트
   - ✅ 다중 컴포넌트 쿼리 + 필터 조합

## 누락된 테스트 영역

### 1. Query API (IWorldQueryApi) - **완료**
   - ✅ 기본 쿼리 (Query<T1>, Query<T1, T2>, 등) - T1..T8 모두 테스트됨
   - ✅ Filter.With<T>() - 추가 컴포넌트 필터
   - ✅ Filter.Without<T>() - 제외 컴포넌트 필터
   - ✅ Filter.WithAny() - OR 그룹 필터
   - ✅ Filter.WithoutAny() - NOT-OR 그룹 필터
   - ✅ 복합 필터 조합
   - ✅ 다중 컴포넌트 쿼리 (T1..T8) - 모두 테스트됨

### 2. Messages API (IWorldMessagesApi) - **높은 우선순위**
   - ❌ Subscribe<T>() - 메시지 구독
   - ❌ Publish<T>() - 메시지 발행
   - ❌ 구독 해제 (IDisposable)
   - ❌ 다중 구독자
   - ❌ World 간 메시지 격리
   - ⚠️ README에 언급되어 있지만 테스트 파일이 없음

### 3. Singleton Components (IWorldSingletonComponent) - **중간 우선순위**
   - ❌ SetSingleton<T>()
   - ❌ GetSingleton<T>()
   - ❌ TryGetSingleton<T>()
   - ❌ RemoveSingleton<T>()
   - ❌ HasSingleton(Entity)
   - ❌ GetAllSingletons()
   - ❌ 싱글톤 위반 감지 (여러 엔티티에 같은 싱글톤 컴포넌트)

### 4. QuerySpan API (IWorldQuerySpanApi) - **중간 우선순위**
   - ❌ QuerySpan<T1>() - 스팬 기반 쿼리
   - ❌ QuerySpan<T1, T2>() 등
   - ❌ Filter와 함께 사용

### 5. Component API 추가 기능 - **낮은 우선순위**
   - ❌ SnapshotComponent<T>() - 스냅샷 델타 디스패치
   - ❌ ReplaceComponent - 이미 일부 테스트됨
   - ❌ Boxed 버전 API들 (HasComponentBoxed, ReadComponentBoxed 등)

### 6. Context API (IWorldContextApi) - **낮은 우선순위**
   - ❌ 컨텍스트 등록/해제
   - ❌ 컨텍스트 조회

### 7. Binder API (IWorldBinderApi) - **낮은 우선순위**
   - ❌ 바인더 등록/해제
   - ❌ 바인더 조회

### 8. Hook API (IWorldHookApi) - **낮은 우선순위**
   - ❌ 훅 등록/해제
   - ❌ 훅 실행

### 9. Worker API (IWorldWorkerApi) - **낮은 우선순위**
   - ❌ 워커 관리
   - ❌ 스케줄링

### 10. Command Buffer API 추가 - **낮은 우선순위**
   - ❌ DestroyAllEntities()
   - ❌ SetSingleton() via command buffer
   - ❌ RemoveSingleton() via command buffer

### 11. Entity API 추가 - **낮은 우선순위**
   - ❌ GetAllEntities() - 이미 일부 사용됨
   - ❌ IsAlive() - 이미 일부 사용됨

## 권장 테스트 추가 순서

1. ✅ **WorldQueryTests.cs** - Query API는 핵심 기능 (완료)
2. **WorldMessagesTests.cs** - README에 언급된 기능 (다음 우선순위)
3. **WorldSingletonTests.cs** - 싱글톤 컴포넌트는 특수한 케이스
4. **WorldQuerySpanTests.cs** - 성능 최적화된 쿼리
5. 나머지는 필요에 따라 추가

