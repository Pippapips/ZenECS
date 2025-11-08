#nullable enable
using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    public interface IWorldCommandBufferApi
    {
        ICommandBuffer BeginWrite(CommandBufferApplyMode mode = CommandBufferApplyMode.Schedule);
        int EndWrite(ICommandBuffer cb);
        void Schedule(ICommandBuffer? cb);
        void ClearAllCommandBuffers();
    }
}
