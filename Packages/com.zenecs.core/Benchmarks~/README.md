# ZenECS Core Performance Benchmarks

Performance benchmarks for ZenECS Core operations using BenchmarkDotNet.

## Running Benchmarks

### Prerequisites

- .NET SDK 8.0 or later
- Release configuration for accurate results

### Run All Benchmarks

**Full Mode (Recommended for accurate results):**
```bash
cd Packages/com.zenecs.core/Benchmarks~
dotnet run -c Release
```
⏱️ **예상 실행 시간: 5-10분** (22개 벤치마크, 각각 15-20회 반복)

**Quick Mode (빠른 테스트용):**
```bash
dotnet run -c Release -- --quick
# 또는
dotnet run -c Release -- -q
```
⏱️ **예상 실행 시간: 1-2분** (반복 횟수 감소, 정확도는 낮을 수 있음)

### Run Specific Benchmark

특정 벤치마크 클래스만 실행 (시간 단축):
```bash
# Entity/Component 벤치마크만 실행 (~2-3분)
dotnet run -c Release -- --filter EntityComponentBenchmarks

# Query 벤치마크만 실행 (~2-3분)
dotnet run -c Release -- --filter QueryBenchmarks

# System 벤치마크만 실행 (~1-2분)
dotnet run -c Release -- --filter SystemBenchmarks

# Message Bus 벤치마크만 실행 (~1분)
dotnet run -c Release -- --filter MessageBusBenchmarks
```

### 실행 시간 안내

BenchmarkDotNet은 정확한 결과를 위해 각 벤치마크를 여러 번 반복 실행합니다:

- **Full Mode**: 각 벤치마크당 약 15-20회 반복 (정확한 결과)
- **Quick Mode**: 각 벤치마크당 약 3회 반복 (빠른 테스트용)

**총 벤치마크 수**: 22개 메서드
- EntityComponentBenchmarks: 9개
- QueryBenchmarks: 7개
- SystemBenchmarks: 4개
- MessageBusBenchmarks: 2개

**실행 중단**: `Ctrl+C`로 언제든지 중단 가능합니다.

## Benchmark Categories

### EntityComponentBenchmarks

Benchmarks for entity and component operations:

- **CreateEntities**: Entity creation performance
- **DestroyEntities**: Entity destruction performance
- **AddComponent**: Component addition performance
- **GetComponent**: Component retrieval performance
- **ReplaceComponent**: Component replacement performance
- **RemoveComponent**: Component removal performance
- **QuerySingleComponent**: Single component query iteration
- **QueryMultipleComponents**: Multi-component query iteration

### QueryBenchmarks

Benchmarks for query operations:

- **QuerySingleComponent_Iterate**: Iterate over single component query
- **QuerySingleComponent_WithModification**: Query with component modification
- **QueryTwoComponents_Iterate**: Iterate over two-component query
- **QueryTwoComponents_WithModification**: Two-component query with modification
- **QueryThreeComponents_Iterate**: Iterate over three-component query
- **HasComponent_Check**: Component existence checking
- **TryGetComponent_Check**: Safe component retrieval

### SystemBenchmarks

Benchmarks for system execution:

- **FixedStep_WithSystems**: Fixed-step simulation with systems
- **BeginFrame_WithSystems**: Begin frame with systems
- **LateFrame_WithSystems**: Late frame with systems
- **PumpAndLateFrame_WithSystems**: Complete frame cycle with systems

### MessageBusBenchmarks

Benchmarks for message bus operations:

- **PublishMessage**: Single message type publishing
- **PublishMultipleMessageTypes**: Multiple message types publishing

## Interpreting Results

BenchmarkDotNet provides:

- **Mean**: Average execution time
- **Error**: Standard error
- **StdDev**: Standard deviation
- **Gen 0/1/2**: Garbage collection generations
- **Allocated**: Memory allocated per operation

## Performance Guidelines

Based on benchmark results:

1. **Entity Creation**: Batch entity creation in command buffers
2. **Component Access**: Use `TryGetComponent` for optional components
3. **Queries**: Prefer multi-component queries over multiple single queries
4. **Systems**: Keep system logic focused and efficient
5. **Messages**: Batch message publishing when possible

## See Also

- [Performance Guide](../Docs/core/performance.md) - Performance optimization guide
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/) - BenchmarkDotNet documentation

