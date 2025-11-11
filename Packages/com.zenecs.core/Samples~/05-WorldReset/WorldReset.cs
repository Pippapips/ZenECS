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
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS;              // Kernel
using ZenECS.Core;
using ZenECS.Core.Systems;        // IVariableRunSystem, IPresentationSystem

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
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs once to demonstrate World.Reset(keepCapacity) behaviors.
    /// </summary>
    [SimulationGroup]
    public sealed class WorldResetDemoSystem : IVariableRunSystem
    {
        private bool _done;

        public void Run(IWorld w, float dt)
        {
            if (_done) return;

            Console.WriteLine("=== World.Reset demo (keepCapacity vs hard reset) ===");

            // Seed a few entities
            var e1 = w.SpawnEntity();
            var e2 = w.SpawnEntity();
            w.AddComponent(e1, new Health(100));
            w.AddComponent(e2, new Health(50));

            Console.WriteLine($"Before reset: alive={w.AliveCount}, e1.Has(Health)={w.HasComponent<Health>(e1)}");

            // Option A: Keep capacity (fast clear). Preserves internal arrays/pools.
            w.Reset(keepCapacity: true);
            Console.WriteLine($"After Reset(keepCapacity:true): alive={w.AliveCount}");

            // Re-seed to verify the world still works and reuses capacity
            var e3 = w.SpawnEntity();
            w.AddComponent(e3, new Health(77));
            Console.WriteLine($"Re-seed: alive={w.AliveCount}, e3.Has(Health)={w.HasComponent<Health>(e3)}");

            // Option B: Hard reset — rebuild internal structures from initial config
            w.Reset(keepCapacity: false);
            Console.WriteLine($"After Reset(keepCapacity:false): alive={w.AliveCount}");

            _done = true;
        }
    }

    /// <summary>
    /// Read-only presentation: prints a lightweight world summary each Late.
    /// </summary>
    [PresentationGroup]
    public sealed class PrintSummarySystem : IPresentationSystem
    {
        public void Run(IWorld w, float dt, float alpha)
        {
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry — Basic.cs style Kernel loop
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample — World Reset (Kernel) ===");

            var kernel = new Kernel();
            var world = kernel.CreateWorld();
            kernel.SetCurrentWorld(world);
            
            world.AddSystems([
                new WorldResetDemoSystem(),
                new PrintSummarySystem()
            ]);

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
