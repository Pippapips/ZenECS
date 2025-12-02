#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using ZenECS.Core.Systems.Internal;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsDebugWindow : EditorWindow
    {
        // =====================================================================
        //  MENU
        // =====================================================================

        [MenuItem("ZenECS/Tools/ZenECS Debug")]
        public static void Open()
        {
            GetWindow<ZenEcsDebugWindow>("ZenECS Debug");
        }

        private double _nextRepaint;
        private const float _repaintInterval = 0.25f;
        private IKernel? _kernel;
        private IWorld? _world;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================

        void ClearState(bool repaint = true)
        {
            if (repaint)
                Repaint();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;

            ClearState();

            _nextRepaint = EditorApplication.timeSinceStartup + _repaintInterval;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeReload;
        }

        void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup > _nextRepaint)
            {
                _nextRepaint = EditorApplication.timeSinceStartup + _repaintInterval;
                Repaint();
            }
        }

        void OnBeforeReload()
        {
            ClearState();
        }

        void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                ClearState();
            }
        }

        void OnGUI()
        {
            _kernel = ZenEcsUnityBridge.Kernel;
            if (_kernel == null) { return; }
            _world = _kernel.CurrentWorld;
            if (_world == null) { return; }
            
            DrawRightDebug();
        }
    }
}
#endif