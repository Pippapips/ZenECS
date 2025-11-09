# ZenECS Core

Minimal, engine‑agnostic **Entity–Component–System (ECS)** runtime focused on clarity, determinism, and zero external dependencies. Built in pure C# with a clean public surface and a replaceable internal architecture (DI‑friendly). Ships with optional Unity UPM packaging and standalone .NET samples.

---

## ✨ Highlights

- **Multi‑World Kernel** — create/destroy worlds, pick current, and step all deterministically.
- **Clean World API** — components, queries, command buffers, messages, hooks, contexts/binders.
- **Zero deps** — no external frameworks; lightweight internal DI and worker queue.
- **Deterministic stepping** — `BeginFrame → FixedUpdate* → Presentation` with explicit ordering attributes.
- **Snapshot I/O** — pluggable backend + `IComponentFormatter` + post‑load migrations.
- **Binding layer** — contexts + binders for view integration (Unity, UI, audio, etc.), kept out of Core data flow.
- **Testable** — small surfaces, sealed scopes, clear lifecycle; samples included.

> **Status:** Release Candidate — APIs locked for 1.0, docs/samples being finalized.

---

## 📦 Packages & Structure

```
Runtime/
  Core/            # primitives, DI, logging, bootstrap
  Kernel/          # IKernel, Kernel, KernelOptions
  World/           # public IWorld* APIs and World implementation
  Systems/         # system contracts + runner/planner + ordering attributes
  Serialization/   # snapshot abstractions + binary formatter + stream backend
  Internal/        # binding router, pools, worker, messaging, hooks
  Binding/         # IBinder/IBinds/IContext/IRequireContext (contracts)
  Events/          # EntityEvents (lifecycle notifications)
Samples~/
  01-Basic, 02-Messages, 03-CommandBuffer, 04-SnapshotIO-PostMig, ...
```

---

## 🚀 Installation

### Unity (UPM)

1. Open **Package Manager** → **Add package from git URL…**
2. Paste: `https://github.com/Pippapips/ZenECS.git#core` (example)
3. Optional samples appear under **Samples** in the package entry.

> For local development, place the repo under your project and reference via `file:` URL or add an entry in `Packages/manifest.json`.

### .NET (non‑Unity)

- Add the Core folder as a project/solution reference, or consume a published NuGet once available.

---

## 🧭 Concepts at a glance

### Kernel

Orchestrates worlds and frame ticks.

```csharp
var kernel = new Kernel();
var world  = kernel.CreateWorld("Game");
kernel.SetCurrentWorld(world);

kernel.BeginFrame(dt: 1f/60f);
kernel.LateFrame();
kernel.Dispose();
```

### World (public façade)

Aggregates APIs: **Entities / Components / Query / CommandBuffer / Messages / Contexts / Hooks / Snapshot / Reset / Worker**.

### Systems

- `IFrameSetupSystem`, `IFixedSetupSystem` — one‑shot prep hooks per frame or fixed phase
- `IVariableRunSystem` — variable timestep (BeginFrame)
- `IFixedRunSystem` — fixed timestep (SimulationGroup)
- `IPresentationSystem` — end of frame (PresentationGroup)

Order via attributes:

```csharp
[SimulationGroup]
[OrderAfter(typeof(PhysicsSystem))]
public sealed class MoveSystem : IFixedRunSystem { /* ... */ }
```

### Components & Pools

Components are `struct` value types stored in type‑segregated pools; presence tracked by dense `BitSet`.

### Queries

Fast, zero‑alloc enumerables seeded by the smallest component pool; fluent filter DSL.

```csharp
var f = Filter.New.With<Position>().Without<Paused>().Build();
foreach (var e in world.Query<Position, Velocity>(f)) {
    ref var p = ref world.Ref<Position>(e);
    var v = world.Get<Velocity>(e);
    p.Value += v.Value * dt;
}
```

### Command Buffer

Buffer structural changes and apply at a safe barrier.

```csharp
using (var cmd = world.BeginWrite(CommandBufferApplyMode.Scheduled)) {
    cmd.AddComponent(e, new Health{ Value = 100 });
    cmd.RemoveComponent<Stunned>(e);
} // scheduled for execution at the worker barrier
```

