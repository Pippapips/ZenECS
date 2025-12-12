using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldBinderTests
{
    private sealed class TestBinder : BaseBinder
    {
        public bool WasBound { get; private set; }
        public bool WasUnbound { get; private set; }
        public int ApplyCount { get; private set; }
        public Entity BoundEntity { get; private set; }

        protected override void OnBind(Entity e)
        {
            WasBound = true;
            BoundEntity = e;
        }

        protected override void OnUnbind()
        {
            WasUnbound = true;
        }

        protected override void OnApply(IWorld w, Entity e)
        {
            ApplyCount++;
        }
    }

    private sealed class AnotherBinder : BaseBinder
    {
        public int ApplyCount { get; private set; }

        protected override void OnApply(IWorld w, Entity e)
        {
            ApplyCount++;
        }
    }

    [Fact]
    public void AttachBinder_attaches_binder_to_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder = new TestBinder();

        host.World.AttachBinder(e1, binder);

        Assert.True(host.World.HasBinder<TestBinder>(e1));
        Assert.True(binder.WasBound);
        Assert.Equal(e1, binder.BoundEntity);
    }

    [Fact]
    public void HasBinder_returns_true_when_binder_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder = new TestBinder();

        host.World.AttachBinder(e1, binder);

        Assert.True(host.World.HasBinder<TestBinder>(e1));
    }

    [Fact]
    public void HasBinder_returns_false_when_binder_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        Assert.False(host.World.HasBinder<TestBinder>(e1));
    }

    [Fact]
    public void DetachBinder_detaches_specific_binder()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder1 = new TestBinder();
        var binder2 = new AnotherBinder();

        host.World.AttachBinder(e1, binder1);
        host.World.AttachBinder(e1, binder2);

        host.World.DetachBinder(e1, binder1);

        Assert.False(host.World.HasBinder<TestBinder>(e1));
        Assert.True(binder1.WasUnbound);
    }

    [Fact]
    public void DetachAllBinders_detaches_all_binders()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder1 = new TestBinder();
        var binder2 = new AnotherBinder();

        host.World.AttachBinder(e1, binder1);
        host.World.AttachBinder(e1, binder2);

        host.World.DetachAllBinders(e1);

        Assert.False(host.World.HasBinder<TestBinder>(e1));
        Assert.True(binder1.WasUnbound);
    }

    [Fact]
    public void DetachBinder_by_type_detaches_binders_of_type()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder1 = new TestBinder();
        var binder2 = new AnotherBinder();

        host.World.AttachBinder(e1, binder1);
        host.World.AttachBinder(e1, binder2);

        bool removed = host.World.DetachBinder(e1, typeof(TestBinder));

        Assert.True(removed);
        Assert.False(host.World.HasBinder<TestBinder>(e1));
        Assert.True(host.World.HasBinder<AnotherBinder>(e1));
    }

    [Fact]
    public void GetAllBinders_returns_all_binders()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder1 = new TestBinder();
        var binder2 = new AnotherBinder();

        host.World.AttachBinder(e1, binder1);
        host.World.AttachBinder(e1, binder2);

        var binders = host.World.GetAllBinders(e1);
        Assert.Equal(2, binders.Length);

        var types = binders.Select(b => b.type).ToList();
        Assert.Contains(typeof(TestBinder), types);
        Assert.Contains(typeof(AnotherBinder), types);
    }

    [Fact]
    public void GetAllBinderList_returns_readonly_list()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder1 = new TestBinder();
        var binder2 = new AnotherBinder();

        host.World.AttachBinder(e1, binder1);
        host.World.AttachBinder(e1, binder2);

        var binders = host.World.GetAllBinderList(e1);
        Assert.NotNull(binders);
        Assert.Equal(2, binders!.Count);
    }

    [Fact]
    public void GetAllBinderList_returns_null_when_no_binders()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        var binders = host.World.GetAllBinderList(e1);
        Assert.Null(binders);
    }

    [Fact]
    public void Binders_are_detached_when_entity_destroyed()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var binder = new TestBinder();

        host.World.AttachBinder(e1, binder);
        Assert.True(host.World.HasBinder<TestBinder>(e1));

        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e1);
        });

        Assert.False(host.World.HasBinder<TestBinder>(e1));
        Assert.True(binder.WasUnbound);
    }

    [Fact]
    public void AttachBinder_with_null_throws_exception()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        // BindingRouter.Attach throws ArgumentNullException when binder is null
        Assert.Throws<ArgumentNullException>(() =>
        {
            host.World.AttachBinder(e1, null);
        });

        // No binders should be attached
        var binders = host.World.GetAllBinderList(e1);
        Assert.Null(binders);
    }
}

