using System.Diagnostics;
using ZenECS.Core;
using ZenECS.Core.Infrastructure;

namespace ZenEcsCoreSamples.Start
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
    /// Entry point demonstrating ZenECS kernel-driven loop.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample ===");
            
            var kernel = new Kernel();
            kernel.Start();
            var world = kernel.CreateWorld(true);
            
            const float fixedDelta = 1f / 60f; // 60Hz simulation
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

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

                // Perform variable-step Begin + multiple Fixed steps + alpha calculation
                const int maxSubStepsPerFrame = 4;
                kernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
                kernel.LateFrame(alpha);

                Thread.Sleep(1); // Reduce CPU load
            }

            Console.WriteLine("Shutting down...");
            kernel.Shutdown();
            Console.WriteLine("Done.");
        }
    }
}
