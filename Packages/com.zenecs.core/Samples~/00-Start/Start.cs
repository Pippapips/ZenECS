using System.Diagnostics;
using ZenECS.Core;

namespace ZenEcsCoreSamples.Start
{
    /// <summary>
    /// Entry point demonstrating ZenECS kernel-driven loop.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample ===");

            var kernel = new Kernel();
            var world = kernel.CreateWorld();
            kernel.SetCurrentWorld(world);

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
                kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

                Thread.Sleep(1); // Reduce CPU load
            }

            Console.WriteLine("Shutting down...");
            kernel.Dispose();
            Console.WriteLine("Done.");
        }
    }
}