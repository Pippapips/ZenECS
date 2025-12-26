// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 02 Messages
// File: Messages.cs
// Purpose: Demonstrates one-way data flow: View publishes messages, Simulation
//          systems mutate ECS data, Presentation reads only.
// Key concepts:
//   • View never mutates World directly (messages only)
//   • Simulation systems subscribe to messages and update components
//   • Presentation runs in Late (read-only)
//   • Uses EcsKernel.Start(...) to register systems (no manual InitializeSystems)
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Messages
{
    // ──────────────────────────────────────────────────────────────────────────
    // DATA COMPONENTS
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct Health
    {
        public readonly int Value;
        public Health(int value) => Value = value;
        public override string ToString() => $"HP={Value}";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MESSAGES (View → Logic)
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct DamageRequest : IMessage
    {
        public readonly Entity Entity;
        public readonly int Amount;
        public DamageRequest(Entity entity, int amount)
        {
            Entity = entity;
            Amount = amount;
        }
        public override string ToString() => $"DamageRequest(e:{Entity}, amt:{Amount})";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SYSTEMS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to DamageRequest messages and updates entity Health.
    /// Runs in Simulation (writes are allowed here).
    /// </summary>
    [FixedGroup]
    public sealed class DamageSystem : ISystemLifecycle
    {
        private IDisposable? _sub;

        public void Initialize(IWorld w)
        {
            // Subscribe to View-originated messages
            _sub = w.Subscribe<DamageRequest>(m =>
            {
                if (!w.IsAlive(m.Entity)) return;
                if (!w.HasComponent<Health>(m.Entity)) return;

                using var cmd = w.BeginWrite();
                var current = w.ReadComponent<Health>(m.Entity);
                var updated = new Health(Math.Max(0, current.Value - m.Amount));
                cmd.ReplaceComponent(m.Entity, updated);
                Console.WriteLine($"[Logic] e:{m.Entity} took {m.Amount} → {updated}");
            });
        }
        
        public void Shutdown()
        {
            _sub?.Dispose();
        }
        
        public void Run(IWorld w, float dt)
        {
        }
    }

    /// <summary>
    /// Read-only presentation that prints Health each Late frame.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintHealthSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, health) in w.Query<Health>())
            {
                Console.WriteLine($"Entity {e.Id,2}: {health}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PROGRAM ENTRY
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - View→Data via MessageBus (Kernel) ===");

            var kernel = new Kernel();
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new DamageSystem(),
                new PrintHealthSystem()
            ]);

            var cmd = world.BeginWrite();
            // Seed entities with Health data
            var e1 = cmd.CreateEntity();
            var e2 = cmd.CreateEntity();
            cmd.AddComponent(e1, new Health(100));
            cmd.AddComponent(e2, new Health(75));

            // Main loop mirrors Basic.cs: Pump variable step + fixed step + Late
            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int   maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press [1]/[2] to deal damage, [ESC] to quit.");

            bool running = true;
            var rand = new Random();

            while (running)
            {
                // View layer: publish messages only (never mutates World directly)
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            world.Publish(new DamageRequest(e1, rand.Next(5, 15)));
                            Console.WriteLine("[View] Sent DamageRequest → e:1");
                            break;
                        case ConsoleKey.D2:
                            world.Publish(new DamageRequest(e2, rand.Next(5, 15)));
                            Console.WriteLine("[View] Sent DamageRequest → e:2");
                            break;
                        case ConsoleKey.Escape:
                            running = false;
                            break;
                    }
                }

                // Timing (same pattern as Basic.cs)
                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                Thread.Sleep(1); // be gentle to CPU in console
            }

            Console.WriteLine("Shutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }
    }
}
