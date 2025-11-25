namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Which phase a write belongs to.
    /// </summary>
    internal enum WorldWritePhase
    {
        None = 0,
        Simulation = 1,      // Fixed* 계열
        FrameInput = 2,
        FrameSync = 3,
        FrameView = 4,
        FrameUI = 5,
    }

    /// <summary>
    /// Per-world write policy: phase + structural/value write switches.
    /// Phase-agnostic PermissionHook과 별개로 동작하는 coarse-grained 레이어.
    /// </summary>
    internal sealed class WorldWritePolicy
    {
        public WorldWritePhase CurrentPhase { get; private set; } = WorldWritePhase.None;

        /// <summary>
        /// If true, 모든 write (structural + value) 금지.
        /// </summary>
        public bool DenyAllWrites { get; private set; }

        /// <summary>
        /// Add/Remove/Spawn/Despawn 같은 structural 변경 허용 여부.
        /// </summary>
        public bool StructuralChangesAllowed { get; private set; }

        public void Set(WorldWritePhase phase, bool denyAllWrites, bool structuralChangesAllowed)
        {
            CurrentPhase = phase;
            DenyAllWrites = denyAllWrites;
            StructuralChangesAllowed = structuralChangesAllowed;
        }

        public void Clear()
        {
            CurrentPhase = WorldWritePhase.None;
            DenyAllWrites = false;
            StructuralChangesAllowed = true;
        }

        public bool CanValueWrite()
        {
            return !DenyAllWrites;
        }

        public bool CanStructuralWrite()
        {
            return !DenyAllWrites && StructuralChangesAllowed;
        }
    }
}