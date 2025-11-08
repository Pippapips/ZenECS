// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 03 CommandBuffer
// File: CommandBuffer.cs
// Purpose: Demonstrates deferred and immediate entity modifications using
//          World.BeginWrite / Schedule / RunScheduledJobs / EndWrite.
// Key concepts:
//   • Collect write operations in a CommandBuffer (thread-safe)
//   • Defer application via Schedule(...) + RunScheduledJobs()
//   • Apply immediately via EndWrite(cb)
//   • Use Kernel loop (Pump + LateFrame) with Simulation/Presentation split
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS;              // Kernel
using ZenECS.Core;
using ZenECS.Core.Abstractions.Diagnostics;
using ZenECS.Core.Systems;        // IVariableRunSystem, IPresentationSystem

namespace ZenEcsCoreSamples.CommandBuffer
{
    // ──────────────────────────────────────────────────────────────────────────
    // Components
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct Health
    {
        public readonly int Value;
        public Health(int value) => Value = value;
        public override string ToString() => Value.ToString();
    }

    public readonly struct Stunned
    {
        public readonly float Seconds;
        public Stunned(float seconds) => Seconds = seconds;
        public override string ToString() => $"{Seconds:0.##}s";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs once to demonstrate CommandBuffer usage:
    /// 1) Build a CB: Add/Replace/Remove → Schedule → RunScheduledJobs
    /// 2) Build another CB → EndWrite (immediate apply)
    /// </summary>
    [SimulationGroup]
    public sealed class CommandBufferDemoSystem : IVariableRunSystem
    {
        private bool _done;

        public void Run(IWorld w, float dt)
        {
            if (_done) return;

            Console.WriteLine("=== CommandBuffer demo (deferred + immediate) ===");

            // Create two entities
            var e1 = w.SpawnEntity();
            var e2 = w.SpawnEntity();

            // 1) Build a CB (thread-safe collection of ops)
            var cb = w.BeginWrite();
            cb.AddComponent(e1, new Health(100));
            cb.AddComponent(e2, new Health(80));
            cb.AddComponent(e2, new Stunned(1.5f));

            // Replace and Remove are supported in CB
            cb.ReplaceComponent(e2, new Health(75));
            cb.RemoveComponent<Stunned>(e2);

            // Defer application: schedule now, apply later at a safe point
            w.Schedule(cb);
            Console.WriteLine("Scheduled ops. Before apply, Has<Health>(e1): " + w.HasComponent<Health>(e1));

            // Apply scheduled jobs explicitly (can also be done by your frame barrier)
            w.RunScheduledJobs();

            Console.WriteLine($"After apply (deferred): e1 Health={w.ReadComponent<Health>(e1).Value}, e2 Health={w.ReadComponent<Health>(e2).Value}, Has<Stunned>(e2)={w.HasComponent<Stunned>(e2)}");

            // 2) Immediate apply via EndWrite
            using (var cb2 = w.BeginWrite(CommandBufferApplyMode.Immediate))
            {
                cb2.ReplaceComponent(e1, new Health(42));
            }
            Console.WriteLine($"After immediate EndWrite: e1 Health={w.ReadComponent<Health>(e1).Value}");

            _done = true;
        }
    }

    /// <summary>
    /// Read-only presentation that prints current Health/Stunned states each Late.
    /// </summary>
    [PresentationGroup]
    public sealed class PrintStatusSystem : IPresentationSystem
    {
        public void Run(IWorld w, float dt, float alpha)
        {
            foreach (var e in w.Query<Health>())
            {
                var h = w.ReadComponent<Health>(e);
                var stunned = w.HasComponent<Stunned>(e) ? w.ReadComponent<Stunned>(e).ToString() : "no";
                Console.WriteLine($"Entity {e.Id,2}: Health={h.Value}, Stunned={stunned}");
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
            Console.WriteLine("=== ZenECS Core Sample - CommandBuffer (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld();
            kernel.SetCurrentWorld(world);
            
            world.Initialize(new ISystem[]
            {
                new CommandBufferDemoSystem(),
                new PrintStatusSystem()
            });

            // Main loop (same timing pattern as Basic.cs)
            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int   maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            bool running = true;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    running = false;
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                Thread.Sleep(10); // be gentle to CPU
            }

            Console.WriteLine("Shutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }
        
        /// <summary>
        /// Simple logger implementation forwarding ECS messages to console.
        /// </summary>
        class EcsLogger : IEcsLogger
        {
            public void Info(string msg)  => Console.WriteLine(msg);
            public void Warn(string msg)  => Console.Error.WriteLine(msg);
            public void Error(string msg) => Console.Error.Write(msg);
        }
    }
}
