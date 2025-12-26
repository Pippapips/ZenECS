# Troubleshooting

> Docs / Adapter (Unity) / Troubleshooting

Common issues and solutions for ZenECS Unity Adapter.

## Common Issues

### EcsDriver Not Found

**Symptoms:**
- `KernelLocator.Current` throws exception
- No kernel available

**Solutions:**
1. Add `EcsDriver` component to scene
2. Or create kernel manually:
   ```csharp
   var kernel = new Kernel();
   ZenEcsUnityBridge.Kernel = kernel;
   ```

### EntityLink Not Working

**Symptoms:**
- GameObject not linked to entity
- `EntityViewRegistry.TryGetLink()` returns null

**Solutions:**
1. Verify `EntityLink` is attached:
   ```csharp
   var link = gameObject.GetComponent<EntityLink>();
   if (link == null)
       link = gameObject.AddComponent<EntityLink>();
   ```
2. Check link is attached:
   ```csharp
   if (!link.IsAlive)
       link.Attach(world, entity);
   ```

### Blueprint Spawn Fails

**Symptoms:**
- Entity not created
- Components not applied

**Solutions:**
1. Verify world is active:
   ```csharp
   var world = KernelLocator.CurrentWorld;
   if (world == null)
       world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
   ```
2. Check shared context resolver:
   ```csharp
   var resolver = ZenEcsUnityBridge.SharedContextResolver;
   if (resolver == null)
       resolver = new SharedContextResolver();
   ```

### Systems Not Running

**Symptoms:**
- System code not executing
- No entities processed

**Solutions:**
1. Verify system is registered:
   ```csharp
   world.AddSystems([new MySystem()]);
   ```
2. Check system has correct attribute:
   ```csharp
   [FixedGroup]  // or [FrameGroup]
   public class MySystem : ISystem { }
   ```
3. Ensure world is being stepped:
   ```csharp
   // EcsDriver handles this automatically
   // Or manually: kernel.PumpAndLateFrame(...)
   ```

### Components Not Updating

**Symptoms:**
- Component values not changing
- Changes not reflected

**Solutions:**
1. Use command buffer for writes:
   ```csharp
   using var cmd = world.BeginWrite();
   cmd.ReplaceComponent(entity, newComponent);
   ```
2. Check system is running
3. Verify entity is alive:
   ```csharp
   if (world.IsAlive(entity))
   {
       // Entity is valid
   }
   ```

## Performance Issues

### Slow Queries

**Symptoms:**
- Queries are slow
- Frame rate drops

**Solutions:**
1. Filter early in pipeline
2. Cache query results when possible
3. Use appropriate data structures
4. Profile to find bottlenecks

### Memory Leaks

**Symptoms:**
- Memory usage grows
- Entities not cleaned up

**Solutions:**
1. Destroy entities properly:
   ```csharp
   using (var cmd = world.BeginWrite())
   {
       cmd.DestroyEntity(entity);
   }
   ```
2. Unregister binders:
   ```csharp
   world.UnregisterBinder(entity, binder);
   ```
3. Dispose worlds:
   ```csharp
   kernel.Dispose();
   ```

## Integration Issues

### Zenject Not Working

**Symptoms:**
- Systems not resolved
- Contexts not resolved

**Solutions:**
1. Verify Zenject is installed
2. Check `ZENECS_ZENJECT` define is set
3. Configure `ProjectInstaller`:
   ```csharp
   Container.Bind<ISystemPresetResolver>()
       .To<SystemPresetResolver>()
       .AsSingle();
   ```

### UniRx Not Working

**Symptoms:**
- Observable methods not available
- Compilation errors

**Solutions:**
1. Verify UniRx is installed
2. Check `ZENECS_UNIRX` define is set
3. Use conditional compilation:
   ```csharp
   #if ZENECS_UNIRX
   world.ObserveMessages<Message>();
   #endif
   ```

## Debugging Tips

### Enable Logging

```csharp
// Enable debug logging
ZenEcsUnityBridge.EnableLogging = true;
```

### Use ECS Explorer

Open **Window** → **ZenECS** → **Tools** → **ZenECS Explorer** to inspect:
- Systems and execution
- Entities and components
- Messages and binders

### Check Entity State

```csharp
// Verify entity is alive
if (world.IsAlive(entity))
{
    // Check components
    if (world.HasComponent<Position>(entity))
    {
        var pos = world.Get<Position>(entity);
        Debug.Log($"Position: {pos.X}, {pos.Y}");
    }
}
```

## Getting Help

If you can't resolve an issue:

1. **Check Documentation**: Review relevant guides
2. **Search Issues**: Look for similar problems
3. **Create Issue**: Provide detailed information
4. **Ask Community**: Use GitHub Discussions

## See Also

- [FAQ](../overview/faq.md) - Common questions
- [Support](../community/support.md) - Get help
- [ECS Explorer](../tooling/ecs-explorer.md) - Debugging tool
