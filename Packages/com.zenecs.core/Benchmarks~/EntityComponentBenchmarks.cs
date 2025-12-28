// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Performance Benchmarks
// File: EntityComponentBenchmarks.cs
// Purpose: Benchmark entity and component operations
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using ZenECS.Core;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Benchmarks for entity creation, destruction, and component operations.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class EntityComponentBenchmarks
    {
        private IKernel? _kernel;
        private IWorld? _world;
        private Entity[]? _entities;

        [GlobalSetup]
        public void Setup()
        {
            _kernel = new Kernel();
            _world = _kernel.CreateWorld(null, "BenchmarkWorld");
            
            // Pre-create entities for some benchmarks
            _entities = new Entity[10000];
            using (var cmd = _world.BeginWrite())
            {
                for (int i = 0; i < _entities.Length; i++)
                {
                    _entities[i] = cmd.CreateEntity();
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
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void CreateEntities(int count)
        {
            using (var cmd = _world!.BeginWrite())
            {
                for (int i = 0; i < count; i++)
                {
                    cmd.CreateEntity();
                }
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void DestroyEntities(int count)
        {
            using (var cmd = _world!.BeginWrite())
            {
                for (int i = 0; i < count && i < _entities!.Length; i++)
                {
                    cmd.DestroyEntity(_entities[i]);
                }
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void AddComponent(int count)
        {
            using (var cmd = _world!.BeginWrite())
            {
                for (int i = 0; i < count && i < _entities!.Length; i++)
                {
                    cmd.AddComponent(_entities[i], new Position { X = i, Y = i });
                }
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void GetComponent(int count)
        {
            for (int i = 0; i < count && i < _entities!.Length; i++)
            {
                var pos = _world!.ReadComponent<Position>(_entities[i]);
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void ReplaceComponent(int count)
        {
            using (var cmd = _world!.BeginWrite())
            {
                for (int i = 0; i < count && i < _entities!.Length; i++)
                {
                    cmd.ReplaceComponent(_entities[i], new Position { X = i * 2, Y = i * 2 });
                }
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void RemoveComponent(int count)
        {
            using (var cmd = _world!.BeginWrite())
            {
                for (int i = 0; i < count && i < _entities!.Length; i++)
                {
                    cmd.RemoveComponent<Position>(_entities[i]);
                }
            }
        }

        [Benchmark]
        public void QuerySingleComponent()
        {
            int count = 0;
            foreach (var (entity, pos) in _world!.Query<Position>())
            {
                count++;
            }
        }

        [Benchmark]
        public void QueryMultipleComponents()
        {
            int count = 0;
            foreach (var (entity, pos, vel) in _world!.Query<Position, Velocity>())
            {
                count++;
            }
        }

        [Benchmark]
        public void QueryThreeComponents()
        {
            int count = 0;
            foreach (var (entity, pos, vel, health) in _world!.Query<Position, Velocity, Health>())
            {
                count++;
            }
        }
    }

    // Test components
    public readonly struct Position
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    public readonly struct Velocity
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    public readonly struct Health
    {
        public float Current { get; init; }
        public float Max { get; init; }
    }
}

