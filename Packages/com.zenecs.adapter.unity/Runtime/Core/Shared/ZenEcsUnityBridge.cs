#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Install;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Global bridge between ZenECS core, Unity runtime, and Unity editor tooling.
    /// This class must remain safe to use in player builds (no UnityEditor references).
    /// </summary>
    public static class ZenEcsUnityBridge
    {
        public static ISystemPresetResolver? SystemPresetResolver { get; set; }
        
        /// <summary>
        /// Global shared context resolver used by both runtime and editor tools.
        /// Editor / runtime can override this during bootstrap.
        /// </summary>
        public static ISharedContextResolver? SharedContextResolver { get; set; }

        /// <summary>
        /// access to the current kernel, if any.
        /// Editor and runtime can plug their own provider.
        /// </summary>
        public static IKernel? Kernel { get; set; }
    }
}