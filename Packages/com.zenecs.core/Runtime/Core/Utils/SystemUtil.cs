using System;
using ZenECS.Core.Systems;

namespace ZenECS.Core
{
    public static class SystemUtil
    {
        /// <summary>
        /// Resolve the execution group for a system type.
        /// </summary>
        /// <param name="t">System type.</param>
        /// <returns>The resolved <see cref="SystemGroup"/>.</returns>
        public static SystemGroup ResolveGroup(Type t)
        {
            if (t.IsDefined(typeof(FrameSetupGroupAttribute), false))   return SystemGroup.FrameSetup;
            if (t.IsDefined(typeof(PresentationGroupAttribute), false)) return SystemGroup.Presentation;
            if (t.IsDefined(typeof(SimulationGroupAttribute), false))   return SystemGroup.Simulation;

            if (typeof(IPresentationSystem).IsAssignableFrom(t)) return SystemGroup.Presentation;
            if (typeof(IFixedSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFrameSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFixedRunSystem).IsAssignableFrom(t))     return SystemGroup.Simulation;
            if (typeof(IVariableRunSystem).IsAssignableFrom(t))  return SystemGroup.Simulation;

            return SystemGroup.Simulation;
        }
    }
}