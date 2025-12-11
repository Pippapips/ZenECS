// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 07 Binding
// File: Binding.cs
// Purpose: Console demo showcasing the ZenECS Core binding pipeline (Unity-free)
// Key concepts:
//   • Demonstrates Entity creation, component binding, and view binding flow
//   • Uses Position component with a console-based view binder
//   • Simulates ECS runtime loop with Begin/Fixed/Late steps
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Abstractions.Config;
using ZenECS.Core.Abstractions.Diagnostics;
using ZenECS.Core.Systems;
using ZenECS.Core.Binding;

namespace ZenECS.Binding.ConsoleSample
{
    /// <summary>
    /// Simple position component for demonstration.
    /// </summary>
    public readonly struct Position : IEquatable<Position>
    {
        public readonly float X, Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        public bool Equals(Position other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>
    /// Simple position component for demonstration.
    /// </summary>
    public readonly struct Health : IEquatable<Health>
    {
        public readonly int Hp, MaxHp;
        public Health(int hp, int maxHp)
        {
            Hp = hp;
            MaxHp = maxHp;
        }
        public bool Equals(Health other) => Hp == other.Hp && MaxHp == other.MaxHp;
        public override bool Equals(object? obj) => obj is Health other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Hp, MaxHp);
        public override string ToString() => $"({Hp}, {MaxHp})";
    }

    /// <summary>
    /// A simple console binder for Position component.
    /// Prints bind/apply/unbind events to the console.
    /// </summary>
    public sealed class ConsoleViewBinder : BaseBinder,
        // IAlwaysApply,
        IBinds<Position>, IBinds<Health>
    {
        public override int Priority => 10;
        private Position _p;
        private Health _h;

        public void OnDelta(in ComponentDelta<Position> delta)
        {
            Console.WriteLine($"Position {delta.Kind}");
            if (delta.Kind == ComponentDeltaKind.Removed)
            {
                _p = new Position(0, 0);
            }
            else
            {
                _p = delta.Value;                
            }
        }
        
        public void OnDelta(in ComponentDelta<Health> delta)
        {
            Console.WriteLine($"Health {delta.Kind}");
            if (delta.Kind == ComponentDeltaKind.Removed)
            {
                _h = new Health(0, 0);
            }
            else
            {
                _h = delta.Value;
            }
        }

        public override void Apply()
        {
            Console.WriteLine($"[Apply]     e={Entity} Position={_p}");
            Console.WriteLine($"[Apply]     e={Entity} Health={_h}");
        }

        protected override void OnBind(Entity e)
        {
            Console.WriteLine($"[Bind]      e={e}");
        }

        protected override void OnUnbind()
        {
            Console.WriteLine($"[Unbind]    e={Entity}");
        }
    }

    /// <summary>
    /// Entry point demonstrating the ECS binding pipeline in a console environment.
    /// </summary>
    internal static class Program
    {
        static void Main()
        {
            var kernel = new Kernel(null, logger: new EcsLogger());
            var world = kernel.CreateWorld();
            kernel.SetCurrentWorld(world);

            // Create entity and associate with a console view
            var e = world.CreateEntity();
            var view = new ConsoleViewBinder();
            world.AttachBinder(e, view);

            // Add and modify Position component
            world.AddComponent(e, new Position(1, 1));
            world.SetComponent(e, new Position(2.5f, 4));
                    
            world.AddComponent(e, new Health(10, 100));
            
            const float fixedDelta = 1f / 60f;   // 60Hz
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            bool loop = true;
            int exitStep = 0;
            while (loop)
            {
                if (exitStep == 1)
                {
                    Console.WriteLine("Exiting... step 1");
                    if (view != null && world != null && world.IsAlive(e))
                    {
                        world.DespawnEntity(e);
                        world.DetachBinder(e, view);
                        view = null;
                    }
                    loop = false;
                }
                
                if (exitStep == 0 && Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    if (view != null && world != null && world.IsAlive(e))
                    {
                        world.RemoveComponent<Position>(e);
                        exitStep++;
                    }
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                // Performs variable-step Begin + multiple Fixed steps + alpha calculation in one call
                const int maxSubStepsPerFrame = 4;
                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                if (!loop)
                    break;
                
                Thread.Sleep(1); // CPU-friendly wait
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
