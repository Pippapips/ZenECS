#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;
using ZenECS.EditorCommands;

namespace ZenECS.EditorRoot
{
    [InitializeOnLoad]
    internal static class ZenEcsEditor
    {
        public static ExternalEditorCommandQueue CommandQueue => _externalEditorCommandQueue;
        private static ExternalEditorCommandQueue _externalEditorCommandQueue;
            
        static ZenEcsEditor()
        {
            _externalEditorCommandQueue = new ExternalEditorCommandQueue();
            
            ZenEcsUnityBridge.KernelCreated += kernel =>
            {
                kernel.CurrentWorldChanged += (prev, next) =>
                {
                    if (prev != null)
                    {
                        _externalEditorCommandQueue.Clear();
                    }
                    
                    if (next != null)
                    {
                        next.EnteredDeterministic += tick =>
                        {
                            _externalEditorCommandQueue.FlushTo(next);
                        };
                    }
                };
            };
        }
    }
}
#endif
