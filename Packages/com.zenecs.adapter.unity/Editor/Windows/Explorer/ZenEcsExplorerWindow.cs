// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerWindow.cs
// Purpose: Main EditorWindow for the ZenECS Explorer tool that provides
//          inspection and editing capabilities for ECS systems and entities.
// Key concepts:
//   • Core lifecycle: window initialization, state management, repaint scheduling.
//   • Partial class: split across multiple files (Core, Header, Main, Footer, etc.).
//   • State management: consolidated ExplorerState for system tree, entity panel, find.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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
        public static ZenEcsExplorerWindow Open()
        {
            var window = GetWindow<ZenEcsExplorerWindow>("ZenECS Explorer");
            return window;
        }

        [Serializable]
        sealed class ExplorerCoreState
        {
            /// <summary>
            /// Global edit mode flag. When false, UI is read-only.
            /// </summary>
            public bool EditMode = true;
        }

        /// <summary>
        /// Consolidated state management for the Explorer window.
        /// </summary>
        [Serializable]
        sealed class ExplorerState
        {
            public ExplorerSystemTreeState SystemTree = new();
            public ExplorerEntityPanelState EntityPanel = new();
            public ExplorerFindState Find = new();

            /// <summary>
            /// Clears all state components.
            /// </summary>
            /// <param name="resetFindTexts">Whether to reset Find state text fields.</param>
            public void Clear(bool resetFindTexts = true)
            {
                SystemTree.ClearSelection();
                SystemTree.ClearTreeFoldouts();
                SystemTree.Scroll = Vector2.zero;

                EntityPanel.ClearSelection();
                EntityPanel.Scroll = Vector2.zero;

                Find.Reset(resetFindTexts);
            }

            /// <summary>
            /// Clears only the system tree state.
            /// </summary>
            public void ClearSystemTree()
            {
                SystemTree.ClearSelection();
                SystemTree.ClearTreeFoldouts();
                SystemTree.Scroll = Vector2.zero;
            }

            /// <summary>
            /// Clears only the entity panel state.
            /// </summary>
            public void ClearEntityPanel()
            {
                EntityPanel.ClearSelection();
                EntityPanel.Scroll = Vector2.zero;
            }

            /// <summary>
            /// Clears only the find state.
            /// </summary>
            /// <param name="resetTexts">Whether to reset text fields.</param>
            public void ClearFind(bool resetTexts = true)
            {
                Find.Reset(resetTexts);
            }
        }

        readonly ExplorerCoreState _coreState = new();
        
        // Consolidated state management - access via _state property
        private readonly ExplorerState _state = new();
        
        // Legacy field accessors for backward compatibility
        // These delegate to _state for unified state management
        private ExplorerSystemTreeState _systemTree => _state.SystemTree;
        private ExplorerEntityPanelState _entityPanel => _state.EntityPanel;
        private ExplorerFindState _findState => _state.Find;

        private double _nextRepaint;
        private const float _repaintInterval = 0.25f;
        private IKernel? _kernel;
        private IWorld? _world;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Clears all window state.
        /// </summary>
        /// <param name="repaint">Whether to trigger a repaint after clearing.</param>
        /// <param name="resetFindStateRemoveTexts">Whether to reset Find state text fields.</param>
        void ClearState(bool repaint = true, bool resetFindStateRemoveTexts = true)
        {
            _state.Clear(resetFindStateRemoveTexts);

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
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
                {
                    GUILayout.Label(ZenStringTable.ZenECSKernelNotActiveYet, ZenGUIStyles.TitleStyle);
                    GUILayout.Space(8);
                    GUILayout.Label(ZenStringTable.ZenECSKernelNotActiveYetDesc, ZenGUIStyles.BodyStyle);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        void DrawNoCurrentWorldOverlay()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
                {
                    GUILayout.Label(ZenStringTable.ZenECSNoCurrentWorld, ZenGUIStyles.TitleStyle);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        // =====================================================================
        //  BRIDGE ENTRY: SelectEntity (called from external)
        // =====================================================================

        /// <summary>
        /// Called from ZenEcsExplorerBridge to open the Explorer and focus a specific entity.
        /// </summary>
        public void SelectEntity(IWorld world, int entityId, int entityGen)
        {
            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null) return;

            // Check with the currently selected World in Explorer.
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