// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Query Performance Benchmarks
// File: QueryBenchmarks.cs
// Purpose: Benchmark query operations and iteration patterns
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using ZenECS.Core;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Benchmarks for query operations and iteration patterns.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class QueryBenchmarks
    {
        private IKernel? _kernel;
        private IWorld? _world;
        private const int EntityCount = 10000;

        [GlobalSetup]
        public void Setup()
        {
            _kernel = new Kernel();
            _world = _kernel.CreateWorld(null, "QueryBenchmarkWorld");
            
            // Create entities with various component combinations
            using (var cmd = _world.BeginWrite())
            {
                for (int i = 0; i < EntityCount; i++)
                {
                    var entity = cmd.CreateEntity();
                    cmd.AddComponent(entity, new Position { X = i, Y = i });
                    
                    if (i % 2 == 0)
                    {
                        cmd.AddComponent(entity, new Velocity { X = 1, Y = 1 });
                    }
                    
                    if (i % 3 == 0)
                    {
                        cmd.AddComponent(entity, new Health { Current = 100, Max = 100 });
                    }
                }
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _world?.Dispose();
            _kernel?.Dispose();
        }

        [Benchmark]
        public int QuerySingleComponent_Iterate()
        {
            int count = 0;
            foreach (var (entity, pos) in _world!.Query<Position>())
            {
                count++;
            }
            return count;
        }

        [Benchmark]
        public int QuerySingleComponent_WithModification()
        {
            int count = 0;
            using (var cmd = _world!.BeginWrite())
            {
                foreach (var (entity, pos) in _world.Query<Position>())
                {
                    cmd.ReplaceComponent(entity, new Position { X = pos.X + 1, Y = pos.Y + 1 });
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public int QueryTwoComponents_Iterate()
        {
            int count = 0;
            foreach (var (entity, pos, vel) in _world!.Query<Position, Velocity>())
            {
                count++;
            }
            return count;
        }

        [Benchmark]
        public int QueryTwoComponents_WithModification()
        {
            int count = 0;
            using (var cmd = _world!.BeginWrite())
            {
                foreach (var (entity, pos, vel) in _world.Query<Position, Velocity>())
                {
                    cmd.ReplaceComponent(entity, new Position 
                    { 
                        X = pos.X + vel.X, 
                        Y = pos.Y + vel.Y 
                    });
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public int QueryThreeComponents_Iterate()
        {
            int count = 0;
            foreach (var (entity, pos, vel, health) in _world!.Query<Position, Velocity, Health>())
            {
                count++;
            }
            return count;
        }

        [Benchmark]
        public int HasComponent_Check()
        {
            int count = 0;
            foreach (var (entity, pos) in _world!.Query<Position>())
            {
                if (_world.HasComponent<Velocity>(entity))
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public int TryGetComponent_Check()
        {
            int count = 0;
            foreach (var (entity, pos) in _world!.Query<Position>())
            {
                if (_world.TryReadComponent<Velocity>(entity, out _))
                {
                    count++;
                }
            }
            return count;
        }
    }
}

