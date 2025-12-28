// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — System Performance Benchmarks
// File: SystemBenchmarks.cs
// Purpose: Benchmark system execution and frame stepping
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
using ZenECS.Core.Systems;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Benchmarks for system execution and frame stepping.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class SystemBenchmarks
    {
        private IKernel? _kernel;
        private IWorld? _world;
        private const int EntityCount = 10000;

        [GlobalSetup]
        public void Setup()
        {
            _kernel = new Kernel();
            _world = _kernel.CreateWorld(null, "SystemBenchmarkWorld");
            
            // Create entities with components
            using (var cmd = _world.BeginWrite())
            {
                for (int i = 0; i < EntityCount; i++)
                {
                    var entity = cmd.CreateEntity();
                    cmd.AddComponent(entity, new Position { X = i, Y = i });
                    cmd.AddComponent(entity, new Velocity { X = 1, Y = 1 });
                }
            }
            
            // Register systems
            _world.AddSystems([
                new MovementSystem(),
                new UpdateSystem()
            ]);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _world?.Dispose();
            _kernel?.Dispose();
        }

        [Benchmark]
        public void FixedStep_WithSystems()
        {
            _kernel!.FixedStep(1f / 60f);
        }

        [Benchmark]
        public void BeginFrame_WithSystems()
        {
            _kernel!.BeginFrame(1f / 60f);
        }

        [Benchmark]
        public void LateFrame_WithSystems()
        {
            _kernel!.LateFrame(1f);
        }

        [Benchmark]
        public void PumpAndLateFrame_WithSystems()
        {
            _kernel!.PumpAndLateFrame(1f / 60f, 1f / 60f, maxSubSteps: 1);
        }
    }

    // Test systems
    [FixedGroup]
    public sealed class MovementSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (entity, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(entity, new Position
                {
                    X = pos.X + vel.X * dt,
                    Y = pos.Y + vel.Y * dt
                });
            }
        }
    }

    [FrameSyncGroup]
    public sealed class UpdateSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (entity, pos) in w.Query<Position>())
            {
                // Read-only operation
                var _ = pos.X + pos.Y;
            }
        }
    }
}

