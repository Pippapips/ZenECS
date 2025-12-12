using System;
using System.Linq;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldContextTests
{
    private sealed class TestContext : IContext, IContextInitialize
    {
        public int Value { get; set; }
        public bool WasInitialized { get; private set; }
        public bool WasReinitialized { get; private set; }

        public void Initialize(IWorld w, Entity e, IContextLookup l)
        {
            WasInitialized = true;
        }

        public void Deinitialize(IWorld w, Entity e)
        {
            WasInitialized = false;
        }
    }

    private sealed class AnotherContext : IContext
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void RegisterContext_registers_context_for_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        host.World.RegisterContext(e1, ctx);

        Assert.True(host.World.HasContext<TestContext>(e1));
    }

    [Fact]
    public void HasContext_returns_true_when_context_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        host.World.RegisterContext(e1, ctx);

        Assert.True(host.World.HasContext<TestContext>(e1));
        Assert.True(host.World.HasContext(e1, typeof(TestContext)));
    }

    [Fact]
    public void HasContext_returns_false_when_context_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        Assert.False(host.World.HasContext<TestContext>(e1));
        Assert.False(host.World.HasContext(e1, typeof(TestContext)));
    }

    [Fact]
    public void GetAllContexts_returns_all_contexts_for_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx1 = new TestContext { Value = 42 };
        var ctx2 = new AnotherContext { Name = "Test" };

        host.World.RegisterContext(e1, ctx1);
        host.World.RegisterContext(e1, ctx2);

        var contexts = host.World.GetAllContexts(e1);
        Assert.Equal(2, contexts.Length);

        var types = contexts.Select(c => c.type).ToList();
        Assert.Contains(typeof(TestContext), types);
        Assert.Contains(typeof(AnotherContext), types);
    }

    [Fact]
    public void GetAllContexts_returns_empty_when_no_contexts()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        var contexts = host.World.GetAllContexts(e1);
        Assert.Empty(contexts);
    }

    [Fact]
    public void RemoveContext_removes_specific_context()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx1 = new TestContext { Value = 42 };
        var ctx2 = new AnotherContext { Name = "Test" };

        host.World.RegisterContext(e1, ctx1);
        host.World.RegisterContext(e1, ctx2);

        bool removed = host.World.RemoveContext(e1, ctx1);

        Assert.True(removed);
        Assert.False(host.World.HasContext<TestContext>(e1));
        Assert.True(host.World.HasContext<AnotherContext>(e1));
    }

    [Fact]
    public void RemoveContext_returns_false_when_context_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        bool removed = host.World.RemoveContext(e1, ctx);
        Assert.False(removed);
    }

    [Fact]
    public void ReinitializeContext_reinitializes_context()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        host.World.RegisterContext(e1, ctx);

        // ReinitializeContext requires IContextInitialize to be implemented
        // TestContext implements IContextInitialize, so it should return true
        bool reinitialized = host.World.ReinitializeContext(e1, ctx);
        Assert.True(reinitialized);
        Assert.True(ctx.WasInitialized || ctx.WasReinitialized);
    }

    [Fact]
    public void ReinitializeContext_returns_false_when_context_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        bool reinitialized = host.World.ReinitializeContext(e1, ctx);
        Assert.False(reinitialized);
    }

    [Fact]
    public void Contexts_are_cleared_when_entity_destroyed()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var ctx = new TestContext { Value = 42 };

        host.World.RegisterContext(e1, ctx);
        Assert.True(host.World.HasContext<TestContext>(e1));

        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e1);
        });

        // Context should be cleared
        Assert.False(host.World.HasContext<TestContext>(e1));
    }
}

