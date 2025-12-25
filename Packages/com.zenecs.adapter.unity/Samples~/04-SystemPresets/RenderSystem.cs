using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.SystemPresets
{
    /// <summary>
    /// Rendering system (FrameViewGroup, read-only).
    /// </summary>
    [FrameViewGroup]
    [ZenSystemWatch(typeof(Position))]
    public sealed class RenderSystem : ISystem
    {
        /// <inheritdoc />
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, pos) in w.Query<Position>())
            {
                // In practice, apply to Transform or render
                // Here, only log output
                if (w.FrameCount % 60 == 0) // Output every second
                {
                    Debug.Log($"[RenderSystem] Entity {e.Id}: pos=({pos.X:0.##}, {pos.Y:0.##}, {pos.Z:0.##})");
                }
            }
        }
    }
}