// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 09 Write Hooks & Validators
// File: WriteHooksValidators.cs
// Purpose: Demonstrates write permission hooks and component value validators.
// Key concepts:
//   • Write permission hooks (AddWritePermission)
//   • Read permission hooks (AddReadPermission)
//   • Typed validators (AddValidator<T>)
//   • Object-level validators (AddValidator)
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

namespace ZenEcsCoreSamples.WriteHooksValidators
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

    public readonly struct Locked
    {
        // Marker component - entities with this cannot be modified
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// System that sets up write hooks and validators.
    /// </summary>
    [FixedGroup]
    public sealed class HookSetupSystem : ISystemLifecycle
    {
        private Func<Entity, Type, bool>? _writePermissionHook;
        private Func<Health, bool>? _healthValidator;

        public void Initialize(IWorld w)
        {
            // Write permission hook: prevent writes to entities with Locked component
            _writePermissionHook = (entity, componentType) =>
            {
                if (w.HasComponent<Locked>(entity))
                {
                    Console.WriteLine($"[Permission] Write denied: Entity {entity.Id} is Locked, cannot modify {componentType.Name}");
                    return false;
                }
                return true;
            };
            w.AddWritePermission(_writePermissionHook);

            // Typed validator: Health must be between 0 and 200
            _healthValidator = (health) =>
            {
                if (health.Value < 0 || health.Value > 200)
                {
                    Console.WriteLine($"[Validator] Health validation failed: {health.Value} (must be 0-200)");
                    return false;
                }
                return true;
            };
            w.AddValidator(_healthValidator);

            // Object-level validator: Position coordinates must be finite numbers
            w.AddValidator((object obj) =>
            {
                if (obj is Position pos)
                {
                    if (float.IsNaN(pos.X) || float.IsNaN(pos.Y) || 
                        float.IsInfinity(pos.X) || float.IsInfinity(pos.Y))
                    {
                        Console.WriteLine($"[Validator] Position validation failed: ({pos.X}, {pos.Y}) (must be finite)");
                        return false;
                    }
                }
                return true;
            });

            Console.WriteLine("[Setup] Write hooks and validators registered");
        }

        public void Shutdown()
        {
            // Cleanup would happen here if we had access to the world
            // In practice, hooks are cleared when world is disposed
        }

        public void Run(IWorld w, float dt)
        {
            // System runs but doesn't need to do anything - hooks handle the work
        }
    }

    /// <summary>
    /// System that attempts various writes to demonstrate hooks and validators.
    /// </summary>
    [FixedGroup]
    public sealed class WriteAttemptSystem : ISystem
    {
        private int _frameCount;
        private Entity? _lockedEntity;
        private Entity? _normalEntity;

        public void Run(IWorld w, float dt)
        {
            _frameCount++;

            // Create entities on frame 1
            if (_frameCount == 1)
            {
                using var cmd = w.BeginWrite();
                _normalEntity = cmd.CreateEntity();
                cmd.AddComponent(_normalEntity.Value, new Position(0, 0));
                cmd.AddComponent(_normalEntity.Value, new Health(100));

                _lockedEntity = cmd.CreateEntity();
                cmd.AddComponent(_lockedEntity.Value, new Position(10, 10));
                cmd.AddComponent(_lockedEntity.Value, new Health(50));
                cmd.AddComponent(_lockedEntity.Value, new Locked());

                Console.WriteLine($"[Demo] Created normal entity {_normalEntity.Value.Id} and locked entity {_lockedEntity.Value.Id}");
            }

            // Attempt valid write on frame 60
            if (_frameCount == 60)
            {
                if (_normalEntity.HasValue)
                {
                    using var cmd = w.BeginWrite();
                    cmd.ReplaceComponent(_normalEntity.Value, new Health(150));
                    Console.WriteLine($"[Demo] Valid write: Updated entity {_normalEntity.Value.Id} Health to 150");
                }
            }

            // Attempt invalid write (to locked entity) on frame 120
            if (_frameCount == 120)
            {
                if (_lockedEntity.HasValue)
                {
                    try
                    {
                        using var cmd = w.BeginWrite();
                        cmd.ReplaceComponent(_lockedEntity.Value, new Health(75));
                        Console.WriteLine($"[Demo] Write succeeded (unexpected)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Demo] Write blocked by permission hook: {ex.Message}");
                    }
                }
            }

            // Attempt invalid value (Health > 200) on frame 180
            if (_frameCount == 180)
            {
                if (_normalEntity.HasValue)
                {
                    try
                    {
                        using var cmd = w.BeginWrite();
                        cmd.ReplaceComponent(_normalEntity.Value, new Health(250));
                        Console.WriteLine($"[Demo] Invalid value write succeeded (unexpected)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Demo] Write blocked by validator: {ex.Message}");
                    }
                }
            }

            // Attempt invalid Position (NaN) on frame 240
            if (_frameCount == 240)
            {
                if (_normalEntity.HasValue)
                {
                    try
                    {
                        using var cmd = w.BeginWrite();
                        cmd.ReplaceComponent(_normalEntity.Value, new Position(float.NaN, 0));
                        Console.WriteLine($"[Demo] Invalid Position write succeeded (unexpected)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Demo] Write blocked by validator: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Read-only presentation: prints current world state.
    /// </summary>
    [FrameViewGroup]
    public sealed class PrintStateSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            if (w.FrameCount % 60 == 0) // Print every second
            {
                Console.WriteLine($"\n=== Frame {w.FrameCount} ===");
                foreach (var (e, pos) in w.Query<Position>())
                {
                    var health = w.HasComponent<Health>(e) 
                        ? w.ReadComponent<Health>(e).ToString() 
                        : "no Health";
                    var locked = w.HasComponent<Locked>(e) ? " [LOCKED]" : "";
                    Console.WriteLine($"  Entity {e.Id,3}: pos={pos}, {health}{locked}");
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
            Console.WriteLine("=== ZenECS Core Sample - Write Hooks & Validators (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new HookSetupSystem(),
                new WriteAttemptSystem(),
                new PrintStateSystem()
            ]);

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... hooks and validators will log permission/validation results.");
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
