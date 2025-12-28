// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 11 System Ordering
// File: SystemOrdering.cs
// Purpose: Demonstrates system execution ordering with OrderBefore/OrderAfter.
// Key concepts:
//   • OrderBefore attribute - system must run before target
//   • OrderAfter attribute - system must run after target
//   • System group ordering within same group
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

namespace ZenEcsCoreSamples.SystemOrdering
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

    public readonly struct Acceleration
    {
        public readonly float X, Y;
        public Acceleration(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems (ordered execution)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// First system: Updates velocity from acceleration.
    /// Must run before MoveSystem.
    /// </summary>
    [FixedGroup]
    [OrderBefore(typeof(MoveSystem))]
    public sealed class UpdateVelocitySystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, vel, acc) in w.Query<Velocity, Acceleration>())
            {
                cmd.ReplaceComponent(e, new Velocity(vel.X + acc.X * dt, vel.Y + acc.Y * dt));
            }
            Console.WriteLine($"[System] UpdateVelocitySystem ran (dt={dt:0.000})");
        }
    }

    /// <summary>
    /// Second system: Moves entities using velocity.
    /// Must run after UpdateVelocitySystem.
    /// </summary>
    [FixedGroup]
    [OrderAfter(typeof(UpdateVelocitySystem))]
    public sealed class MoveSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
            }
            Console.WriteLine($"[System] MoveSystem ran (dt={dt:0.000})");
        }
    }

    /// <summary>
    /// Third system: Applies damping to velocity.
    /// Must run after MoveSystem.
    /// </summary>
    [FixedGroup]
    [OrderAfter(typeof(MoveSystem))]
    public sealed class DampingSystem : ISystem
    {
        private const float DampingFactor = 0.98f;

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, vel) in w.Query<Velocity>())
            {
                cmd.ReplaceComponent(e, new Velocity(vel.X * DampingFactor, vel.Y * DampingFactor));
            }
            Console.WriteLine($"[System] DampingSystem ran (dt={dt:0.000})");
        }
    }

    /// <summary>
    /// Read-only presentation: prints positions and execution order.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintOrderSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            if (w.FrameCount % 60 == 0) // Print every second
            {
                Console.WriteLine($"\n=== Frame {w.FrameCount} ===");
                Console.WriteLine("Execution order (within FixedGroup):");
                Console.WriteLine("  1. UpdateVelocitySystem (updates velocity from acceleration)");
                Console.WriteLine("  2. MoveSystem (moves position using velocity)");
                Console.WriteLine("  3. DampingSystem (applies damping to velocity)");
                Console.WriteLine("\nEntity states:");
                foreach (var (e, pos) in w.Query<Position>())
                {
                    var vel = w.HasComponent<Velocity>(e) 
                        ? w.ReadComponent<Velocity>(e) 
                        : default;
                    var acc = w.HasComponent<Acceleration>(e) 
                        ? w.ReadComponent<Acceleration>(e) 
                        : default;
                    Console.WriteLine($"  Entity {e.Id,3}: pos={pos}, vel=({vel.X:0.###}, {vel.Y:0.###}), acc=({acc.X:0.###}, {acc.Y:0.###})");
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - System Ordering (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            // Add systems - order doesn't matter, attributes control execution
            world.AddSystems([
                new DampingSystem(),      // Added third, but runs last
                new MoveSystem(),         // Added second, but runs middle
                new UpdateVelocitySystem(), // Added first, but runs first
                new PrintOrderSystem()
            ]);

            // Seed entities using CommandBuffer
            using (var cmd = world.BeginWrite())
            {
                var e1 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Position(0, 0));
                cmd.AddComponent(e1, new Velocity(1, 0));
                cmd.AddComponent(e1, new Acceleration(0.1f, 0)); // Accelerates +X

                var e2 = cmd.CreateEntity();
                cmd.AddComponent(e2, new Position(5, 5));
                cmd.AddComponent(e2, new Velocity(0, -0.5f));
                cmd.AddComponent(e2, new Acceleration(0, 0.05f)); // Accelerates +Y
            }

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("System execution order (within FixedGroup):");
            Console.WriteLine("  1. UpdateVelocitySystem");
            Console.WriteLine("  2. MoveSystem");
            Console.WriteLine("  3. DampingSystem");
            Console.WriteLine("\nRunning... press any key to exit.");

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

                Thread.Sleep(10); // Reduce CPU load
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
