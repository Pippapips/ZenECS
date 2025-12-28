// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 06 Multi-World
// File: MultiWorld.cs
// Purpose: Demonstrates creating and managing multiple worlds with Kernel.
// Key concepts:
//   • Multiple worlds creation and management
//   • World switching and independent execution
//   • World lookup by name and tags
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

namespace ZenEcsCoreSamples.MultiWorld
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

    public readonly struct WorldTag
    {
        public readonly string Tag;
        public WorldTag(string tag) => Tag = tag;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves entities in the current world (Simulation phase).
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
    /// Read-only presentation: prints positions from all worlds.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintAllWorldsSystem : ISystem
    {
        private readonly Kernel _kernel;

        public PrintAllWorldsSystem(Kernel kernel)
        {
            _kernel = kernel;
        }

        public void Run(IWorld w, float dt)
        {
            // Print current world info
            var current = _kernel.CurrentWorld;
            if (current != null)
            {
                Console.WriteLine($"\n=== Current World: {current.Name} (ID: {current.Id.Value}) ===");
                foreach (var (e, pos) in current.Query<Position>())
                {
                    Console.WriteLine($"  Entity {e.Id,3}: pos={pos}");
                }
            }

            // Print summary of all worlds
            int worldCount = 0;
            foreach (var _ in _kernel.GetAllWorld())
            {
                worldCount++;
            }
            Console.WriteLine($"\n[All Worlds: {worldCount}]");
            foreach (var world in _kernel.GetAllWorld())
            {
                int entityCount = 0;
                foreach (var _ in world.Query<Position>())
                {
                    entityCount++;
                }
                Console.WriteLine($"  - {world.Name} (ID: {world.Id.Value}): {entityCount} entities");
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
            Console.WriteLine("=== ZenECS Core Sample - Multi-World (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            
            // Create multiple worlds with different names and tags
            var world1 = kernel.CreateWorld(null, name: "GameWorld", tags: new[] { "game", "main" });
            var world2 = kernel.CreateWorld(null, name: "UISimulation", tags: new[] { "ui", "overlay" });
            var world3 = kernel.CreateWorld(null, name: "Background", tags: new[] { "background" });

            // Set first world as current
            kernel.SetCurrentWorld(world1);

            // Add systems to each world
            world1.AddSystems([new MoveSystem()]);
            world2.AddSystems([new MoveSystem()]);
            world3.AddSystems([new MoveSystem()]);

            // Add presentation system to current world (it will access kernel)
            world1.AddSystems([new PrintAllWorldsSystem(kernel)]);

            // Seed entities in each world using CommandBuffer
            using (var cmd1 = world1.BeginWrite())
            {
                var e1 = cmd1.CreateEntity();
                cmd1.AddComponent(e1, new Position(0, 0));
                cmd1.AddComponent(e1, new Velocity(1, 0)); // moves +X / sec

                var e2 = cmd1.CreateEntity();
                cmd1.AddComponent(e2, new Position(2, 1));
                cmd1.AddComponent(e2, new Velocity(0, -0.5f)); // moves -Y / sec
            }

            using (var cmd2 = world2.BeginWrite())
            {
                var e3 = cmd2.CreateEntity();
                cmd2.AddComponent(e3, new Position(5, 5));
                cmd2.AddComponent(e3, new Velocity(-0.5f, 0.5f));
            }

            using (var cmd3 = world3.BeginWrite())
            {
                var e4 = cmd3.CreateEntity();
                cmd3.AddComponent(e4, new Position(10, 10));
                cmd3.AddComponent(e4, new Velocity(0.3f, -0.3f));
            }

            Console.WriteLine("Created 3 worlds:");
            Console.WriteLine($"  - {world1.Name} (ID: {world1.Id.Value})");
            Console.WriteLine($"  - {world2.Name} (ID: {world2.Id.Value})");
            Console.WriteLine($"  - {world3.Name} (ID: {world3.Id.Value})");
            Console.WriteLine("\nPress [1]/[2]/[3] to switch worlds, [ESC] to exit.");

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            bool running = true;
            while (running)
            {
                // Handle input for world switching
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            kernel.SetCurrentWorld(world1);
                            Console.WriteLine($"\n[Switched to: {world1.Name}]");
                            break;
                        case ConsoleKey.D2:
                            kernel.SetCurrentWorld(world2);
                            Console.WriteLine($"\n[Switched to: {world2.Name}]");
                            break;
                        case ConsoleKey.D3:
                            kernel.SetCurrentWorld(world3);
                            Console.WriteLine($"\n[Switched to: {world3.Name}]");
                            break;
                        case ConsoleKey.Escape:
                            running = false;
                            break;
                    }
                }

                // Timing
                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                // Pump all worlds (or only current if configured)
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
