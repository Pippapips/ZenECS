#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>Internal world instance (implementation hidden). External code should use <see cref="IWorldAPI"/>.</summary>
    public interface IWorldSnapshot
    {
        void SaveFullSnapshotBinary(Stream s);
        void LoadFullSnapshotBinary(Stream s);
    }
}