// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 07 Query Filtering
// File: QueryFiltering.cs
// Purpose: Demonstrates advanced query filtering with With/Without/WithAny/WithoutAny.
// Key concepts:
//   • Filter.New.With<T>().Without<T>() pattern
//   • WithAny/WithoutAny for OR-group filtering
//   • Complex filter combinations
//   • CommandBuffer usage for entity creation
//   • Kernel.PumpAndLateFrame for frame loop
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Config;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.QueryFiltering
{
    // ──────────────────────────────────────────────────────────────────────────
    // Components
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct Position
    {
        public readonly float X, Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct Health
    {
        public readonly int Value;
        public Health(int value) => Value = value;
        public override string ToString() => $"HP={Value}";
    }

    public readonly struct Paused
    {
        // Marker component - no data needed
    }

    public readonly struct Enemy
    {
        // Marker component
    }

    public readonly struct Player
    {
        // Marker component
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves only non-paused entities (Simulation phase).
    /// </summary>
    [FixedGroup]
    public sealed class MoveSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // Filter: entities with Position AND Velocity, but WITHOUT Paused
            var filter = Filter.New
                .With<Position>()
                .With<Velocity>()
                .Without<Paused>()
                .Build();

            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>(filter))
            {
                cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
            }
        }
    }

    /// <summary>
    /// Damages only enemies (not players) using WithAny filter.
    /// </summary>
    [FixedGroup]
    public sealed class DamageEnemySystem : ISystem
    {
        private int _frameCount;

        public void Run(IWorld w, float dt)
        {
            _frameCount++;
            if (_frameCount % 60 != 0) return; // Damage every second

            // Filter: entities with Health AND (Enemy OR without Player)
            // This demonstrates WithAny - entity must have at least one of the types
            var filter = Filter.New
                .With<Health>()
                .WithAny(typeof(Enemy))
                .Without<Player>()
                .Build();

            using var cmd = w.BeginWrite();
            foreach (var (e, health) in w.Query<Health>(filter))
            {
                var newHealth = new Health(Math.Max(0, health.Value - 1));
                cmd.ReplaceComponent(e, newHealth);
                Console.WriteLine($"[Damage] Entity {e.Id}: {health.Value} → {newHealth.Value}");
            }
        }
    }

    /// <summary>
    /// Read-only presentation: prints filtered results.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintFilteredSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            Console.WriteLine($"\n=== Frame {w.FrameCount} ===");

            // Filter 1: All moving entities (Position + Velocity, not Paused)
            var movingFilter = Filter.New
                .With<Position>()
                .With<Velocity>()
                .Without<Paused>()
                .Build();

            Console.WriteLine("Moving entities (Position + Velocity, not Paused):");
            foreach (var (e, pos) in w.Query<Position>(movingFilter))
            {
                Console.WriteLine($"  Entity {e.Id,3}: pos={pos}");
            }

            // Filter 2: Entities with Health
            var healthFilter = Filter.New.With<Health>().Build();
            Console.WriteLine("\nEntities with Health:");
            foreach (var (e, health) in w.Query<Health>(healthFilter))
            {
                var isEnemy = w.HasComponent<Enemy>(e) ? " [Enemy]" : "";
                var isPlayer = w.HasComponent<Player>(e) ? " [Player]" : "";
                Console.WriteLine($"  Entity {e.Id,3}: {health}{isEnemy}{isPlayer}");
            }

            // Filter 3: Paused entities
            var pausedFilter = Filter.New.With<Paused>().Build();
            int pausedCount = 0;
            foreach (var _ in w.Query<Paused>(pausedFilter))
            {
                pausedCount++;
            }
            Console.WriteLine($"\nPaused entities: {pausedCount}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - Query Filtering (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new MoveSystem(),
                new DamageEnemySystem(),
                new PrintFilteredSystem()
            ]);

            // Create entities with different component combinations using CommandBuffer
            using (var cmd = world.BeginWrite())
            {
                // Moving enemy (will move and take damage)
                var e1 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Position(0, 0));
                cmd.AddComponent(e1, new Velocity(1, 0));
                cmd.AddComponent(e1, new Health(100));
                cmd.AddComponent(e1, new Enemy());

                // Moving player (will move but not take damage)
                var e2 = cmd.CreateEntity();
                cmd.AddComponent(e2, new Position(5, 5));
                cmd.AddComponent(e2, new Velocity(0, -0.5f));
                cmd.AddComponent(e2, new Health(150));
                cmd.AddComponent(e2, new Player());

                // Paused enemy (won't move, will take damage)
                var e3 = cmd.CreateEntity();
                cmd.AddComponent(e3, new Position(10, 10));
                cmd.AddComponent(e3, new Velocity(0.5f, 0.5f));
                cmd.AddComponent(e3, new Health(80));
                cmd.AddComponent(e3, new Enemy());
                cmd.AddComponent(e3, new Paused());

                // Moving entity without Health (will move, no damage)
                var e4 = cmd.CreateEntity();
                cmd.AddComponent(e4, new Position(15, 15));
                cmd.AddComponent(e4, new Velocity(-0.3f, 0.3f));
            }

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    break;
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                Thread.Sleep(100); // Slow down for readability
            }

            Console.WriteLine("\nShutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Simple logger implementation that routes ECS messages to the console.
        /// </summary>
        class EcsLogger : IEcsLogger
        {
            public void Info(string msg) => Console.WriteLine(msg);
            public void Warn(string msg) => Console.Error.WriteLine(msg);
            public void Error(string msg) => Console.Error.Write(msg);
        }
    }
}
