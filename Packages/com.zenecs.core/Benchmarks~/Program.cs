// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Benchmark Runner
// File: Program.cs
// Purpose: Entry point for running performance benchmarks
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using ZenECS.Core.Benchmarks;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Entry point for running ZenECS performance benchmarks.
    /// </summary>
    /// <remarks>
    /// Usage:
    ///   dotnet run -c Release                    # Run all benchmarks (full accuracy, ~5-10 minutes)
    ///   dotnet run -c Release -- --quick         # Quick mode (reduced iterations, ~1-2 minutes)
    ///   dotnet run -c Release -- --filter Name   # Run specific benchmark class
    /// </remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            
            // Check for quick mode
            if (args.Contains("--quick", StringComparer.OrdinalIgnoreCase) ||
                args.Contains("-q", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("⚡ Quick mode enabled (reduced iterations for faster results)");
                Console.WriteLine("   Note: Results may be less accurate than full mode.\n");
                
                // Quick mode: Reduced iterations for faster execution
                config = config
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    .AddJob(Job.Default
                        .WithIterationCount(3)      // Reduced from default (~15-20)
                        .WithWarmupCount(1)         // Reduced from default (~3-5)
                        .WithInvocationCount(1)      // Single invocation per iteration
                        .WithUnrollFactor(1)        // No unrolling
                        .WithToolchain(InProcessEmitToolchain.Instance));
            }
            
            // Run benchmarks
            var summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config);
            
            // Display summary
            if (summaries.Length > 0 && summaries.Any(s => s.HasCriticalValidationErrors))
            {
                Console.WriteLine("\n❌ Benchmark validation failed!");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("\n✅ Benchmarks completed successfully!");
            }
        }
    }
}

