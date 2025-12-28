# ZenECS Core Performance Benchmarks

Performance benchmarks for ZenECS Core operations using BenchmarkDotNet.

## Running Benchmarks

### Prerequisites

- .NET SDK 8.0 or later
- Release configuration for accurate results

### Run All Benchmarks

```bash
cd Packages/com.zenecs.core/Benchmarks~
dotnet run -c Release
```

### Run Specific Benchmark

```bash
dotnet run -c Release -- --filter EntityComponentBenchmarks
```

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

