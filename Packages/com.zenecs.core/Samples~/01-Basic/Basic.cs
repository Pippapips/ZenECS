// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 01 Basic
// File: Basic.cs
// Purpose: Minimal ECS sample demonstrating Kernel usage with a simulation and
//          presentation system.
// Key concepts:
//   • Demonstrates Position/Velocity component pattern
//   • MoveSystem integrates Position += Velocity * dt (SimulationGroup)
//   • PrintPositionsSystem reads and prints Position each frame (PresentationGroup)
//   • Shows typical Pump + LateFrame loop structure
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System.Diagnostics;
using ZenECS.Core;
using ZenECS.Core.Config;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Basic
{
    /// <summary>
    /// Position component used for storing entity coordinates.
    /// </summary>
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

    /// <summary>
    /// Velocity component representing delta per second.
    /// </summary>
    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Integrates: Position += Velocity * dt (Simulation phase)
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
    /// Read-only presentation: prints positions each frame (Late phase)
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintPositionsSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, pos) in w.Query<Position>())
            {
                Console.WriteLine($"Entity {e.Id,3}: pos={pos}");
            }
        }
    }

    /// <summary>
    /// Entry point demonstrating ZenECS kernel-driven loop.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - Basic (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new MoveSystem(),
                new PrintPositionsSystem()
            ]);

            using (var cmd = world.BeginWrite())
            {
                // Create sample entities with Position and Velocity
                var e1 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Position(0, 0));
                cmd.AddComponent(e1, new Velocity(1, 0)); // moves +X / sec

                var e2 = cmd.CreateEntity();
                cmd.AddComponent(e2, new Position(2, 1));
                cmd.AddComponent(e2, new Velocity(0, -0.5f)); // moves -Y / sec
            }

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            EcsRuntimeOptions.Log.Info("Hello World!");
            
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

                // Perform variable-step Begin + multiple Fixed steps + alpha calculation
                const int maxSubStepsPerFrame = 4;
                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                Thread.Sleep(1); // Reduce CPU load
            }

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
