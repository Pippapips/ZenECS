// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 08 World Hook
// File: WorldHook.cs
// Purpose: Demonstrates world-level hooks for entity and component lifecycle events.
// Key concepts:
//   • EntityEvents.EntityCreated / EntityDestroyRequested / EntityDestroy
//   • ComponentEvents.ComponentAdded / ComponentRemoved
//   • Event subscription and cleanup
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

namespace ZenEcsCoreSamples.WorldHook
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

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// System that subscribes to world hooks and logs events.
    /// </summary>
    [FixedGroup]
    public sealed class HookSubscriberSystem : ISystemLifecycle
    {
        private int _entityCreatedCount;
        private int _entityDestroyedCount;
        private int _componentAddedCount;
        private int _componentRemovedCount;

        public void Initialize(IWorld w)
        {
            // Subscribe to entity lifecycle events
            EntityEvents.EntityCreated += OnEntityCreated;
            EntityEvents.EntityDestroyRequested += OnEntityDestroyRequested;
            EntityEvents.EntityDestroy += OnEntityDestroyed;

            // Subscribe to component lifecycle events
            ComponentEvents.ComponentAdded += OnComponentAdded;
            ComponentEvents.ComponentRemoved += OnComponentRemoved;
        }

        public void Shutdown()
        {
            // Unsubscribe from all events
            EntityEvents.EntityCreated -= OnEntityCreated;
            EntityEvents.EntityDestroyRequested -= OnEntityDestroyRequested;
            EntityEvents.EntityDestroy -= OnEntityDestroyed;

            ComponentEvents.ComponentAdded -= OnComponentAdded;
            ComponentEvents.ComponentRemoved -= OnComponentRemoved;
        }

        private void OnEntityCreated(IWorld world, Entity entity)
        {
            _entityCreatedCount++;
            Console.WriteLine($"[Hook] EntityCreated: e={entity.Id} (total created: {_entityCreatedCount})");
        }

        private void OnEntityDestroyRequested(IWorld world, Entity entity)
        {
            Console.WriteLine($"[Hook] EntityDestroyRequested: e={entity.Id}");
        }

        private void OnEntityDestroyed(IWorld world, Entity entity)
        {
            _entityDestroyedCount++;
            Console.WriteLine($"[Hook] EntityDestroy: e={entity.Id} (total destroyed: {_entityDestroyedCount})");
        }

        private void OnComponentAdded(IWorld world, Entity entity, Type componentType, object value)
        {
            _componentAddedCount++;
            Console.WriteLine($"[Hook] ComponentAdded: e={entity.Id}, type={componentType.Name}, value={value} (total added: {_componentAddedCount})");
        }

        private void OnComponentRemoved(IWorld world, Entity entity, Type componentType)
        {
            _componentRemovedCount++;
            Console.WriteLine($"[Hook] ComponentRemoved: e={entity.Id}, type={componentType.Name} (total removed: {_componentRemovedCount})");
        }

        public void Run(IWorld w, float dt)
        {
            // System runs but doesn't need to do anything - hooks handle the work
        }
    }

    /// <summary>
    /// System that creates and destroys entities to trigger hooks.
    /// </summary>
    [FixedGroup]
    public sealed class EntityLifecycleDemoSystem : ISystem
    {
        private int _frameCount;
        private Entity? _entityToDestroy;

        public void Run(IWorld w, float dt)
        {
            _frameCount++;

            // Create entity on frame 1
            if (_frameCount == 1)
            {
                using var cmd = w.BeginWrite();
                var e = cmd.CreateEntity();
                cmd.AddComponent(e, new Position(0, 0));
                cmd.AddComponent(e, new Health(100));
                _entityToDestroy = e;
                Console.WriteLine($"[Demo] Created entity {e.Id} with Position and Health");
            }

            // Add component on frame 60
            if (_frameCount == 60)
            {
                if (_entityToDestroy.HasValue)
                {
                    using var cmd = w.BeginWrite();
                    cmd.AddComponent(_entityToDestroy.Value, new Health(150));
                    Console.WriteLine($"[Demo] Added second Health component to entity {_entityToDestroy.Value.Id}");
                }
            }

            // Remove component on frame 120
            if (_frameCount == 120)
            {
                if (_entityToDestroy.HasValue)
                {
                    using var cmd = w.BeginWrite();
                    cmd.RemoveComponent<Health>(_entityToDestroy.Value);
                    Console.WriteLine($"[Demo] Removed Health component from entity {_entityToDestroy.Value.Id}");
                }
            }

            // Destroy entity on frame 180
            if (_frameCount == 180)
            {
                if (_entityToDestroy.HasValue)
                {
                    using var cmd = w.BeginWrite();
                    cmd.DestroyEntity(_entityToDestroy.Value);
                    Console.WriteLine($"[Demo] Destroyed entity {_entityToDestroy.Value.Id}");
                    _entityToDestroy = null;
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
                Console.WriteLine($"Alive entities: {w.AliveCount}");

                foreach (var (e, pos) in w.Query<Position>())
                {
                    var health = w.HasComponent<Health>(e) 
                        ? w.ReadComponent<Health>(e).ToString() 
                        : "no Health";
                    Console.WriteLine($"  Entity {e.Id,3}: pos={pos}, {health}");
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
            Console.WriteLine("=== ZenECS Core Sample - World Hook (Kernel) ===");

            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld(null);
            kernel.SetCurrentWorld(world);

            world.AddSystems([
                new HookSubscriberSystem(),
                new EntityLifecycleDemoSystem(),
                new PrintStateSystem()
            ]);

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... hooks will log entity/component lifecycle events.");
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
