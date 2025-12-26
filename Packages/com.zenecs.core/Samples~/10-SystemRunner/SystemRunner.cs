// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 10 System Runner
// File: SystemRunner.cs
// Purpose: Demonstrates manual frame loop control using Kernel.PumpAndLateFrame.
// Key concepts:
//   • Manual frame loop with Kernel.PumpAndLateFrame
//   • Fixed timestep accumulation and sub-stepping
//   • Alpha interpolation for presentation
//   • CommandBuffer usage for entity creation
//   • Console app frame loop pattern
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

namespace ZenEcsCoreSamples.SystemRunner
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

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves entities (Simulation phase - Fixed timestep).
    /// </summary>
    [FixedGroup]
    public sealed class MoveSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
            }
        }
    }

    /// <summary>
    /// Read-only presentation: prints positions with alpha interpolation info.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintPositionsSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            if (w.FrameCount % 60 == 0) // Print every second
            {
                Console.WriteLine($"\n=== Frame {w.FrameCount} (dt={dt:0.000}) ===");
                foreach (var (e, pos) in w.Query<Position>())
                {
                    Console.WriteLine($"  Entity {e.Id,3}: pos={pos}");
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
            Console.WriteLine("=== ZenECS Core Sample - System Runner (Manual Loop) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new MoveSystem(),
                new PrintPositionsSystem()
            ]);

            // Seed entities using CommandBuffer
            using (var cmd = world.BeginWrite())
            {
                var e1 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Position(0, 0));
                cmd.AddComponent(e1, new Velocity(1, 0)); // moves +X / sec

                var e2 = cmd.CreateEntity();
                cmd.AddComponent(e2, new Position(2, 1));
                cmd.AddComponent(e2, new Velocity(0, -0.5f)); // moves -Y / sec
            }

            // Manual frame loop configuration
            const float fixedDelta = 1f / 60f; // 60Hz fixed timestep
            const int maxSubStepsPerFrame = 4; // Maximum fixed steps per frame

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running manual frame loop...");
            Console.WriteLine($"Fixed timestep: {fixedDelta:0.000}s ({1f / fixedDelta}Hz)");
            Console.WriteLine($"Max sub-steps per frame: {maxSubStepsPerFrame}");
            Console.WriteLine("Press any key to exit.");

            int frameCount = 0;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    break;
                }

                // Calculate frame delta time
                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                // Manual frame loop using Kernel.PumpAndLateFrame
                // This internally:
                // 1. Calls BeginFrame(dt) - variable timestep systems
                // 2. Accumulates dt into fixed timestep and calls FixedStep() multiple times
                // 3. Calculates alpha for interpolation
                // 4. Calls LateFrame(alpha) - presentation systems (read-only)
                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                frameCount++;

                // Optional: Frame rate limiting
                Thread.Sleep(1); // Reduce CPU load
            }

            Console.WriteLine($"\nTotal frames: {frameCount}");
            Console.WriteLine("Shutting down...");
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
