#if UNITY_EDITOR
#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Singletons section in the systems tree and singleton entity detail view.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        // =====================================================================
        //  LEFT: Singletons 리스트 한 줄
        // =====================================================================

        void DrawSingletonRow(Type type, Entity owner, IWorld world)
        {
            var typeName = type.Name;

            // === 한 줄 Rect 계산 ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Indent 반영
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float iconW = 24f;
            const float gap = 1f;

            // 오른쪽 끝: X
            var removeRect = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
            var pingRect = new Rect(removeRect.x - iconW, rowRect.y, iconW, rowRect.height);

            // 가운데: Singleton 버튼
            float sysX = rowRect.x;
            float sysRight = pingRect.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Singleton 버튼 =====
            string label = $"{typeName}  (Entity #{owner.Id}:{owner.Gen})";

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };

            bool selected =
                _entityPanel.HasSelectedSingleton &&
                _entityPanel.SelectedSingletonType == type &&
                _entityPanel.SelectedSingletonEntity.Id == owner.Id &&
                _entityPanel.SelectedSingletonEntity.Gen == owner.Gen;

            bool clicked = GUI.Toggle(sysRect, selected, label, btnStyle);
            if (clicked && !selected)
            {
                ClearState(true, false);
                _entityPanel.HasSelectedSingleton = true;
                _entityPanel.SelectedSingletonType = type;
                _entityPanel.SelectedSingletonEntity = owner;
            }

            // ===== 돋보기 버튼 (컴포넌트 타입 핑) =====
            {
                var pingBtnRect = new Rect(
                    pingRect.x,
                    pingRect.y + 1f,
                    pingRect.width,
                    pingRect.height - 2f
                );

                var searchContent = ZenGUIContents.IconPing();
                var iconStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(3, 3, 3, 3),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 10
                };

                if (GUI.Button(pingBtnRect, searchContent, iconStyle))
                {
                    PingComponentType(type);
                }
            }
            
            // 🔸 삭제 버튼 (기존 그대로)
            using (new EditorGUI.DisabledScope(!_coreState.EditMode))
            {
                var delStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontStyle = FontStyle.Normal,
                    fontSize = 10
                };

                var gcDel = new GUIContent("X", "Remove this singleton from Entity");
                if (GUI.Button(removeRect, gcDel, delStyle))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Singleton",
                            $"Remove this {label} singleton?",
                            "Yes", "No"))
                    {
                        world.ExternalCommandEnqueue(ExternalCommand.RemoveSingleton(type));
                        Repaint();
                    }
                }
            }
        }
    }
}
#endif