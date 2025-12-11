using System;
using ZenECS.Core;
using ZenECS.Core.Config;

namespace ZenECS.Core.TestFramework;

/// <summary>
/// Core 전용 월드를 손쉽게 만들고 결정론적으로 스텝하기 위한 경량 테스트 호스트.
/// </summary>
public sealed class TestWorldHost : IDisposable
{
    public Kernel Kernel { get; }
    public IWorld World { get; }

    /// <summary>
    /// <see cref="TickFixed"/> 또는 <see cref="TickFullFrame"/> 호출 시 기본으로 사용할 고정 델타.
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
    /// BeginFrame(메시지 펌프 포함) 후 LateFrame까지 한 프레임을 스텝합니다.
    /// </summary>
    public void TickFrame(float dt = 0f, float lateAlpha = 1f)
    {
        Kernel.BeginFrame(dt);
        Kernel.LateFrame(lateAlpha);
    }

    /// <summary>
    /// 고정 스텝 시뮬레이션을 한 번 실행합니다.
    /// </summary>
    public void TickFixed(float? fixedDelta = null)
    {
        Kernel.FixedStep(fixedDelta ?? DefaultFixedDelta);
    }

    /// <summary>
    /// BeginFrame → N회 FixedStep → LateFrame까지 전체 프레임을 실행합니다.
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

