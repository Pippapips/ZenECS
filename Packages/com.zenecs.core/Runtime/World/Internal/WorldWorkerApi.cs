#nullable enable
namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldWorkerApi
    {
        public int RunScheduledJobs() => _worker.RunScheduledJobs(this);
    }
}