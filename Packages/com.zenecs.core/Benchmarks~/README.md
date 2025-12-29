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
⏱️ **Estimated execution time: 5-10 minutes** (22 benchmarks, 15-20 iterations each)

**Quick Mode (for quick testing):**
```bash
dotnet run -c Release -- --quick
# or
dotnet run -c Release -- -q
```
⏱️ **Estimated execution time: 1-2 minutes** (reduced iterations, may be less accurate)

### Run Specific Benchmark

Run specific benchmark class only (time-saving):
```bash
# Run Entity/Component benchmarks only (~2-3 minutes)
dotnet run -c Release -- --filter EntityComponentBenchmarks

# Run Query benchmarks only (~2-3 minutes)
dotnet run -c Release -- --filter QueryBenchmarks

# Run System benchmarks only (~1-2 minutes)
dotnet run -c Release -- --filter SystemBenchmarks

# Run Message Bus benchmarks only (~1 minute)
dotnet run -c Release -- --filter MessageBusBenchmarks
```

### Execution Time Guide

BenchmarkDotNet runs each benchmark multiple times for accurate results:

- **Full Mode**: Approximately 15-20 iterations per benchmark (accurate results)
- **Quick Mode**: Approximately 3 iterations per benchmark (for quick testing)

**Total benchmarks**: 22 methods
- EntityComponentBenchmarks: 9 methods
- QueryBenchmarks: 7 methods
- SystemBenchmarks: 4 methods
- MessageBusBenchmarks: 2 methods

**Interrupt execution**: You can interrupt at any time with `Ctrl+C`.

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

