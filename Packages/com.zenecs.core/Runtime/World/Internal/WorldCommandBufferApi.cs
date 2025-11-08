#nullable enable
using ZenECS.Core.Internal.Scheduling;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldCommandBufferApi
    {
        /// <summary>
        /// Begins a command-buffer write scope.
        /// </summary>
        /// <param name="mode">
        /// Application mode that determines what happens on <see cref="CommandBuffer.Dispose"/>:
        /// <see cref="CommandBufferApplyMode.Schedule"/> (default) queues the buffer to apply at the frame barrier, while
        /// <see cref="CommandBufferApplyMode.Immediate"/> applies instantly.
        /// </param>
        /// <returns>A new <see cref="CommandBuffer"/> bound to this world.</returns>
        /// <remarks>
        /// Supports the <c>using</c> pattern:
        /// <code>
        /// using (var cb = world.BeginWrite()) { /* enqueue ops */ }                    // Applies on Dispose via Schedule
        /// using (var cb = world.BeginWrite(ApplyMode.Immediate)) { /* enqueue ops */ } // Applies on Dispose immediately
        /// </code>
        /// </remarks>
        public ICommandBuffer BeginWrite(CommandBufferApplyMode mode = CommandBufferApplyMode.Schedule)
        {
            var cb = new CommandBuffer();
            cb.Bind(this, mode);
            return cb;
        }

        /// <summary>
        /// Applies all queued operations in the specified <paramref name="cb"/> immediately.
        /// </summary>
        /// <param name="cb">The command buffer to flush. If <see langword="null"/>, no work is performed.</param>
        /// <returns>The number of operations applied.</returns>
        /// <remarks>
        /// This method is typically called on the main thread. For deferred application,
        /// see <see cref="Schedule(CommandBuffer?)"/>.
        /// </remarks>
        public int EndWrite(ICommandBuffer icb)
        {
            var cb = (CommandBuffer)icb;
            if (cb == null) return 0;
            int n = 0;
            while (cb.Q.TryDequeue(out var op))
            {
                op.Apply(this);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Enqueues the given <paramref name="cb"/> to be executed at the next frame barrier via the world's scheduler.
        /// </summary>
        /// <param name="cb">The command buffer to schedule. If <see langword="null"/>, the call is ignored.</param>
        public void Schedule(ICommandBuffer? cb)
        {
            if (cb != null)
            {
                _worker.Schedule((IJob)cb);
            }
        }

        /// <summary>
        /// Clears any frame-local/deferred command buffers by flushing the world's scheduled jobs queue.
        /// </summary>
        public void ClearAllCommandBuffers()
        {
            _worker.ClearAllScheduledJobs();
        }
        
        /// <summary>
        /// Hook executed before <see cref="WorldOld.Reset(bool)"/>. When capacity will be rebuilt,
        /// this flushes scheduled jobs to avoid dropping queued operations.
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to keep current capacities; <see langword="false"/> to rebuild.
        /// </param>
        partial void OnBeforeWorldReset(bool keepCapacity)
        {
            if (!keepCapacity) _worker.RunScheduledJobs(this);
        }
    }
}