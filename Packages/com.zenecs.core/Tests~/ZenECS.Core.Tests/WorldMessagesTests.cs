using System;
using System.Collections.Generic;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldMessagesTests
{
    private struct TestMessage : IMessage
    {
        public int Value;
        public string Text;
    }

    private struct AnotherMessage : IMessage
    {
        public float Value;
    }

    [Fact]
    public void Subscribe_and_publish_delivers_message()
    {
        using var host = new TestWorldHost();

        bool received = false;
        TestMessage receivedMsg = default;

        host.World.Subscribe<TestMessage>(msg =>
        {
            received = true;
            receivedMsg = msg;
        });

        host.World.Publish(new TestMessage { Value = 42, Text = "Hello" });

        // Message is queued, not delivered yet
        Assert.False(received);

        // Pump messages (happens during BeginFrame)
        host.TickFrame(dt: 0.016f);

        // Message should be delivered
        Assert.True(received);
        Assert.Equal(42, receivedMsg.Value);
        Assert.Equal("Hello", receivedMsg.Text);
    }

    [Fact]
    public void Multiple_subscribers_receive_same_message()
    {
        using var host = new TestWorldHost();

        var received1 = new List<TestMessage>();
        var received2 = new List<TestMessage>();

        host.World.Subscribe<TestMessage>(msg => received1.Add(msg));
        host.World.Subscribe<TestMessage>(msg => received2.Add(msg));

        host.World.Publish(new TestMessage { Value = 1, Text = "A" });
        host.World.Publish(new TestMessage { Value = 2, Text = "B" });

        host.TickFrame(dt: 0.016f);

        Assert.Equal(2, received1.Count);
        Assert.Equal(2, received2.Count);
        Assert.Equal(1, received1[0].Value);
        Assert.Equal(2, received1[1].Value);
        Assert.Equal(1, received2[0].Value);
        Assert.Equal(2, received2[1].Value);
    }

    [Fact]
    public void Unsubscribe_stops_receiving_messages()
    {
        using var host = new TestWorldHost();

        int callCount = 0;

        var subscription = host.World.Subscribe<TestMessage>(msg => callCount++);

        host.World.Publish(new TestMessage { Value = 1, Text = "A" });
        host.TickFrame(dt: 0.016f);
        Assert.Equal(1, callCount);

        // Unsubscribe
        subscription.Dispose();

        host.World.Publish(new TestMessage { Value = 2, Text = "B" });
        host.TickFrame(dt: 0.016f);

        // Should still be 1, not 2
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Messages_are_delivered_in_fifo_order()
    {
        using var host = new TestWorldHost();

        var received = new List<int>();

        host.World.Subscribe<TestMessage>(msg => received.Add(msg.Value));

        host.World.Publish(new TestMessage { Value = 1, Text = "A" });
        host.World.Publish(new TestMessage { Value = 2, Text = "B" });
        host.World.Publish(new TestMessage { Value = 3, Text = "C" });

        host.TickFrame(dt: 0.016f);

        Assert.Equal(3, received.Count);
        Assert.Equal(1, received[0]);
        Assert.Equal(2, received[1]);
        Assert.Equal(3, received[2]);
    }

    [Fact]
    public void Different_message_types_are_isolated()
    {
        using var host = new TestWorldHost();

        var testMessages = new List<TestMessage>();
        var anotherMessages = new List<AnotherMessage>();

        host.World.Subscribe<TestMessage>(msg => testMessages.Add(msg));
        host.World.Subscribe<AnotherMessage>(msg => anotherMessages.Add(msg));

        host.World.Publish(new TestMessage { Value = 42, Text = "Test" });
        host.World.Publish(new AnotherMessage { Value = 3.14f });

        host.TickFrame(dt: 0.016f);

        Assert.Single(testMessages);
        Assert.Single(anotherMessages);
        Assert.Equal(42, testMessages[0].Value);
        Assert.Equal(3.14f, anotherMessages[0].Value);
    }

    [Fact]
    public void Messages_are_isolated_between_worlds()
    {
        using var host1 = new TestWorldHost();
        using var host2 = new TestWorldHost();

        var received1 = new List<TestMessage>();
        var received2 = new List<TestMessage>();

        host1.World.Subscribe<TestMessage>(msg => received1.Add(msg));
        host2.World.Subscribe<TestMessage>(msg => received2.Add(msg));

        // Publish to world1
        host1.World.Publish(new TestMessage { Value = 1, Text = "World1" });
        host1.TickFrame(dt: 0.016f);

        // Publish to world2
        host2.World.Publish(new TestMessage { Value = 2, Text = "World2" });
        host2.TickFrame(dt: 0.016f);

        // Each world should only receive its own messages
        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Equal(1, received1[0].Value);
        Assert.Equal(2, received2[0].Value);
    }

    [Fact]
    public void Multiple_pumps_deliver_queued_messages_separately()
    {
        using var host = new TestWorldHost();

        var received = new List<int>();

        host.World.Subscribe<TestMessage>(msg => received.Add(msg.Value));

        // Publish first batch
        host.World.Publish(new TestMessage { Value = 1, Text = "A" });
        host.World.Publish(new TestMessage { Value = 2, Text = "B" });
        host.TickFrame(dt: 0.016f);

        Assert.Equal(2, received.Count);

        // Publish second batch
        host.World.Publish(new TestMessage { Value = 3, Text = "C" });
        host.TickFrame(dt: 0.016f);

        Assert.Equal(3, received.Count);
        Assert.Equal(3, received[2]);
    }

    [Fact]
    public void Subscribe_after_publish_still_receives_message()
    {
        using var host = new TestWorldHost();

        bool received = false;

        // Publish before subscribing
        host.World.Publish(new TestMessage { Value = 42, Text = "Test" });

        // Subscribe after publish
        host.World.Subscribe<TestMessage>(msg =>
        {
            received = true;
            Assert.Equal(42, msg.Value);
        });

        // Pump messages
        host.TickFrame(dt: 0.016f);

        // Should receive the message that was published before subscription
        Assert.True(received);
    }

    [Fact]
    public void Multiple_unsubscribes_work_correctly()
    {
        using var host = new TestWorldHost();

        int count1 = 0;
        int count2 = 0;
        int count3 = 0;

        var sub1 = host.World.Subscribe<TestMessage>(msg => count1++);
        var sub2 = host.World.Subscribe<TestMessage>(msg => count2++);
        var sub3 = host.World.Subscribe<TestMessage>(msg => count3++);

        host.World.Publish(new TestMessage { Value = 1, Text = "A" });
        host.TickFrame(dt: 0.016f);

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);

        // Unsubscribe one
        sub2.Dispose();

        host.World.Publish(new TestMessage { Value = 2, Text = "B" });
        host.TickFrame(dt: 0.016f);

        Assert.Equal(2, count1);
        Assert.Equal(1, count2); // Should not increment
        Assert.Equal(2, count3);

        // Unsubscribe another
        sub1.Dispose();

        host.World.Publish(new TestMessage { Value = 3, Text = "C" });
        host.TickFrame(dt: 0.016f);

        Assert.Equal(2, count1); // Should not increment
        Assert.Equal(1, count2);
        Assert.Equal(3, count3); // Only this one should increment
    }

    [Fact]
    public void Empty_message_queue_does_not_crash()
    {
        using var host = new TestWorldHost();

        bool received = false;
        host.World.Subscribe<TestMessage>(msg => received = true);

        // Pump without publishing
        host.TickFrame(dt: 0.016f);

        Assert.False(received);
    }
}

