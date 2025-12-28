// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 05 World Reset
// File: WorldReset.cs
// Purpose: Demonstrates World.Reset(...) behaviors:
//          • Reset(keepCapacity: true)  → fast clear, keep internal arrays/pools
//          • Reset(keepCapacity: false) → hard reset from initial config
// Key concepts:
//   • Kernel boot pattern (Basic.cs style)
//   • Simulation system does the reset demo once
//   • Presentation system stays read-only (Late)
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.WorldReset
{
    // ──────────────────────────────────────────────────────────────────────────
    // Components
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>Simple health component to verify data presence.</summary>
    public readonly struct Health
    {
        public readonly int Value;
        public Health(int v) => Value = v;
        public override string ToString() => $"HP={Value}";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry — Basic.cs style Kernel loop
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - World Reset (Kernel) ===");

            var kernel = new Kernel();
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);
            
            Console.WriteLine("=== World.Reset demo (keepCapacity vs hard reset) ===");

            // Seed initial entities
            Entity e1, e2;
            using (var cmd = world.BeginWrite())
            {
                e1 = cmd.CreateEntity();
                e2 = cmd.CreateEntity();
                cmd.AddComponent(e1, new Health(100));
                cmd.AddComponent(e2, new Health(50));
            }
            kernel.PumpAndLateFrame(0, 0, 1);
            Console.WriteLine($"Before reset: alive={world.AliveCount}, e1.Has(Health)={world.HasComponent<Health>(e1)}");

            // Option A: Keep capacity (fast clear). Preserves internal arrays/pools.
            world.Reset(keepCapacity: true);
            Console.WriteLine($"After Reset(keepCapacity:true): alive={world.AliveCount}");
            // Note: e1 and e2 are now invalid after reset

            // Re-seed to verify the world still works and reuses capacity
            Entity e3;
            using (var cmd = world.BeginWrite())
            {
                e3 = cmd.CreateEntity();
                cmd.AddComponent(e3, new Health(77));
            }
            kernel.PumpAndLateFrame(0, 0, 1);
            Console.WriteLine($"Re-seed: alive={world.AliveCount}, e3.Has(Health)={world.HasComponent<Health>(e3)}");

            // Option B: Hard reset — rebuild internal structures from initial config
            world.Reset(keepCapacity: false);
            Console.WriteLine($"After Reset(keepCapacity:false): alive={world.AliveCount}");
            // Note: e3 is now invalid after reset
            
            const float fixedDelta = 1f / 60f;   // 60Hz simulation
            const int   maxSubSteps = 4;

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

                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubSteps);

                Thread.Sleep(10); // be gentle to CPU
            }

            Console.WriteLine("Shutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }
    }
}
