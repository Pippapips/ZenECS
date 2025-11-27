#if ZENECS_UNIRX
using System;
using UniRx;
using ZenECS.Core;
using ZenECS.Core.Messaging;

namespace ZenECS.Adapter.Unity.Rx
{
    public static class WorldRx
    {
        // Core → Rx
        public static IObservable<T> Messages<T>(this IWorld w) where T : struct, IMessage
            => Observable.Create<T>(obs =>
            {
                var sub = w.Subscribe<T>(m => obs.OnNext(m));
                return Disposable.Create(() => sub.Dispose());
            });

        // Rx → Core
        public static IDisposable PublishFrom<T>(this IWorld w, IObservable<T> stream)
            where T : struct, IMessage
            => stream.Subscribe(m => w.Publish(m));
    }
    
    // View 입력 → 메시지 발행
    // this.OnMouseDownAsObservable()
    //     .ThrottleFirst(TimeSpan.FromMilliseconds(120))
    //     .Select(_ => new ClickMessage(world, entity))
    //     .PublishFrom(world)        // == stream.Subscribe(m => world.Publish(m))
    //     .AddTo(this.BindTo());
    //
    // Core 메시지 → Rx로 구독 (Adapter에서만)
    // world.Messages<HighlightChanged>()
    //     .ObserveOnMainThread()
    //     .Subscribe(m => _ctx.SetHighlight(m.On))
    //     .AddTo(this.BindTo());
}
#endif