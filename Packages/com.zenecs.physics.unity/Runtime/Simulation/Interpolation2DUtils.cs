using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation
{
    /// <summary>
    /// 2D 보간 관련 공통 유틸.
    /// - FixedPosition2D 기반으로 Position2D를 스냅하고
    /// - MovementStats2D의 보간 상태를 "완료" 상태로 정리한다.
    /// </summary>
    public static class Interpolation2DUtils
    {
        /// <summary>
        /// FixedPosition2D를 기준으로 Position2D를 즉시 스냅하고,
        /// MovementStats2D의 보간 관련 필드를 "완료" 상태로 만든다.
        /// 
        /// 반환값:
        /// - 업데이트된 MovementStats2D (보간 상태 리셋)
        /// - 스냅된 Position2D (world 좌표)
        /// </summary>
        public static void SnapToFixedAndStopInterpolation(
            in FixedPosition2D fixedPos,
            in MovementStats2D statsIn,
            out MovementStats2D statsOut,
            out Position2D snappedPos)
        {
            statsOut = statsIn;

            // Fixed 좌표를 보간 기준값으로 고정
            statsOut.LastFixedX = fixedPos.x;
            statsOut.LastFixedY = fixedPos.y;
            statsOut.InterpolationAlpha = 1f;

            // Fixed → world 좌표 스냅
            var units = statsOut.UnitsPerUnity <= 0 ? 1000f : statsOut.UnitsPerUnity;
            snappedPos = new Position2D(
                fixedPos.x / units,
                fixedPos.y / units
            );
        }
    }
}