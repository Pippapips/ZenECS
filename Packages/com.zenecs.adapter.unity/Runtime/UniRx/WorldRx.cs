// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — UniRx
// File: WorldRx.cs
// Purpose: UniRx extension helpers that bridge ZenECS world messaging with
//          IObservable streams for reactive-style composition.
// Key concepts:
//   • Core → Rx: wrap IWorld message bus as IObservable<T>.
//   • Rx → Core: publish any IObservable<T> into the world's message bus.
//   • Adapter-only: keeps UniRx dependencies out of ZenECS core assemblies.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if ZENECS_UNIRX
#nullable enable
using System;
using UniRx;
using ZenECS.Core;
using ZenECS.Core.Messaging;

namespace ZenECS.Adapter.Unity.UniRx
{
    /// <summary>
    /// UniRx extensions for <see cref="IWorld"/> message streams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WorldRx"/> provides a small bridge between the ZenECS
    /// message system and UniRx, allowing:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Subscribing to ECS messages as <see cref="IObservable{T}"/> sequences.
    /// </description></item>
    /// <item><description>
    /// Publishing events from UniRx observable pipelines back into the ECS
    /// message bus.
    /// </description></item>
    /// </list>
    /// <para>
    /// Typical patterns:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>View input → ECS message</b>
    /// <code>
    /// this.OnMouseDownAsObservable()
    ///     .ThrottleFirst(TimeSpan.FromMilliseconds(120))
    ///     .Select(_ => new ClickMessage(world, entity))
    ///     .PublishFrom(world)   // stream.Subscribe(m =&gt; world.Publish(m))
    ///     .AddTo(this.BindTo());
    /// </code>
    /// </description></item>
    /// <item><description>
    /// <b>ECS message → view reaction</b>
    /// <code>
    /// world.Messages&lt;HighlightChanged&gt;()
    ///     .ObserveOnMainThread()
    ///     .Subscribe(m =&gt; _ctx.SetHighlight(m.On))
    ///     .AddTo(this.BindTo());
    /// </code>
    /// </description></item>
    /// </list>
    /// </remarks>
    public static class WorldRx
    {
        /// <summary>
        /// Converts the world's message stream for a given message type into
        /// a UniRx observable sequence.
        /// </summary>
        /// <typeparam name="T">
        /// Message type to subscribe to. Must be a value type that implements
        /// <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="w">The ECS world whose messages should be observed.</param>
        /// <returns>
        /// An <see cref="IObservable{T}"/> that emits each message of type
        /// <typeparamref name="T"/> published on the world.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned observable:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// Subscribes to <see cref="IWorld.Subscribe{T}(Action{T})"/> when a
        /// subscription is created.
        /// </description></item>
        /// <item><description>
        /// Forwards each received message to <see cref="IObserver{T}.OnNext"/>.
        /// </description></item>
        /// <item><description>
        /// Disposes the underlying world subscription when the UniRx
        /// subscription is disposed.
        /// </description></item>
        /// </list>
        /// </remarks>
        public static IObservable<T> Messages<T>(this IWorld w) where T : struct, IMessage
            => Observable.Create<T>(obs =>
            {
                var sub = w.Subscribe<T>(m => obs.OnNext(m));
                return Disposable.Create(() => sub.Dispose());
            });

        /// <summary>
        /// Publishes all values from the observable stream into the world's
        /// message bus.
        /// </summary>
        /// <typeparam name="T">
        /// Message type carried by the stream. Must be a value type that
        /// implements <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="w">The ECS world that will receive the messages.</param>
        /// <param name="stream">
        /// The source observable whose values will be forwarded as messages.
        /// </param>
        /// <returns>
        /// A disposable representing the active subscription from
        /// <paramref name="stream"/> to <paramref name="w"/>. Disposing it
        /// stops forwarding messages.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is equivalent to:
        /// </para>
        /// <code>
        /// stream.Subscribe(m =&gt; w.Publish(m));
        /// </code>
        /// </remarks>
        public static IDisposable PublishFrom<T>(this IWorld w, IObservable<T> stream)
            where T : struct, IMessage
            => stream.Subscribe(m => w.Publish(m));

        /// <summary>
        /// Publishes all values from the observable stream into the world's
        /// message bus. This overload allows fluent chaining from observables.
        /// </summary>
        /// <typeparam name="T">
        /// Message type carried by the stream. Must be a value type that
        /// implements <see cref="IMessage"/>.
        /// </typeparam>
        /// <param name="stream">
        /// The source observable whose values will be forwarded as messages.
        /// </param>
        /// <param name="w">The ECS world that will receive the messages.</param>
        /// <returns>
        /// A disposable representing the active subscription from
        /// <paramref name="stream"/> to <paramref name="w"/>. Disposing it
        /// stops forwarding messages.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is equivalent to:
        /// </para>
        /// <code>
        /// stream.Subscribe(m =&gt; w.Publish(m));
        /// </code>
        /// <para>
        /// This overload enables fluent chaining in observable pipelines:
        /// </para>
        /// <code>
        /// observable
        ///     .Select(x =&gt; new Message(x))
        ///     .PublishFrom(world)
        ///     .AddTo(disposables);
        /// </code>
        /// </remarks>
        public static IDisposable PublishFrom<T>(this IObservable<T> stream, IWorld w)
            where T : struct, IMessage
            => stream.Subscribe(m => w.Publish(m));
    }
}
#endif
