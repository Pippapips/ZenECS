// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 04 Snapshot IO + Post Migration
// File: SnapshotIO_PostMig.cs
// Purpose: Demonstrates snapshot save/load and post-load migration using the
//          ZenECS serialization and migration pipeline.
// Key concepts:
//   • Binary snapshot save/load via SnapshotBackend
//   • Versioned component migration (PositionV1 → PositionV2)
//   • PostLoadMigration hook example
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Snapshot
{
    // ──────────────────────────────────────────────────────────────────────────
    // Versioned Components
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct PositionV1
    {
        public readonly float X, Y;

        public PositionV1(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    public readonly struct PositionV2
    {
        public readonly float X, Y;
        public readonly int Layer;

        public PositionV2(float x, float y, int layer = 0)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public override string ToString() => $"({X:0.##}, {Y:0.##}, layer:{Layer})";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Binary Formatters
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class PositionV1Formatter : BinaryComponentFormatter<PositionV1>
    {
        public override void Write(in PositionV1 v, ISnapshotBackend b)
        {
            b.WriteFloat(v.X);
            b.WriteFloat(v.Y);
        }

        public override PositionV1 ReadTyped(ISnapshotBackend b)
            => new PositionV1(b.ReadFloat(), b.ReadFloat());
    }

    public sealed class PositionV2Formatter : BinaryComponentFormatter<PositionV2>
    {
        public override void Write(in PositionV2 v, ISnapshotBackend b)
        {
            b.WriteFloat(v.X);
            b.WriteFloat(v.Y);
            b.WriteInt(v.Layer);
        }

        public override PositionV2 ReadTyped(ISnapshotBackend b)
            => new PositionV2(b.ReadFloat(), b.ReadFloat(), b.ReadInt());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Post-Load Migration
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class DemoPostLoadMigration : IPostLoadMigration
    {
        public int Order => 0;

        public void Run(IWorld world)
        {
            using var cmd = world.BeginWrite();

            foreach (var (e, posV1) in world.Query<PositionV1>())
            {
                cmd.AddComponent(e, new PositionV2(posV1.X, posV1.Y, layer: 1));
                cmd.RemoveComponent<PositionV1>(e);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    [FrameViewGroup]
    public sealed class PrintSummarySystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // read-only logging for demonstration
            foreach (var (e, posV1) in w.Query<PositionV1>())
            {
                Console.WriteLine($"Entity {e.Id}: PositionV1={posV1}");
            }

            foreach (var (e, posV2) in w.Query<PositionV2>())
            {
                Console.WriteLine($"Entity {e.Id}: PositionV2={posV2}");
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
            Console.WriteLine("=== ZenECS Core Sample - SnapshotIO + PostMigration (Kernel) ===");

            IKernel kernel = new Kernel();
            var world = kernel.CreateWorld(new WorldConfig(initialEntityCapacity: 8));
            world.AddSystems([
                new PrintSummarySystem()
            ]);

            // Register StableIds & formatters at runtime
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            // Create data in V1
            Entity e;
            using (var cmd = world.BeginWrite())
            {
                e = cmd.CreateEntity();
                cmd.AddComponent(e, new PositionV1(3, 7));
            }

            kernel.PumpAndLateFrame(0, 0, 1);

            // Save snapshot (binary) into memory stream
            using var ms = new MemoryStream();
            world.SaveFullSnapshotBinary(ms);
            Console.WriteLine($"Saved snapshot bytes: {ms.Length}");

            // Modify entity after save (to show snapshot contains original state)
            using (var cmd2 = world.BeginWrite())
            {
                cmd2.AddComponent(e, new PositionV1(103, 107));
            }

            kernel.PumpAndLateFrame(0, 0, 1);

            world.RemoveSystem<PrintSummarySystem>();

            // Load snapshot into a NEW world
            var world2 = kernel.CreateWorld(new WorldConfig(initialEntityCapacity: 8));
            world2.AddSystems([
                new PrintSummarySystem()
            ]);
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            // Register migration - will be executed automatically by LoadFullSnapshotBinary
            PostLoadMigrationRegistry.Register(new DemoPostLoadMigration());

            ms.Position = 0;
            world2.LoadFullSnapshotBinary(ms);
            // LoadFullSnapshotBinary loads PositionV1 components and runs PostLoadMigrationRegistry.RunAll(world2)
            // Migration uses CommandBuffer, so we need to flush it to apply changes
            kernel.PumpAndLateFrame(0, 0, 1);

            // Verify migration results
            foreach (var (e2, posV2) in world2.Query<PositionV2>())
            {
                Console.WriteLine($"Migrated entity {e2.Id} → {posV2}");
            }

            // Run systems to print current state (after migration)
            kernel.PumpAndLateFrame(0, 0, 1);

            world2.RemoveSystem<PrintSummarySystem>();
            
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
}