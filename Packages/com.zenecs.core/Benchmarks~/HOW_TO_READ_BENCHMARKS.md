# How to Read Benchmark Results

A guide to interpreting BenchmarkDotNet benchmark results.

## Running Benchmarks

```bash
cd Packages/com.zenecs.core/Benchmarks~
dotnet run -c Release
```

## Output Format

After benchmarks complete, results are displayed in a table format:

```
| Method                    | count | Mean      | Error    | StdDev   | Gen0   | Gen1 | Allocated |
|-------------------------- |------ |---------- |----------|----------|--------|------|-----------|
| CreateEntities            | 100   | 12.45 us  | 0.234 us | 0.198 us | 0.1234 | -    | 1.23 KB   |
| CreateEntities            | 1000  | 125.67 us | 2.456 us | 2.189 us | 1.2345 | -    | 12.34 KB  |
| GetComponent              | 100   | 0.45 us   | 0.012 us | 0.010 us | -      | -    | -         |
```

## Column Descriptions

### Time Metrics

#### Mean (Average)
- **Meaning**: Average execution time across multiple runs
- **Interpretation**: Lower is better
- **Example**: `12.45 us` = average 12.45 microseconds

#### Error (Standard Error)
- **Meaning**: Standard error of the mean
- **Interpretation**: Smaller values indicate more stable measurements
- **Warning**: If Error > 10% of Mean, measurements may be unstable

#### StdDev (Standard Deviation)
- **Meaning**: Variability in execution times
- **Interpretation**: Smaller values indicate more consistent performance
- **Comparison**: Compare with Mean to assess variability

### Memory Metrics

#### Gen0, Gen1, Gen2
- **Meaning**: Number of garbage collections per generation
- **Interpretation**: 
  - `-` = No GC occurred (good)
  - Lower numbers are better
  - Gen2 collections indicate significant performance impact

#### Allocated (Allocated Memory)
- **Meaning**: Memory allocated per operation
- **Interpretation**: 
  - `-` = No allocation (optimal)
  - Lower is better
  - Should be near zero when using structs

## Time Units

BenchmarkDotNet automatically selects appropriate units:

- **ns** (nanoseconds): 1/1,000,000,000 second
- **us** (microseconds): 1/1,000,000 second
- **ms** (milliseconds): 1/1,000 second
- **s** (seconds): 1 second

## Result Interpretation Examples

### Example 1: Entity Creation

```
| Method           | count | Mean      | Error    | Allocated |
|----------------- |------ |---------- |----------|-----------|
| CreateEntities   | 100   | 12.45 us  | 0.234 us | 1.23 KB   |
| CreateEntities   | 1000  | 125.67 us | 2.456 us | 12.34 KB  |
```

**Interpretation**:
- 100 entities: Average 12.45 microseconds, 1.23 KB allocated
- 1000 entities: Average 125.67 microseconds, 12.34 KB allocated
- **Linear scaling**: ~10x increase for 10x entities (good)
- **Memory efficient**: ~12.3 bytes per entity (very efficient)

### Example 2: Component Retrieval

```
| Method      | count | Mean    | Error   | Allocated |
|------------ |------ |---------|---------|-----------|
| GetComponent| 100   | 0.45 us | 0.012 us| -         |
```

**Interpretation**:
- Very fast: 0.45 microseconds
- No memory allocation: `-` indicates optimal
- Stable: Error is 2.7% of Mean (very stable)

### Example 3: Query Performance

```
| Method                    | Mean      | Error    | Allocated |
|-------------------------- |---------- |----------|-----------|
| QuerySingleComponent      | 45.67 us  | 1.234 us | -         |
| QueryMultipleComponents   | 78.90 us  | 2.345 us | -         |
```

**Interpretation**:
- Single component query: 45.67 microseconds
- Multiple component query: 78.90 microseconds
- **Comparison**: Multi-query is ~1.7x slower (reasonable)
- **Memory**: Both have no allocation (optimal)

## Performance Comparison

### 1. Comparing Different Implementations

```
| Method              | Mean      | Ratio |
|-------------------- |---------- |-------|
| OldImplementation   | 100.0 us  | 1.00  |
| NewImplementation   | 50.0 us   | 0.50  |
```

**Interpretation**: New implementation is 2x faster (Ratio 0.50)

### 2. Checking Scalability

