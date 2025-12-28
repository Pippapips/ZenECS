# ZenECS Adapter Unity — Sample 06: FixedStep vs Update

This sample demonstrates how to compare and use Unity's **Update/FixedUpdate** with ZenECS's **BeginFrame/FixedStep/LateFrame** structure.

* **BeginFrame** — Variable timestep (Update)
* **FixedStep** — Fixed timestep (FixedUpdate)
* **LateFrame** — Interpolation and presentation (LateUpdate)
* **EcsDriver** — Automatic integration of Unity lifecycle with ECS frame structure

---

## What This Sample Shows

1. **Frame Structure Understanding**
   Shows how Unity's Update/FixedUpdate/LateUpdate maps to ECS's BeginFrame/FixedStep/LateFrame.

2. **Timestep Difference**
   Visually confirm the difference between variable timestep and fixed timestep.

3. **System Group Separation**
   Shows when FixedGroup and FrameViewGroup execute in each frame phase.

4. **FrameTiming Component**
   Demonstrates tracking frame counts and delta times for both FixedStep and LateFrame.

5. **OnGUI Display**
   Real-time display of Unity and ECS frame timing information for comparison.

---

## TL;DR Flow

```
[Unity]
  Update() → EcsDriver.Update() → Kernel.BeginFrame(deltaTime)
  FixedUpdate() → EcsDriver.FixedUpdate() → Kernel.FixedStep(fixedDeltaTime)
  LateUpdate() → EcsDriver.LateUpdate() → Kernel.LateFrame(alpha)

[ECS]
  BeginFrame (variable timestep)
    ↓
  FixedStep × N (fixed timestep, simulation)
    ↓
  LateFrame (interpolation, presentation)
```

---

## File Structure

```
06-FixedStepUpdate/
├── README.md
├── FixedStepUpdateSample.cs    # Sample script (contains all components and systems)
│   ├── Position component
│   ├── Velocity component
│   ├── FrameTiming component
│   ├── SimulationSystem (FixedGroup)
│   └── PresentationSystem (FrameViewGroup)
└── 06 - FixedStepUpdateSample.unity  # Sample scene
```

---

## Usage

### 1. EcsDriver Automatic Integration

`EcsDriver` automatically converts Unity lifecycle to ECS frame structure:

```csharp
// Inside EcsDriver.cs (automatically executed)
private void Update() => Kernel?.BeginFrame(Time.deltaTime);
private void FixedUpdate() => Kernel?.FixedStep(Time.fixedDeltaTime);
private void LateUpdate() => Kernel?.LateFrame();
```

### 2. Execution Timing by System Group

```csharp
[FixedGroup]
public sealed class SimulationSystem : ISystem
{
    // Runs in FixedStep (fixed timestep)
    public void Run(IWorld w, float dt)
    {
        // dt is fixedDeltaTime (e.g., 0.02f for 50Hz)
        // Execute simulation logic
    }
}

[FrameViewGroup]
public sealed class PresentationSystem : ISystem
{
    // Runs in LateFrame (variable timestep)
    public void Run(IWorld w, float dt)
    {
        // dt is deltaTime (variable)
        // Execute presentation logic (read-only)
    }
}
```

### 3. Manual Frame Control (Optional)

You can also control frames manually without using `EcsDriver`:

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld(null);

// Manual frame control
kernel.BeginFrame(Time.deltaTime);
kernel.FixedStep(Time.fixedDeltaTime);
kernel.LateFrame();
```

---

## Key APIs

* **IKernel.BeginFrame()**: Start variable timestep frame
* **IKernel.FixedStep()**: Fixed timestep simulation step
* **IKernel.LateFrame()**: Interpolation and presentation
* **IKernel.PumpAndLateFrame()**: Convenience method (BeginFrame + FixedStep + LateFrame)
* **EcsDriver**: Automatic Unity lifecycle integration

---

## Notes and Best Practices

* **FixedGroup** systems run in **FixedStep** and use **fixed timestep**.
* **FrameViewGroup** systems run in **LateFrame** and use **variable timestep**.
* Always run simulation logic in **FixedGroup** to ensure deterministic behavior.
* Presentation logic runs in **FrameViewGroup** and should be **read-only**.
* Using `EcsDriver` automatically integrates Unity lifecycle, so manual control is not needed.

---

## License

MIT © 2026 Pippapips Limited.
