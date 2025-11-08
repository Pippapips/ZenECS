#nullable enable
using System;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.Scheduling;

namespace ZenECS.Core
{
    public interface IWorldWorkerApi
    {
        int RunScheduledJobs();
    }
}
