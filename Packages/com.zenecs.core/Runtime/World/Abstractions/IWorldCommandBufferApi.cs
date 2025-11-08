#nullable enable
namespace ZenECS.Core
{
    /// <summary>
    /// Controls how a <see cref="CommandBuffer"/> is applied when disposed or explicitly flushed.
    /// </summary>
    public enum CommandBufferApplyMode
    {
        /// <summary>
        /// Queue this buffer to be applied at the next frame barrier.
        /// Use for background threads or when you want deterministic, batched commits.
        /// </summary>
        Schedule = 0,

        /// <summary>
        /// Apply this buffer immediately on dispose (or when explicitly ended).
        /// Recommended from the main thread only, to minimize contention.
        /// </summary>
        Immediate = 1,
    }
    
    public interface ICommandBuffer : System.IDisposable
    {
        void AddComponent<T>(Entity e, in T v) where T : struct;
        void ReplaceComponent<T>(Entity e, in T v) where T : struct;
        void RemoveComponent<T>(Entity e) where T : struct;
        void DespawnEntity(Entity e);
    }
    
    public interface IWorldCommandBufferApi
    {
        ICommandBuffer BeginWrite(CommandBufferApplyMode mode = CommandBufferApplyMode.Schedule);
        int EndWrite(ICommandBuffer cb);
        void Schedule(ICommandBuffer? cb);
        void ClearAllCommandBuffers();
    }
}
