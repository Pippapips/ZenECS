#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Binding;
using ZenECS.Core.DI;
using ZenECS.Core.Internal;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Internal.Bootstrap;
using ZenECS.Core.Internal.ComponentPooling;
using ZenECS.Core.Internal.Contexts;
using ZenECS.Core.Internal.Hooking;
using ZenECS.Core.Internal.Scheduling;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldWorkerApi
    {
        public int RunScheduledJobs() => _worker.RunScheduledJobs(this);
    }
}