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

8. **WorldMessagesTests.cs**
   - ✅ Subscribe<T>() - 메시지 구독
   - ✅ Publish<T>() - 메시지 발행
   - ✅ 구독 해제 (IDisposable)
   - ✅ 다중 구독자
   - ✅ World 간 메시지 격리
   - ✅ 메시지 타입별 격리
   - ✅ 메시지 FIFO 순서
   - ✅ 여러 번의 펌프
   - ✅ 빈 큐 처리

9. **WorldSingletonTests.cs**
   - ✅ SetSingleton<T>() - 싱글톤 설정
   - ✅ GetSingleton<T>() - 싱글톤 가져오기
   - ✅ TryGetSingleton<T>() - 싱글톤 시도 가져오기
   - ✅ RemoveSingleton<T>() - 싱글톤 제거
   - ✅ HasSingleton(Entity) - 엔티티가 싱글톤 소유자인지 확인
   - ✅ GetAllSingletons() - 모든 싱글톤 가져오기
   - ✅ 싱글톤 위반 감지 (여러 엔티티에 같은 싱글톤 컴포넌트)
   - ✅ SetSingleton으로 업데이트
   - ✅ CommandBuffer를 통한 SetSingleton/RemoveSingleton
   - ✅ 엔티티 파괴 시 싱글톤 인덱스 업데이트

10. **WorldQuerySpanTests.cs**
   - ✅ QueryToSpan<T1>() - 단일 컴포넌트 스팬 수집
   - ✅ QueryToSpan<T1, T2>() - 다중 컴포넌트 스팬 수집
   - ✅ QueryToSpan<T1, T2, T3, T4>() - 4개 컴포넌트 스팬 수집
   - ✅ QueryToSpan과 Filter 조합
   - ✅ Span 용량 제한 처리
   - ✅ Process<T>() - ref로 컴포넌트 처리
   - ✅ Process가 dead entity 스킵
   - ✅ Process가 컴포넌트 없는 엔티티 스킵
   - ✅ QueryToSpan + Process 워크플로우
   - ✅ 빈 스팬 처리

## 누락된 테스트 영역

### 1. Query API (IWorldQueryApi) - **완료**
   - ✅ 기본 쿼리 (Query<T1>, Query<T1, T2>, 등) - T1..T8 모두 테스트됨
   - ✅ Filter.With<T>() - 추가 컴포넌트 필터
   - ✅ Filter.Without<T>() - 제외 컴포넌트 필터
   - ✅ Filter.WithAny() - OR 그룹 필터
   - ✅ Filter.WithoutAny() - NOT-OR 그룹 필터
   - ✅ 복합 필터 조합
   - ✅ 다중 컴포넌트 쿼리 (T1..T8) - 모두 테스트됨

### 2. Messages API (IWorldMessagesApi) - **완료**
   - ✅ Subscribe<T>() - 메시지 구독
   - ✅ Publish<T>() - 메시지 발행
   - ✅ 구독 해제 (IDisposable)
   - ✅ 다중 구독자
   - ✅ World 간 메시지 격리
   - ✅ 메시지 타입별 격리
   - ✅ 메시지 FIFO 순서

### 3. Singleton Components (IWorldSingletonComponent) - **완료**
   - ✅ SetSingleton<T>() - 싱글톤 설정
   - ✅ GetSingleton<T>() - 싱글톤 가져오기
   - ✅ TryGetSingleton<T>() - 싱글톤 시도 가져오기
   - ✅ RemoveSingleton<T>() - 싱글톤 제거
   - ✅ HasSingleton(Entity) - 엔티티가 싱글톤 소유자인지 확인
   - ✅ GetAllSingletons() - 모든 싱글톤 가져오기
   - ✅ 싱글톤 위반 감지 (여러 엔티티에 같은 싱글톤 컴포넌트)

### 4. QuerySpan API (IWorldQuerySpanApi) - **완료**
   - ✅ QueryToSpan<T1>() - 스팬 기반 쿼리
   - ✅ QueryToSpan<T1, T2>() 등 (최대 4개까지 테스트)
   - ✅ Filter와 함께 사용
   - ✅ Process<T>() - ref로 컴포넌트 처리
   - ✅ Span 용량 제한 처리
   - ✅ Dead entity 및 컴포넌트 없는 엔티티 스킵

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
2. ✅ **WorldMessagesTests.cs** - README에 언급된 기능 (완료)
3. ✅ **WorldSingletonTests.cs** - 싱글톤 컴포넌트는 특수한 케이스 (완료)
4. ✅ **WorldQuerySpanTests.cs** - 성능 최적화된 쿼리 (완료)
5. 나머지는 필요에 따라 추가

