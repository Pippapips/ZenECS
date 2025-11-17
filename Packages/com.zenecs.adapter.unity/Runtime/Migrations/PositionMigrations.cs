using Unity.Mathematics;
using ZenECS.Core.Serialization;
using ZenECS.Adapter.Unity.Components.Common; // Position 타입 네임스페이스
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Migrations
{
    public sealed class PositionFillIntValueIfMissing : IPostLoadMigration
    {
        public int Order => 100; // 필요 시 다른 마이그보다 늦게/빨리 조절
        public void Run(IWorld world)
        {
            foreach (var (e, p) in world.Query<Position>())
            {
            }
        }
    }
}