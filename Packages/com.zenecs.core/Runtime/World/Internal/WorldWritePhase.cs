#nullable enable
namespace ZenECS.Core.Internal
{
    internal sealed partial class World
    {
        private readonly WorldWritePolicy _writePolicy = new WorldWritePolicy();

        internal WorldWritePhase CurrentWritePhase => _writePolicy.CurrentPhase;

        internal void SetWritePhase(
            WorldWritePhase phase,
            bool denyAllWrites,
            bool structuralChangesAllowed)
        {
            _writePolicy.Set(phase, denyAllWrites, structuralChangesAllowed);
        }

        internal void ClearWritePhase()
        {
            _writePolicy.Clear();
        }
    }
}