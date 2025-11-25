#nullable enable
using System;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    /// <summary>
    /// Utility helpers for resolving system metadata such as execution groups.
    /// </summary>
    public static class SystemUtil
    {
        /// <summary>
        /// Resolve the execution <see cref="SystemGroup"/> for a system type.
        /// <para>
        /// 우선순위:
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       타입에 직접 선언된 <see cref="SimulationGroupAttribute"/> (또는 파생형:
        ///       <see cref="FixedInputGroupAttribute"/>,
        ///       <see cref="FixedDecisionGroupAttribute"/>,
        ///       <see cref="FixedSimulationGroupAttribute"/>,
        ///       <see cref="FixedPostGroupAttribute"/>,
        ///       <see cref="FrameInputGroupAttribute"/>,
        ///       <see cref="FrameSyncGroupAttribute"/>,
        ///       <see cref="FrameUIGroupAttribute"/>,
        ///       <see cref="FrameViewGroupAttribute"/> 등)
        ///       을 가장 먼저 사용합니다.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       어트리뷰트가 없는 경우, 마커 인터페이스를 통해 그룹을 추론합니다:
        ///       <see cref="IPresentationSystem"/>,
        ///       <see cref="IFixedSetupSystem"/>,
        ///       <see cref="IFixedRunSystem"/>,
        ///       <see cref="IFrameSetupSystem"/>,
        ///       <see cref="IFrameRunSystem"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       어떤 조건에도 해당하지 않으면
        ///       <see cref="SystemGroup.FixedSimulation"/> 을 기본값으로 사용합니다.
        ///     </description>
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// 그룹/마커 조합의 정합성 검사는 <c>SystemPlanner.ValidatePhaseMarkers</c> 에서 별도로 수행됩니다.
        /// </para>
        /// </summary>
        /// <param name="t">System type.</param>
        /// <returns>The resolved <see cref="SystemGroup"/>.</returns>
        public static SystemGroup ResolveGroup(Type t)
        {
            // 1) 명시적인 SimulationGroupAttribute(또는 파생형)가 있으면 그것을 우선 사용
            //    (SetupGroupAttribute, FixedInputGroupAttribute, FrameInputGroupAttribute 등 포함)
            if (Attribute.GetCustomAttribute(t, typeof(SimulationGroupAttribute), inherit: false)
                is SimulationGroupAttribute sgAttr)
            {
                return sgAttr.Group;
            }
            
            return SystemGroup.Unknown;
        }
    }
}