### Messaging

Struct‑only publish/subscribe with per‑type topics.

```csharp
struct Damage : IMessage { public Entity Target; public int Amount; }
var sub = world.Subscribe<Damage>(d => { /* React */ });
world.Publish(new Damage{ Target = e, Amount = 5 });
```

### Hooks & Validators

World‑scoped permission/validation hooks for read/write and typed value checks.

```csharp
hooks.AddWritePermission((e,t) => t != typeof(GodMode));
hooks.AddValidator<Health>(h => h.Value >= 0);
```

### Binding (view integration)

Contexts carry references/resources; Binders apply deltas to external systems.

```csharp
public sealed class SpriteBinder : BaseBinder, IBinds<Position>, IRequireContext<SpriteCtx>
{
    public void OnDelta(in ComponentDelta<Position> d) { /* move sprite */ }
}
```

### Snapshot I/O

Pluggable backend + formatters + migrations for robust save/load.

```csharp
world.Save(stream, new BinaryComponentFormatter());
world.Load(stream, new BinaryComponentFormatter(), migrations);
```

---

## 🧪 Quick Start (end‑to‑end)

```csharp
// 1) Kernel & world
var kernel = new Kernel();
var world = kernel.CreateWorld("Game");

// 2) Components
public struct Position { public float2 Value; }
public struct Velocity { public float2 Value; }

// 3) A system
[SimulationGroup]
public sealed class MoveSystem : IVariableRunSystem
{
    public void Run(IWorld w, float dt)
    {
        var f = Filter.New.With<Position>().With<Velocity>().Build();
        foreach (var e in w.Query<Position, Velocity>(f))
        {
            ref var p = ref w.Ref<Position>(e);
            var v = w.Get<Velocity>(e);
            p.Value += v.Value * dt;
        }
    }
}

// 4) Entity & loop
var e = world.CreateEntity();
world.AddComponent(e, new Position());
world.AddComponent(e, new Velocity{ Value = new float2(1,0) });

kernel.BeginFrame(1f/60f);
kernel.LateUpdate();
```

---

## 📚 Samples

- **01‑Basic** — hello world, movement
- **02‑Messages** — pub/sub
- **03‑CommandBuffer** — scheduled structural changes
- **04‑SnapshotIO‑PostMig** — persistence with post‑load migrations
- **05‑WorldReset** — teardown & rebuild patterns
- **06‑WriteHooks‑Validators** — permissions and typed validators
- **07‑ComponentChangeFeed** — binder delta flow
- **08‑SystemRunner** — grouping & ordering

> Open the `Samples~` folder in Unity (UPM) or build/run as .NET console projects.

---

## 🔧 Extensibility points

- **Logging** — plug your logger via `EcsRuntimeOptions.Log`.
- **DI/Services** — swap world internals by composing your own `CoreBootstrap` child scope.
- **Snapshot backend** — implement `ISnapshotBackend`.
- **Serialization** — implement `IComponentFormatter` (binary/JSON/custom) and `IPostLoadMigration`.
- **Binding** — provide custom contexts/binders and use the router to validate `IRequireContext<>`.

---

## 🆚 Why ZenECS vs Others?

- **Simplicity** — fewer concepts, explicit lifecycles, readable code.
- **Engine‑agnostic** — Unity optional; Core works in plain .NET.
- **Deterministic frame model** — predictable ordering and barriers.
- **Batteries included** — messaging, snapshots, command buffers, validators.

---

## 🧩 Versioning & Compatibility

- Target frameworks: **.NET Standard 2.1** / **.NET 8** (samples)
- Unity: **2021.3+** recommended
- Semantic Versioning (SemVer). RC builds may adjust internal details without breaking public contracts.

---

## 🗺️ Roadmap

- Docs: deeper guides for Binding and Snapshot pipelines
- NuGet packages for Core & Adapters
- Unity adapter samples (view binders, inspectors)

---

## ⚖️ License

MIT © Pippapips Limited

---

## 🧾 Acknowledgements

Built with love for data‑driven games and tools. Feedback and PRs welcome!

