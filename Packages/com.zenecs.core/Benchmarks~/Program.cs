// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Benchmark Runner
// File: Program.cs
// Purpose: Entry point for running performance benchmarks
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using BenchmarkDotNet.Running;
using ZenECS.Core.Benchmarks;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Entry point for running ZenECS performance benchmarks.
    /// </summary>
    /// <remarks>
    /// Run with: dotnet run -c Release
    /// </remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run all benchmarks
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}

