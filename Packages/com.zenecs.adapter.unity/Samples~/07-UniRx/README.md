# ZenECS Adapter Unity — Sample 07: UniRx Integration

This sample demonstrates how to convert ZenECS World message bus to `IObservable` streams using **UniRx**, and conversely, publish UniRx streams as ECS messages.

* **WorldRx.Messages<T>()** — Convert ECS messages to IObservable
* **WorldRx.PublishFrom()** — Publish UniRx streams as ECS messages
* **Conditional Compilation** — `ZENECS_UNIRX` define required
* **Reactive Programming** — Combine UniRx operators with ECS messages

---

## What This Sample Shows

1. **ECS → UniRx**
   Use `WorldRx.Messages<T>()` to convert ECS messages to UniRx Observable.

2. **UniRx → ECS**
   Use `WorldRx.PublishFrom()` to publish UniRx streams as ECS messages.

3. **Reactive Pipeline**
   Process messages using UniRx operators (Throttle, Filter, Select, etc.).

4. **Three Integration Patterns**
   - ECS messages → UniRx → Health update
   - Unity Input → UniRx → ECS messages (JumpIntent)
   - Observable.Interval → ECS messages (periodic damage)

5. **CompositeDisposable Management**
   Proper disposal of all UniRx subscriptions in OnDestroy.

6. **OnGUI Display**
   Shows current player health and instructions.

---

## TL;DR Flow

```
[ECS Message]
  └─ world.Messages<T>()
      └─ IObservable<T>
          └─ UniRx operators (Throttle, Filter, Select, etc.)
              └─ Subscribe() → View update

[UniRx Stream]
  └─ world.PublishFrom(stream)
      └─ world.Publish(message)
          └─ Processed in ECS systems
```

---

## File Structure

```
07-UniRx/
├── README.md
├── UniRxSample.cs              # Sample script (contains all messages, components, and systems)
│   ├── DamageMessage (IMessage)
│   ├── JumpIntent (IMessage)
│   ├── Health component
│   ├── DamageSystem (FixedGroup)
│   └── UniRx subscriptions setup
└── 07 - UniRx.unity            # Sample scene
```

---

## Usage

### 1. Convert ECS Messages to UniRx Observable

```csharp
#if ZENECS_UNIRX
using System;
using UniRx;
using ZenECS.Adapter.Unity.UniRx;
using ZenECS.Core;

var world = KernelLocator.CurrentWorld;
var disposables = new CompositeDisposable();

// Convert ECS messages to Observable
world.Messages<DamageMessage>()
    .ThrottleFirst(TimeSpan.FromMilliseconds(100))
    .ObserveOnMainThread()
    .Subscribe(msg =>
    {
        Debug.Log($"Damage received: {msg.Amount}");
        // Update Health component
        if (world.HasComponent<Health>(msg.Target))
        {
            using var cmd = world.BeginWrite();
            var health = world.ReadComponent<Health>(msg.Target);
            var newHealth = Math.Max(0, health.Current - msg.Amount);
            cmd.ReplaceComponent(msg.Target, new Health(health.Max, newHealth));
        }
    })
    .AddTo(disposables);

// Don't forget to dispose
private void OnDestroy()
{
    disposables?.Dispose();
}
#endif
```

### 2. Publish UniRx Streams as ECS Messages

```csharp
#if ZENECS_UNIRX
using UniRx;
using ZenECS.Adapter.Unity.UniRx;

// Convert Unity Input to Observable
this.UpdateAsObservable()
    .Where(_ => Input.GetKeyDown(KeyCode.Space))
    .Select(_ => new JumpIntent())
    .PublishFrom(world)  // Publish as ECS message
    .AddTo(this);
#endif
```

### 3. Complex Pipeline

```csharp
#if ZENECS_UNIRX
// ECS message → UniRx → Filter → ECS message
world.Messages<HealthChanged>()
    .Where(msg => msg.NewHealth <= 0)
    .Select(msg => new DeathEvent(msg.Entity))
    .PublishFrom(world)
    .AddTo(this);
#endif
```

---

## Key APIs

* **WorldRx.Messages<T>()**: Convert ECS messages to IObservable
* **WorldRx.PublishFrom()**: Publish UniRx streams as ECS messages
* **Conditional Compilation**: `#if ZENECS_UNIRX` required
* **UniRx Operators**: Throttle, Filter, Select, Merge, etc. available

---

## Notes and Best Practices

* UniRx integration is provided via **conditional compilation** and requires `ZENECS_UNIRX` define.
* `WorldRx.Messages<T>()` requires message type `T` to implement `IMessage`.
* UniRx subscriptions must be properly disposed (`AddTo()`, `CompositeDisposable` usage).
* Use `ObserveOnMainThread()` to perform UI updates on the main thread.
* ECS message processing is still performed in FixedGroup, and UniRx is mainly used in View layer.

---

## License

MIT © 2026 Pippapips Limited.
