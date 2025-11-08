using System;
using ZenECS.Core.Binding;

namespace ZenECS.Core
{
    public interface IWorldResetApi
    {
        void Reset(bool keepCapacity);
    }
}