```
| Method           | count | Mean      | Ratio |
|----------------- |------ |---------- |-------|
| CreateEntities   | 100   | 10.0 us   | 1.00  |
| CreateEntities   | 1000  | 100.0 us  | 10.00 |
| CreateEntities   | 10000 | 1000.0 us| 100.00|
```

**Interpretation**: Linear scaling (O(n)) - ideal

## Important Notes

### 1. First Run is Ignored
- First run includes JIT compilation time and may be slower
- BenchmarkDotNet automatically performs warmup

### 2. Environment Impact
- CPU load and background processes can affect results
- Must run in Release mode for accurate results

### 3. Memory Allocation Interpretation
- `-` means no allocation (optimal)
- Small values can accumulate and cause GC pressure

### 4. Check Error Ratio
- If Error > 10% of Mean, measurements may be unstable
- Run multiple times to check consistency

## Performance Optimization Guide

### Characteristics of Good Results

1. **Fast execution**: Low Mean value
2. **Stable**: Error < 5% of Mean
3. **Memory efficient**: Allocated is `-` or very small
4. **No GC pressure**: Gen0/Gen1/Gen2 are `-` or 0

### When Improvement is Needed

1. **High memory allocation**: Large Allocated value
   - Consider using structs
   - Apply pooling patterns

2. **Unstable measurements**: Error > 10% of Mean
   - Run more iterations
   - Clean up environment

3. **High GC occurrence**: Large Gen0/Gen1/Gen2 values
   - Reduce memory allocation
   - Reuse objects

## Real-World Example

### Scenario: Entity Creation Optimization

**Before (Before Optimization)**:
```
| Method           | count | Mean      | Allocated |
|----------------- |------ |---------- |-----------|
| CreateEntities   | 1000  | 200.0 us  | 50.0 KB   |
```

**After (After Optimization)**:
```
| Method           | count | Mean      | Allocated |
|----------------- |------ |---------- |-----------|
| CreateEntities   | 1000  | 100.0 us  | 10.0 KB   |
```

**Improvements**:
- Execution time: 50% improvement (200 us → 100 us)
- Memory: 80% reduction (50 KB → 10 KB)

## Filtering Benchmarks

Run specific benchmarks:

```bash
# Run only entity/component benchmarks
dotnet run -c Release -- --filter EntityComponentBenchmarks

# Run only query benchmarks
dotnet run -c Release -- --filter QueryBenchmarks

# Run only system benchmarks
dotnet run -c Release -- --filter SystemBenchmarks

# Run only message bus benchmarks
dotnet run -c Release -- --filter MessageBusBenchmarks
```

## Exporting Results

BenchmarkDotNet automatically generates:

- **Markdown report**: `BenchmarkDotNet.Artifacts/results/*.md`
- **CSV export**: `BenchmarkDotNet.Artifacts/results/*.csv`
- **HTML report**: `BenchmarkDotNet.Artifacts/results/*.html`

View reports in:
```
BenchmarkDotNet.Artifacts/results/
```

## Common Patterns

### Pattern 1: Linear Scaling (Good)
```
count: 100   → Mean: 10 us
count: 1000  → Mean: 100 us
count: 10000 → Mean: 1000 us
```
O(n) complexity - expected behavior

### Pattern 2: Sub-linear Scaling (Excellent)
```
count: 100   → Mean: 10 us
count: 1000  → Mean: 50 us
count: 10000 → Mean: 200 us
```
Better than O(n) - may indicate caching or optimization

### Pattern 3: Super-linear Scaling (Problem)
```
count: 100   → Mean: 10 us
count: 1000  → Mean: 150 us
count: 10000 → Mean: 2000 us
```
Worse than O(n) - may indicate memory pressure or algorithm issues

## Troubleshooting

### High Error Values
- **Cause**: Unstable environment, insufficient iterations
- **Solution**: Run more iterations, close background applications

### Unexpected Allocations
- **Cause**: Boxing, LINQ, closures
- **Solution**: Use structs, avoid LINQ in hot paths, minimize closures

### High GC Counts
- **Cause**: Frequent allocations
- **Solution**: Reduce allocations, use object pooling

## Additional Resources

- [BenchmarkDotNet Official Documentation](https://benchmarkdotnet.org/)
- [Performance Guide](../Docs/core/performance.md)
- [Memory Optimization](../Docs/core/memory-growth-policy.md)
- [BenchmarkDotNet GitHub](https://github.com/dotnet/BenchmarkDotNet)
