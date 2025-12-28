// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Message Bus Performance Benchmarks
// File: MessageBusBenchmarks.cs
// Purpose: Benchmark message bus operations
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using ZenECS.Core;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Benchmarks
{
    /// <summary>
    /// Benchmarks for message bus operations.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class MessageBusBenchmarks
    {
        private IKernel? _kernel;
        private IWorld? _world;
        private int _messageCount;

        [GlobalSetup]
        public void Setup()
        {
            _kernel = new Kernel();
            _world = _kernel.CreateWorld(null, "MessageBenchmarkWorld");
            
            // Subscribe to messages
            _world.Subscribe<TestMessage>(OnTestMessage);
            _world.Subscribe<DamageMessage>(OnDamageMessage);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _world?.Dispose();
            _kernel?.Dispose();
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void PublishMessage(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _world!.Publish(new TestMessage { Value = i });
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void PublishMultipleMessageTypes(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _world!.Publish(new TestMessage { Value = i });
                _world!.Publish(new DamageMessage { Amount = i * 10 });
            }
        }

        private void OnTestMessage(TestMessage msg)
        {
            _messageCount++;
        }

        private void OnDamageMessage(DamageMessage msg)
        {
            _messageCount++;
        }
    }

    // Test messages
    public struct TestMessage : IMessage
    {
        public int Value { get; init; }
    }

    public struct DamageMessage : IMessage
    {
        public float Amount { get; init; }
    }
}

