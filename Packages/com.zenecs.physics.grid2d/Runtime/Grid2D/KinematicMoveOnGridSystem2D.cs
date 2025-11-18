using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Physics.Kinematic2D;

namespace ZenECS.Physics.Grid2D
{
    [OrderAfter(typeof(ApplyMoveInputSystem2D))]
    public sealed class KinematicMoveOnGridSystem2D : IFixedRunSystem
    {
        private static readonly Filter f = new Filter.Builder()
            .With<ZenECS.Physics.Kinematic2D.KinematicBodyTag2D>()
            .Build();
        
        public void Run(IWorld w, float dt)
        {
            var map = w.GetSingleton<MapGrid2D>();

            foreach (var (e, pos, vel, col) in
                     w.Query<
                         ZenECS.Physics.Kinematic2D.Position2D,
                         ZenECS.Physics.Kinematic2D.Velocity2D,
                         ZenECS.Physics.Kinematic2D.CircleCollider2D>(f))
            {
                var newPos = pos;
                KinematicGridMove2D.MoveWithTileCollision(ref newPos, in vel, in col, in map);
                w.ReplaceComponent(e, newPos);
            }
        }
    }
}