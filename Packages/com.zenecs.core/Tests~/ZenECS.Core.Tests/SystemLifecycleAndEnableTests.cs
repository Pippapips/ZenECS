using System.Collections.Generic;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class SystemLifecycleAndEnableTests
{
    [FrameInputGroup]
    private sealed class CountingSystem : ISystem
    {
        public List<string> Trace { get; } = new();
        private readonly string _label;
        public CountingSystem(string label) => _label = label;
        public void Run(IWorld w, float dt) => Trace.Add(_label);
    }

    [FrameInputGroup]
    private sealed class ToggleSystem : ISystem, ISystemEnabledFlag
    {
        public int Count;
        public bool Enabled { get; set; } = true;
        public void Run(IWorld w, float dt) => Count++;
    }

    [Fact]
    public void RemoveSystem_is_applied_at_next_frame()
    {
        using var host = new TestWorldHost();
        var sys = new CountingSystem("a");

        host.World.AddSystem(sys);
        host.TickFrame(); // add applied + run
        host.TickFrame(); // run again
        Assert.Equal(new[] { "a", "a" }, sys.Trace);

        host.World.RemoveSystem<CountingSystem>();
        host.TickFrame(); // removal applied before run

        Assert.Equal(new[] { "a", "a" }, sys.Trace);
        Assert.False(host.World.TryGetSystem<CountingSystem>(out _));
    }

    [Fact]
    public void SetEnabledSystem_skips_execution_when_disabled()
    {
        using var host = new TestWorldHost();
        var sys = new ToggleSystem();
        host.World.AddSystem(sys);

        host.TickFrame(dt: 0.016f); // apply + run once
        Assert.Equal(1, sys.Count);
        Assert.True(sys.Enabled); // Verify initial value

        Assert.True(host.World.SetEnabledSystem<ToggleSystem>(false));
        Assert.False(sys.Enabled); // Verify disabled state
        host.TickFrame(dt: 0.016f);
        Assert.Equal(1, sys.Count); // Should not execute

        Assert.True(host.World.SetEnabledSystem<ToggleSystem>(true));
        Assert.True(sys.Enabled); // Verify re-enabled state
        host.TickFrame(dt: 0.016f);
        Assert.Equal(2, sys.Count); // Should execute
    }
}

