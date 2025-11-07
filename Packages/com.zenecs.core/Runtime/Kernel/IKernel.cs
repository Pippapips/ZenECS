#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core
{
    /// <summary>
    /// Kernel lifecycle and multi-world management interface.
    /// Adapters (Unity/Godot/…) create a Kernel, Start it, create Worlds, and drive Tick().
    /// External code should not mutate world state via Kernel internals; use IWorldAPI instead.
    /// </summary>
    public interface IKernel : IDisposable
    {
        /// <summary>Whether the kernel has been started and not yet shut down.</summary>
        bool IsRunning { get; }
        bool IsPaused { get; }

        /// <summary>Total uptime (seconds) accumulated while running.</summary>
        float SimulationAccumulatorSeconds { get; }
        float TotalTimeSeconds { get; }

        long FrameCount { get; }
        long FixedFrameCount { get; }
        int FixedFrameIndexInFrame { get; }
        
        /// <summary>커널이 실제로 '고정 스텝'을 실행한 누적 시뮬레이션 시간(초). 
        /// 프레임 누적 경과 시간이 아니라, 고정 스텝으로 처리된 총합만 집계합니다.</summary>
        double TotalSimulatedSeconds { get; }

        /// <summary>The currently selected world, if any.</summary>
        IWorld? CurrentWorld { get; }

        /// <summary>Behavioral options for world creation and ticking.</summary>
        KernelOptions? Options { get; }

        /// <summary>Raised when a world is created and registered.</summary>
        event Action<IWorld>? WorldCreated;

        /// <summary>Raised after a world has been destroyed and deregistered.</summary>
        event Action<IWorld>? WorldDestroyed;

        /// <summary>Raised when <see cref="CurrentWorld"/> changes (may be null).</summary>
        event Action<IWorld?>? CurrentWorldChanged;

        /// <summary>
        /// Create a new world.
        /// If <paramref name="setAsCurrent"/> is true, the kernel also sets it as the CurrentWorld.
        /// </summary>
        /// <param name="name">Optional display name; auto-generated when omitted.</param>
        /// <param name="tags">Optional tags for discovery/grouping.</param>
        /// <param name="presetId">Optional preassigned id; otherwise generated via <see cref="KernelOptions.NewWorldId"/>.</param>
        /// <param name="setAsCurrent">If true, select the created world as current immediately.</param>
        /// <returns>The created world instance.</returns>
        IWorld CreateWorld(WorldConfig? cfg = null, string? name = null, IEnumerable<string>? tags = null, WorldId? presetId = null,
            bool setAsCurrent = false);

        /// <summary>Destroy a previously created world. No-op if not registered.</summary>
        void DestroyWorld(IWorld world);

        /// <summary>Try to get a live world by id.</summary>
        bool TryGet(WorldId id, out IWorld world);

        /// <summary>Find worlds by exact name (multiple worlds may share a name).</summary>
        IEnumerable<IWorld> FindByName(string name);

        /// <summary>Find worlds which contain the given tag.</summary>
        IEnumerable<IWorld> FindByTag(string tag);

        /// <summary>여러 태그 중 하나라도 일치하는 월드들을 반환.</summary>
        IEnumerable<IWorld> FindByAnyTag(params string[] tags);

        /// <summary>이름 prefix로 월드 검색(대소문자 무시 권장).</summary>
        IEnumerable<IWorld> FindByNamePrefix(string prefix);
        
        void SetCurrentWorld(IWorld world);
        
        /// <summary>Set the current world using a safe handle (resolves or throws).</summary>
        void SetCurrentWorld(WorldHandle handle);

        /// <summary>Clear the current world selection (no current world).</summary>
        void ClearCurrentWorld();

        void BeginFrame(float dt);
        void FixedStep(float fixedDelta);
        void LateFrame(float alpha = 1.0f);
        
        event Action<IWorld, float>? OnBeginFrame;
        event Action<IWorld, float>? OnFixedStep;
        event Action<IWorld, float, float>? OnLateFrame;
        
        int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps);
        
        void Pause();
        void Resume();
        void TogglePause();
    }
}
