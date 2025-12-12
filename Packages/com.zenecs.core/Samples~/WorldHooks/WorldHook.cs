// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 06 World Hooks (Kernel style)
// File: WorldHook.cs
// Purpose: Demonstrates per-world read/write permissions and validator hooks.
// Key concepts:
//   • Write permission (even entity IDs only)
//   • Validator (Mana >= 0)
//   • Read permission (allow reads only for Mana type)
//   • Hooks can be removed/cleared at runtime
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Config;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.WorldHooks
{
    // Component used in this sample
    public readonly struct Mana
    {
        public readonly int Value;
        public Mana(int v) => Value = v;
        public override string ToString() => Value.ToString();
    }

    // Program entry — Basic.cs style kernel loop
    public static class Program
    {
        // Keep references to remove later
        private static Func<Entity, Type, bool>? _writePerm;
        private static Func<Mana, bool>? _validator;

        private static void TryAdd(IWorld w, Entity e, in Mana v)
        {
            try
            {
                w.AddComponent(e, v);
                Console.WriteLine($"Add<Mana> OK on e:{e.Id} -> {v}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Add<Mana> FAIL on e:{e.Id} :: {ex.Message}");
            }
        }

        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - World Hooks (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld();
            kernel.SetCurrentWorld(world);

            // Write permission: only even entity IDs
            _writePerm = (e, t) => (e.Id & 1) == 0;
            world.AddWritePermission(_writePerm);

            // Validator: Mana must be >= 0
            _validator = (m) => m.Value >= 0;
            world.AddValidator(_validator);

            // Create entities
            var e1 = world.CreateEntity(); // odd id
            var e2 = world.CreateEntity(); // even id

            // Try to add/replace with various values
            TryAdd(world, e1, new Mana(10)); // should be rejected by write permission
            TryAdd(world, e2, new Mana(-10)); // rejected by validator
            TryAdd(world, e2, new Mana(5)); // OK

            // Read permission: allow reads only for Mana type
            world.AddReadPermission((e, t) => t == typeof(Mana));

            // Read attempts
            if (world.TryRead<Mana>(e2, out var mana))
                Console.WriteLine($"Read OK (e:{e2.Id}) -> Mana={mana.Value}");
            else
                Console.WriteLine("Read denied");

            // Remove hooks to restore default behavior
            world.RemoveWritePermission(_writePerm);
            world.RemoveValidator(_validator);
            world.ClearReadPermissions();

            const float fixedDelta = 1f / 60f;
            const int maxSubSteps = 4;

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

                Thread.Sleep(10);
            }

            Console.WriteLine("Shutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }
    }

    sealed class EcsLogger : IEcsLogger
    {
        public void Info(string msg) => Console.WriteLine(msg);
        public void Warn(string msg) => Console.WriteLine("WARN: " + msg);
        public void Error(string msg) => Console.WriteLine("ERROR: " + msg);
    }
}