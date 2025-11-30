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
    /// <summary>
    /// Core lifecycle, layout entry points, toolbar and shared helpers
    /// for the ZenECS Explorer window.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow : EditorWindow
    {
        // =====================================================================
        //  MENU
        // =====================================================================

        [MenuItem("ZenECS/Tools/ZenECS Explorer")]
        public static void Open()
        {
            GetWindow<ZenEcsExplorerWindow>("ZenECS Explorer");
        }

        [Serializable]
        sealed class ExplorerCoreState
        {
            /// <summary>
            /// Global edit mode flag. When false, UI is read-only.
            /// </summary>
            public bool EditMode = true;
        }

        readonly ExplorerCoreState _coreState = new();

        private double _nextRepaint;
        private const float _repaintInterval = 0.25f;
        private IKernel? _kernel;
        private IWorld? _world;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================

        void ClearState(bool repaint = true)
        {
            // 좌측 트리 / 우측 엔티티 패널 상태 리셋
            _systemTree.ClearSelection();
            _systemTree.ClearTreeFoldouts();
            _systemTree.Scroll = Vector2.zero;

            _entityPanel.ClearSelection();
            _entityPanel.Scroll = Vector2.zero;

            // Find 모드 리셋
            _findState.Reset();

            // EditMode는 사용자가 토글하는 값이니 강제 초기화는 안 함
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
            if (_kernel == null) { DrawKernelNotReadyOverlay(); return; }
            _world = _kernel.CurrentWorld;
            if (_world == null) { DrawNoCurrentWorldOverlay(); return; }

            DrawHeader();
            if (_findState.IsFindMode) DrawFindLayout();
            else DrawMainLayout();
            DrawFooter();
        }

        // =====================================================================
        //  HELPER: KERNEL NOT READY OVERLAY
        // =====================================================================

        void DrawKernelNotReadyOverlay()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = EditorStyles.boldLabel.fontSize + 2,
                wordWrap = true
            };
            var bodyStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
                {
                    GUILayout.Label("ZenECS Kernel is not active yet.", titleStyle);
                    GUILayout.Space(8);
                    GUILayout.Label(
                        "Enter Play Mode to initialize the EcsDriver and Kernel.\n\n" +
                        "When the Kernel becomes active, you can inspect Systems and Entities\n" +
                        "through the ZenECS Explorer.",
                        bodyStyle);
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        void DrawNoCurrentWorldOverlay()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = EditorStyles.boldLabel.fontSize + 2,
                wordWrap = true
            };
            var bodyStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
                {
                    GUILayout.Label("ZenECS No current world set in kernel.", titleStyle);
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        // =====================================================================
        //  BRIDGE ENTRY: SelectEntity (외부에서 호출)
        // =====================================================================

        /// <summary>
        /// Called from ZenEcsExplorerBridge to open the Explorer and focus a specific entity.
        /// </summary>
        public void SelectEntity(IWorld world, int entityId, int entityGen)
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null) return;

            // Explorer에서 현재 선택된 World로 검사한다.
            var currentWorld = kernel.CurrentWorld;
            if (currentWorld == null || currentWorld.Id != world.Id)
            {
                kernel.SetCurrentWorld(world);
                currentWorld = world;
            }

            _findState.FoundValid = currentWorld?.IsAlive(entityId, entityGen) ?? false;
            if (_findState.FoundValid)
            {
                _findState.EntityIdText = entityId.ToString();
                _findState.EntityGenText = entityGen.ToString();
                _findState.FoundEntity = _findState.FoundValid
                    ? (Entity)Activator.CreateInstance(typeof(Entity), entityId, entityGen)
                    : default;

                if (_findState.FoundValid)
                {
                    if (_entityPanel.EntityFold.TryGetValue(_findState.FoundEntity, out var fold))
                    {
                        _findState.EntityFoldBackup = fold;
                        _entityPanel.EntityFold[_findState.FoundEntity] = true;
                    }
                    else
                    {
                        _findState.EntityFoldBackup = false;
                        _entityPanel.EntityFold.TryAdd(_findState.FoundEntity, true);
                    }
                }

                _findState.IsFindMode = true;
                Repaint();
                return;
            }

            _findState.FoundValid = false;
            _findState.WatchedSystemsFold = false;
            _findState.IsFindMode = true;
        }
    }
}
#endif