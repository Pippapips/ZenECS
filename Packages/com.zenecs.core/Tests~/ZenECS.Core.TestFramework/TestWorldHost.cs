using System;
using ZenECS.Core;
using ZenECS.Core.Config;

namespace ZenECS.Core.TestFramework;

/// <summary>
/// Lightweight test host for easily creating and deterministically stepping a Core-only world.
/// </summary>
public sealed class TestWorldHost : IDisposable
{
    public Kernel Kernel { get; }
    public IWorld World { get; }

    /// <summary>
    /// Default fixed delta to use when calling <see cref="TickFixed"/> or <see cref="TickFullFrame"/>.
    /// </summary>
    public float DefaultFixedDelta { get; }

    public TestWorldHost(
        KernelOptions? kernelOptions = null,
        WorldConfig? worldConfig = null,
        float defaultFixedDelta = 1f / 60f)
    {
        Kernel = new Kernel(kernelOptions);
        World = Kernel.CreateWorld(worldConfig);
        DefaultFixedDelta = defaultFixedDelta;
    }

    /// <summary>
    /// Steps one frame from BeginFrame (including message pump) through LateFrame.
    /// </summary>
    public void TickFrame(float dt = 0f, float lateAlpha = 1f)
    {
        Kernel.BeginFrame(dt);
        Kernel.LateFrame(lateAlpha);
    }

    /// <summary>
    /// Executes one fixed-step simulation.
    /// </summary>
    public void TickFixed(float? fixedDelta = null)
    {
        Kernel.FixedStep(fixedDelta ?? DefaultFixedDelta);
    }

    /// <summary>
    /// Executes a full frame: BeginFrame → N times FixedStep → LateFrame.
    /// </summary>
    public void TickFullFrame(float dt = 0f, float? fixedDelta = null, int fixedSteps = 1, float lateAlpha = 1f)
    {
        Kernel.BeginFrame(dt);

        var step = fixedDelta ?? DefaultFixedDelta;
        for (int i = 0; i < fixedSteps; i++)
        {
            Kernel.FixedStep(step);
        }

        Kernel.LateFrame(lateAlpha);
    }

    public void Dispose()
    {
        Kernel.Dispose();
    }
}

