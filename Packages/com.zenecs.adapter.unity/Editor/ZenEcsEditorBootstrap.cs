#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor
{
    /// <summary>
    /// Editor-time bootstrapping for ZenECS Unity integration.
    /// Registers editor logger and kernel provider into ZenEcsUnityBridge.
    /// </summary>
    [InitializeOnLoad]
    internal static class ZenEcsEditorBootstrap
    {
        static ZenEcsEditorBootstrap()
        {
        }
    }
}
#endif
