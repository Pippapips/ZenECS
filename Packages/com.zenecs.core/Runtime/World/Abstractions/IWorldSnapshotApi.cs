#nullable enable
using System.IO;

namespace ZenECS.Core
{
    public interface IWorldSnapshotApi
    {
        void SaveFullSnapshotBinary(Stream s);
        void LoadFullSnapshotBinary(Stream s);
    }
}