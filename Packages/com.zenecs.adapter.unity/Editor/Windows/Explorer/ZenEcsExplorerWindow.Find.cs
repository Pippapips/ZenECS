// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerWindow.Find.cs
// Purpose: Find mode implementation for ZenECS Explorer window, providing
//          entity search by ID/Gen and display of watched systems.
// Key concepts:
//   • Entity search: find entity by ID and generation number.
//   • Watched systems: displays systems that watch the found entity.
//   • Partial class: part of ZenEcsExplorerWindow split across multiple files.
//   • Editor-only: compiled out in player builds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
        [Serializable]
        sealed class ExplorerFindState
        {
            public ZenEntityForm.EntityFoldoutInfo? EntityFoldoutInfo;
            
            // Input strings
            public string EntityIdText = string.Empty;
            public string EntityGenText = "0";

            // UI state
            public bool IsFindMode;
            public bool FoundValid;
            public bool WatchedSystemsFold;
            public bool EntityFoldBackup;

            public Entity FoundEntity;

            public void Reset(bool removeTexts = true)
            {
                if (removeTexts)
                {
                    EntityIdText = "";
                    EntityGenText = "0";
                }
                
                EntityFoldoutInfo = null;

                FoundValid = false;
                WatchedSystemsFold = false;
                IsFindMode = false;
            }
        }

        void DrawFindLayout()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_entityPanel.Scroll))
                {
                    _entityPanel.Scroll = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        // Top Close button
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(ZenStringTable.BtnClose, GUILayout.Width(80)))
                            {
                                _findState.Reset(false);
                                return;
                            }
                        }

                        // Display result
                        DrawFindResult();
                    }
                }
            }
        }

        private void DrawFindResult()
        {
            if (_world == null) return;
            
            if (_findState.FoundEntity.IsNone || !_findState.FoundValid)
            {
                EditorGUILayout.HelpBox(ZenStringTable.EntityNotFound, MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField(ZenStringTable.GetFoundEntityTitle(_findState.FoundEntity), ZenGUIStyles.LabelBold14);

            GUILayout.Space(4);

            var systems = _world.GetAllSystems();
            if (systems.Count > 0)
            {
                var watchedList = ZenUtil.CollectWatchedSystemsForEntity(_world, _findState.FoundEntity, systems);
                if (watchedList.Count == 0)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.LabelField(ZenStringTable.NoWatchedSystem, ZenGUIStyles.LabelMLNormal10);
                    }
                }
                else
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        _findState.WatchedSystemsFold = EditorGUILayout.Foldout(
                            _findState.WatchedSystemsFold,
                            ZenStringTable.GetWatchedSystems(watchedList.Count),
                            true,
                            ZenGUIStyles.FoldoutNormal);

                        if (_findState.WatchedSystemsFold)
                        {
                            foreach (var (_, t) in watchedList)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(t.Name, ZenGUIStyles.LabelMLNormal10);
                                    EditorGUILayout.LabelField(t.Namespace, ZenGUIStyles.LabelMLNormal9Gray);
                                    GUILayout.FlexibleSpace();
                                    var line = EditorGUIUtility.singleLineHeight;
                                    var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));
                                    var marginRight = new Rect(r.xMax - 20, r.y, 20, r.height);
                                    if (GUI.Button(marginRight, ZenGUIContents.IconPing(), EditorStyles.iconButton))
                                    {
                                        ZenUtil.PingType(t);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GUILayout.Space(4);

            // Actual Entity Inspect
            //DrawOneEntity(_world, _findState.FoundEntity);
            
            if (_findState.EntityFoldoutInfo == null)
            {
                _findState.EntityFoldoutInfo = new ZenEntityForm.EntityFoldoutInfo(_world, _findState.FoundEntity);
                _findState.EntityFoldoutInfo.ExpandAll();
            }
            
            ZenEntityForm.DrawEntity(_world, _findState.FoundEntity, _coreState.EditMode, ref _findState.EntityFoldoutInfo);
        }

        private void DrawFindMenu()
        {
            if (_world == null) return;
            
            GUILayout.Label(
                text: ZenStringTable.LabelEntityId,
                style: ZenGUIStyles.LabelLCNormal10,
                options: GUILayout.Width(70));

            _findState.EntityIdText = GUILayout.TextField(_findState.EntityIdText, ZenGUIStyles.TextFieldLFNormal10, GUILayout.Width(40));
            _findState.EntityIdText = new string(_findState.EntityIdText.Where(char.IsDigit).ToArray());

            _findState.EntityGenText = GUILayout.TextField(_findState.EntityGenText, ZenGUIStyles.TextFieldLFNormal10, GUILayout.Width(40));
            _findState.EntityGenText = new string(_findState.EntityGenText.Where(char.IsDigit).ToArray());

            if (int.TryParse(_findState.EntityGenText, out int gen))
            {
                if (gen < 0) _findState.EntityGenText = "0";
            }

            // Find => enter single-entity view (no system switching)
            var contentFind = new GUIContent(ZenStringTable.BtnFind, ZenStringTable.TipFind);
            if (GUILayout.Button(contentFind, ZenGUIStyles.ButtonMCNormal10, GUILayout.Width(56)))
            {
                if (int.TryParse(_findState.EntityIdText, out var id) && id > 0)
                {
                    _findState.Reset(false);
                    
                    _findState.FoundValid = _world.IsAlive(id, gen);
                    _findState.FoundEntity =
                        _findState.FoundValid ? (Entity)Activator.CreateInstance(typeof(Entity), id, gen) : default;

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
                }
                else
                {
                    _findState.Reset(false);
                    _findState.IsFindMode = true; // still enter to show guidance
                }

                Repaint();
            }

            // Clear Filter => exit single-entity view
            var contentClear = new GUIContent(ZenStringTable.BtnClear, ZenStringTable.TipClear);
            if (GUILayout.Button(contentClear, ZenGUIStyles.ButtonMCNormal10, GUILayout.Width(60)))
            {
                if (_findState.IsFindMode)
                {
                    _entityPanel.EntityFold[_findState.FoundEntity] = _findState.EntityFoldBackup;
                }

                _findState.Reset();
                Repaint();
            }

            _coreState.EditMode = GUILayout.Toggle(_coreState.EditMode,
                ZenStringTable.BtnEdit,
                ZenGUIStyles.ButtonMCNormal10,
                GUILayout.Width(60));
        }
    }
}
