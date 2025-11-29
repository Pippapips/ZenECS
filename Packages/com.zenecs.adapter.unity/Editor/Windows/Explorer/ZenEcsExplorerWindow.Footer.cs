#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.EditorWindows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        private void DrawFooter(IKernel? kernel)
        {
            if (kernel?.CurrentWorld == null) return;
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // ===== Global Pause 토글 버튼 (커널 기반, System Pause와 동일 스타일) =====
                var isPaused = kernel.IsPaused;

                // 툴바 라인 높이에 맞춘 Rect
                var rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                var pauseRect = GUILayoutUtility.GetRect(24f, rowHeight, GUILayout.Width(24f));

                using (new EditorGUI.DisabledScope(false))
                {
                    // System 리스트에서 쓰는 것과 동일한 버튼 영역 보정
                    var btnRect = new Rect(
                        pauseRect.x,
                        pauseRect.y + 1f,
                        pauseRect.width,
                        pauseRect.height - 2f
                    );

                    // Unity 기본 Pause 아이콘
                    var pauseContent = EditorGUIUtility.IconContent("PauseButton");
                    if (pauseContent == null || pauseContent.image == null)
                        pauseContent = EditorGUIUtility.TrTextContent("⏸");

                    // 항상 같은 스타일을 사용해서 크기/모양 변화 없게
                    var pauseStyle = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };

                    var oldBg = GUI.backgroundColor;
                    var oldCont = GUI.contentColor;

                    if (isPaused)
                    {
                        // Unity 툴바 Pause랑 비슷한 파란색
                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                            ? new Color(0.24f, 0.48f, 0.90f, 1f) // Dark Skin
                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                        GUI.contentColor = Color.white;
                    }

                    if (GUI.Button(btnRect, pauseContent, pauseStyle))
                    {
                        kernel.TogglePause();
                    }

                    GUI.backgroundColor = oldBg;
                    GUI.contentColor = oldCont;
                }

                GUILayout.Space(4);

                // ===== 기존 정보 라벨들 =====
                var elapsed = kernel.SimulationAccumulatorSeconds;

                // Create a custom GUIStyle for the label
                GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
                centeredLabelStyle.alignment = TextAnchor.LowerCenter;
                centeredLabelStyle.fontStyle = FontStyle.Normal;
                centeredLabelStyle.fontSize = 10;

                GUILayout.Label($"Since running in seconds: {elapsed:0}", centeredLabelStyle);

                GUILayout.FlexibleSpace();

                GUILayout.Label(ExplorerFindState.LabelEntityId, centeredLabelStyle, GUILayout.Width(70));

                GUIStyle tfStyle = new GUIStyle(GUI.skin.textField);
                tfStyle.alignment = TextAnchor.LowerLeft;
                tfStyle.fontStyle = FontStyle.Normal;
                tfStyle.fontSize = 10;

                _findState.EntityIdText = GUILayout.TextField(_findState.EntityIdText, tfStyle, GUILayout.Width(40));
                _findState.EntityIdText = new string(_findState.EntityIdText.Where(char.IsDigit).ToArray());

                _findState.EntityGenText = GUILayout.TextField(_findState.EntityGenText, tfStyle, GUILayout.Width(40));
                _findState.EntityGenText = new string(_findState.EntityGenText.Where(char.IsDigit).ToArray());

                if (int.TryParse(_findState.EntityGenText, out var gen))
                {
                    if (gen < 0) _findState.EntityGenText = "0";
                }

                GUIStyle centeredButtonStyle = new GUIStyle(GUI.skin.button);
                centeredButtonStyle.alignment = TextAnchor.MiddleCenter;
                centeredButtonStyle.fontStyle = FontStyle.Normal;
                centeredButtonStyle.fontSize = 10;
                // Find => enter single-entity view (no system switching)
                var contentFind = new GUIContent(ExplorerFindState.BtnFind, ExplorerFindState.TipFind);
                if (GUILayout.Button(contentFind, centeredButtonStyle, GUILayout.Width(56)))
                {
                    if (int.TryParse(_findState.EntityIdText, out var id) && id > 0)
                    {
                        _findState.EntityId = id;

                        if (int.TryParse(_findState.EntityGenText, out var gen2))
                        {
                            _findState.EntityGen = gen2;
                        }

                        var world = kernel.CurrentWorld;
                        _findState.FoundValid = world?.IsAlive(id, gen2) ?? false;
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
                        _findState.EntityId = null;
                        _findState.EntityGen = null;
                        _findState.FoundValid = false;
                        _findState.WatchedSystemsFold = false;
                        _findState.IsFindMode = true; // still enter to show guidance
                    }

                    Repaint();
                }

                // Clear Filter => exit single-entity view
                var contentClear = new GUIContent(ExplorerFindState.BtnClear, ExplorerFindState.TipClear);
                if (GUILayout.Button(contentClear, centeredButtonStyle, GUILayout.Width(60)))
                {
                    if (_findState.IsFindMode)
                    {
                        _entityPanel.EntityFold[_findState.FoundEntity] = _findState.EntityFoldBackup;
                    }

                    _findState.EntityIdText = "";
                    _findState.EntityGenText = "0";
                    _findState.EntityId = null;
                    _findState.EntityGen = null;
                    _findState.FoundValid = false;
                    _findState.WatchedSystemsFold = false;
                    _findState.IsFindMode = false;
                    Repaint();
                }

                _coreState.EditMode = GUILayout.Toggle(_coreState.EditMode, "Edit", centeredButtonStyle, GUILayout.Width(60));
            }
        }
    }
}