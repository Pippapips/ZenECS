#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// ViewModel for the right entity/singleton panel.
        /// </summary>
        [Serializable]
        sealed class ExplorerEntityPanelState
        {
            // 스크롤
            public Vector2 Scroll;

            public Dictionary<Entity, ZenEntityForm.EntityFoldoutInfo> EntityFoldoutInfos = new();
            
            // Foldouts per entity / context
            public readonly Dictionary<Entity, bool> EntityFold = new();

            // Watched foldouts (key: system name or other key)
            public readonly Dictionary<string, bool> WatchedFold = new();

            // Singleton selection
            public bool HasSelectedSingleton;
            public Entity SelectedSingletonEntity;
            public Type? SelectedSingletonType;

            /// <summary>
            /// Clear all entity-related foldouts.
            /// </summary>
            public void ClearEntityView()
            {
                EntityFoldoutInfos.Clear();
                EntityFold.Clear();
                WatchedFold.Clear();
            }

            /// <summary>
            /// Clear singleton selection.
            /// </summary>
            public void ClearSingletonSelection()
            {
                HasSelectedSingleton = false;
                SelectedSingletonEntity = default;
                SelectedSingletonType = null;
            }
            
            /// <summary>
            /// Clear selection for the entity panel (entities + singleton).
            /// </summary>
            public void ClearSelection()
            {
                ClearSingletonSelection();
                ClearEntityView();
            }
        }
        readonly ExplorerEntityPanelState _entityPanel = new();
        
        private void DrawRightEntityPanel()
        {
            if (_world == null) return;
            
            using var sv = new EditorGUILayout.ScrollViewScope(_entityPanel.Scroll);
            _entityPanel.Scroll = sv.scrollPosition;

            EditorGUILayout.Space(4);

            var systems = _world.GetAllSystems();
            bool hasSystem = systems.Count > 0 &&
                             _systemTree.SelectedSystemIndex >= 0 &&
                             _systemTree.SelectedSystemIndex < (systems?.Count ?? 0);

            bool hasSingleton = false;
            if (_entityPanel.HasSelectedSingleton)
            {
                hasSingleton = _world.IsAlive(_entityPanel.SelectedSingletonEntity);
                if (!hasSingleton)
                {
                    _entityPanel.ClearSingletonSelection();
                }
            }
            
            if (!hasSystem && !hasSingleton)
            {
                // 시스템/싱글톤 선택 없음 → 안내 메시지만
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.HelpBox(
                        "Select a system or singleton from the left panel.\n" +
                        "The System Meta or Singleton Entity will be shown here.",
                        MessageType.Info);

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
            else if (hasSingleton && !hasSystem)
            {
                var e = _entityPanel.SelectedSingletonEntity;
                if (_world.IsAlive(e))
                {
                    if (!_entityPanel.EntityFoldoutInfos.TryGetValue(e, out var foldoutInfo))
                    {
                        foldoutInfo = new ZenEntityForm.EntityFoldoutInfo(_world, e, true);
                        _entityPanel.EntityFoldoutInfos.Add(e, foldoutInfo);
                    }

                    ZenEntityForm.DrawEntity(_world, e, _coreState.EditMode, ref foldoutInfo);
                }
            }
            else
            {
                // =========================
                // 정상 리스트 모드 (시스템 선택 있음)
                // =========================

                var sys = systems![_systemTree.SelectedSystemIndex];

                ZenSystemMetaForm.DrawSystemMeta(sys);

                EditorGUILayout.Space(6);

                bool noEntities = false;
                var tmp = new List<Entity>();
                if (!ZenUtil.TryCollectEntitiesBySystemWatched(_world, sys, tmp))
                {
                    noEntities = true;
                }

                _systemTree.SelectedSystemEntityCount = tmp.Count;

                // 🔹 2) Entities 헤더 + 리스트
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_systemTree.SelectedSystemEntityCount > 0)
                        EditorGUILayout.LabelField($"Entities ({_systemTree.SelectedSystemEntityCount})", EditorStyles.boldLabel);
                    else
                        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);

                if (noEntities)
                {
                    EditorGUILayout.HelpBox(
                        "No inspector. add [Watch].",
                        MessageType.Info);
                    return;
                }
                
                foreach (var e in tmp.Distinct())
                    if (_world != null)
                    {
                        if (_world.IsAlive(e))
                        {
                            if (!_entityPanel.EntityFoldoutInfos.TryGetValue(e, out var foldoutInfo))
                            {
                                foldoutInfo = new ZenEntityForm.EntityFoldoutInfo(_world, e);
                                _entityPanel.EntityFoldoutInfos.Add(e, foldoutInfo);
                            }

                            ZenEntityForm.DrawEntity(_world, e, _coreState.EditMode, ref foldoutInfo);
                        }
                    }
            }
        }
    }
}