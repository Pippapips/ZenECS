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

                GUILayout.Label(LABEL_ENTITY_ID, centeredLabelStyle, GUILayout.Width(70));

                GUIStyle tfStyle = new GUIStyle(GUI.skin.textField);
                tfStyle.alignment = TextAnchor.LowerLeft;
                tfStyle.fontStyle = FontStyle.Normal;
                tfStyle.fontSize = 10;

                _entityIdText = GUILayout.TextField(_entityIdText, tfStyle, GUILayout.Width(40));
                _entityIdText = new string(_entityIdText.Where(char.IsDigit).ToArray());

                _entityGenText = GUILayout.TextField(_entityGenText, tfStyle, GUILayout.Width(40));
                _entityGenText = new string(_entityGenText.Where(char.IsDigit).ToArray());

                if (int.TryParse(_entityGenText, out var gen))
                {
                    if (gen < 0) _entityGenText = "0";
                }

                GUIStyle centeredButtonStyle = new GUIStyle(GUI.skin.button);
                centeredButtonStyle.alignment = TextAnchor.MiddleCenter;
                centeredButtonStyle.fontStyle = FontStyle.Normal;
                centeredButtonStyle.fontSize = 10;
                // Find => enter single-entity view (no system switching)
                var contentFind = new GUIContent(BTN_FIND, TIP_FIND);
                if (GUILayout.Button(contentFind, centeredButtonStyle, GUILayout.Width(56)))
                {
                    if (int.TryParse(_entityIdText, out var id) && id > 0)
                    {
                        _findEntityId = id;

                        if (int.TryParse(_entityGenText, out var gen2))
                        {
                            _findEntityGen = gen2;
                        }

                        var world = kernel.CurrentWorld;
                        _foundValid = world?.IsAlive(id, gen2) ?? false;
                        _foundEntity =
                            _foundValid ? (Entity)Activator.CreateInstance(typeof(Entity), id, gen) : default;

                        if (_foundValid)
                        {
                            if (_entityFold.TryGetValue(_foundEntity, out var fold))
                            {
                                _findEntityFoldBackup = fold;
                                _entityFold[_foundEntity] = true;
                            }
                            else
                            {
                                _findEntityFoldBackup = false;
                                _entityFold.TryAdd(_foundEntity, true);
                            }
                        }

                        _findMode = true;
                    }
                    else
                    {
                        _findEntityId = null;
                        _findEntityGen = null;
                        _foundValid = false;
                        _findWatchedSystemsFold = false;
                        _findMode = true; // still enter to show guidance
                    }

                    Repaint();
                }

                // Clear Filter => exit single-entity view
                var contentClear = new GUIContent(BTN_CLEAR_FILTER, TIP_CLEAR);
                if (GUILayout.Button(contentClear, centeredButtonStyle, GUILayout.Width(60)))
                {
                    if (_findMode)
                    {
                        _entityFold[_foundEntity] = _findEntityFoldBackup;
                    }

                    _entityIdText = "";
                    _entityGenText = "0";
                    _findEntityId = null;
                    _findEntityGen = null;
                    _foundValid = false;
                    _findWatchedSystemsFold = false;
                    _findMode = false;
                    Repaint();
                }

                _editMode = GUILayout.Toggle(_editMode, "Edit", centeredButtonStyle, GUILayout.Width(60));
            }
        }
    }
}