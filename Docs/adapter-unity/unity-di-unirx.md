# Zenject / UniRx Integration

> Docs / Adapter (Unity) / Zenject / UniRx

Optional integrations with Zenject (dependency injection) and UniRx (reactive programming).

## Overview

ZenECS Unity Adapter supports optional integrations:

- **Zenject**: Dependency injection for systems and contexts
- **UniRx**: Reactive programming with observables

These are **optional** - ZenECS works without them.

## Zenject Integration

### Setup

1. Install Zenject package
2. Define `ZENECS_ZENJECT` (auto-detected)
3. Configure in `ProjectInstaller`

### ProjectInstaller

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Bind system preset resolver
        Container.Bind<ISystemPresetResolver>()
            .To<SystemPresetResolver>()
            .AsSingle();
        
        // Bind shared context resolver
        Container.Bind<ISharedContextResolver>()
            .To<SharedContextResolver>()
            .AsSingle();
    }
}
#endif
```

### System Resolution

Systems can be resolved via DI:

```csharp
#if ZENECS_ZENJECT
// Systems resolved via Zenject
var resolver = Container.Resolve<ISystemPresetResolver>();
var systems = resolver.InstantiateSystems(systemTypes);
world.AddSystems(systems);
#endif
```

### Context Resolution

Contexts resolved via DI:

```csharp
#if ZENECS_ZENJECT
// Shared contexts resolved via Zenject
var resolver = Container.Resolve<ISharedContextResolver>();
var context = resolver.Resolve(sharedContextAsset);
#endif
```

## UniRx Integration

### Setup

1. Install UniRx package
2. Define `ZENECS_UNIRX` (auto-detected)
3. Use extension methods

### Message Observables

Convert message streams to observables:

```csharp
#if ZENECS_UNIRX
using UniRx;
using ZenECS.Adapter.Unity.UniRx;

// Observe messages as IObservable
world.ObserveMessages<DamageMessage>()
    .Where(msg => msg.Amount > 10)
    .Subscribe(msg => Debug.Log($"High damage: {msg.Amount}"));
#endif
```

### Reactive Composition

Compose reactive pipelines:

```csharp
#if ZENECS_UNIRX
// Combine multiple message streams
var damageStream = world.ObserveMessages<DamageMessage>();
var healStream = world.ObserveMessages<HealMessage>();

damageStream
    .Merge(healStream)
    .Throttle(TimeSpan.FromSeconds(1))
    .Subscribe(msg => UpdateHealthBar());
#endif
```

## Combined Usage

Use both Zenject and UniRx together:

```csharp
#if ZENECS_ZENJECT && ZENECS_UNIRX
using Zenject;
using UniRx;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.UniRx;

public class GameBootstrap : MonoBehaviour
{
    [Inject] private ISystemPresetResolver _systemResolver;
    
    private void Start()
    {
        var world = KernelLocator.CurrentWorld;
        
        // Resolve systems via Zenject
        var systems = _systemResolver.InstantiateSystems(systemTypes);
        world.AddSystems(systems);
        
        // Observe messages via UniRx
        world.ObserveMessages<GameEventMessage>()
            .Subscribe(msg => HandleEvent(msg));
    }
}
#endif
```

## Benefits

### Zenject Benefits

- **Dependency Injection**: Testable, modular code
- **System Resolution**: Automatic system instantiation
- **Context Management**: Shared context resolution
- **Lifetime Management**: Automatic cleanup

### UniRx Benefits

- **Reactive Programming**: Event-driven architecture
- **Composition**: LINQ operators for streams
- **Throttling**: Rate limiting and debouncing
- **Combining**: Merge, zip, combine streams

## Best Practices

### ✅ Do

- **Use Zenject for DI**: When you need dependency injection
- **Use UniRx for reactivity**: When you need reactive streams
- **Keep optional**: Don't require these packages
- **Test without**: Ensure code works without integrations

### ❌ Don't

- **Don't require packages**: Make integrations optional
- **Don't mix patterns**: Choose appropriate pattern
- **Don't overuse**: Use when it adds value

## See Also

- [Unity Adapter Overview](./overview.md) - Integration overview
- [Message Bus](../core/message-bus.md) - Message system
- [Binding System](../core/binding.md) - Context binding
