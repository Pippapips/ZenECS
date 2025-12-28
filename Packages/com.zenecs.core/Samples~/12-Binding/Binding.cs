// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 12 Binding
// File: Binding.cs
// Purpose: Demonstrates view binding using Contexts and Binders.
// Key concepts:
//   • IContext - view-related data container
//   • IBinder - component change detection and view updates
//   • BaseBinder - convenience base class for binders
//   • ComponentDelta - component change information
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
using ZenECS.Core.Binding;
using ZenECS.Core.Config;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Binding
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

    public readonly struct Health
    {
        public readonly int Value;
        public Health(int value) => Value = value;
        public override string ToString() => $"HP={Value}";
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
    // Contexts (View Data)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Context representing a console view for an entity.
    /// </summary>
    public class ConsoleViewContext : IContext
    {
        public string DisplayName { get; set; } = "";
        public ConsoleColor Color { get; set; } = ConsoleColor.White;
        public long LastUpdateFrame { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Binders (View Updates)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Binder that updates console view when Position changes.
    /// </summary>
    public sealed class PositionBinder : BaseBinder, IBind<Position>
    {
        public void OnDelta(in ComponentDelta<Position> delta)
        {
            if (Contexts == null || World == null) return;

            var context = Contexts.Get<ConsoleViewContext>(World, Entity);
            if (context != null)
            {
                context.LastUpdateFrame = World.FrameCount;
                var kind = delta.Kind == ComponentDeltaKind.Added ? "added" :
                          delta.Kind == ComponentDeltaKind.Changed ? "changed" :
                          delta.Kind == ComponentDeltaKind.Removed ? "removed" : "snapshot";
                Console.WriteLine($"[Binder] {context.DisplayName}: Position {kind} to {delta.Value}");
            }
        }

        protected override void OnApply(IWorld w, Entity e)
        {
            // Called at end of presentation phase
            // Can perform final view updates here if needed
        }
    }

    /// <summary>
    /// Binder that updates console view when Health changes.
    /// </summary>
    public sealed class HealthBinder : BaseBinder, IBind<Health>
    {
        public void OnDelta(in ComponentDelta<Health> delta)
        {
            if (Contexts == null || World == null) return;

            var context = Contexts.Get<ConsoleViewContext>(World, Entity);
            if (context != null)
            {
                context.LastUpdateFrame = World.FrameCount;
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = context.Color;
                var kind = delta.Kind == ComponentDeltaKind.Added ? "added" :
                          delta.Kind == ComponentDeltaKind.Changed ? "changed" :
                          delta.Kind == ComponentDeltaKind.Removed ? "removed" : "snapshot";
                Console.WriteLine($"[Binder] {context.DisplayName}: Health {kind} to {delta.Value.Value}");
                Console.ForegroundColor = oldColor;
            }
        }

        protected override void OnApply(IWorld w, Entity e)
        {
            // Called at end of presentation phase
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves entities (Simulation phase).
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
    /// Damages entities periodically (Simulation phase).
    /// </summary>
    [FixedGroup]
    public sealed class DamageSystem : ISystem
    {
        private int _frameCount;

        public void Run(IWorld w, float dt)
        {
            _frameCount++;
            if (_frameCount % 120 != 0) return; // Damage every 2 seconds

            using var cmd = w.BeginWrite();
            foreach (var (e, health) in w.Query<Health>())
            {
                var newHealth = new Health(Math.Max(0, health.Value - 10));
                cmd.ReplaceComponent(e, newHealth);
            }
        }
    }

    /// <summary>
    /// Read-only presentation: prints summary.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintSummarySystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            if (w.FrameCount % 60 == 0) // Print every second
            {
                Console.WriteLine($"\n=== Frame {w.FrameCount} Summary ===");
                Console.WriteLine($"Total entities: {w.AliveCount}");
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
            Console.WriteLine("=== ZenECS Core Sample - Binding (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new MoveSystem(),
                new DamageSystem(),
                new PrintSummarySystem()
            ]);

            // Create entities with components and binders using CommandBuffer
            using (var cmd = world.BeginWrite())
            {
                // Entity 1: Moving entity with health
                var e1 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Position(0, 0));
                cmd.AddComponent(e1, new Velocity(1, 0));
                cmd.AddComponent(e1, new Health(100));

                // Attach context and binders
                var ctx1 = new ConsoleViewContext { DisplayName = "Entity1", Color = ConsoleColor.Green };
                world.RegisterContext(e1, ctx1);
                world.AttachBinder(e1, new PositionBinder());
                world.AttachBinder(e1, new HealthBinder());

                // Entity 2: Moving entity with health
                var e2 = cmd.CreateEntity();
                cmd.AddComponent(e2, new Position(5, 5));
                cmd.AddComponent(e2, new Velocity(0, -0.5f));
                cmd.AddComponent(e2, new Health(150));

                var ctx2 = new ConsoleViewContext { DisplayName = "Entity2", Color = ConsoleColor.Cyan };
                world.RegisterContext(e2, ctx2);
                world.AttachBinder(e2, new PositionBinder());
                world.AttachBinder(e2, new HealthBinder());
            }

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... binders will log component changes.");
            Console.WriteLine("Press any key to exit.");

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
