#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Right side entity/singleton inspector panel of the ZenECS Explorer.
    /// 이 partial에서는 엔티티 헤더와 섹션 진입만 담당합니다.
    /// 실제 렌더링 로직은 Components / Contexts / Binders partial로 분리됩니다.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// Draws the full inspector of a single entity:
        /// header → components → contexts → binders.
        /// </summary>
        void DrawOneEntity(IWorld world, Entity e)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                bool isSingleton = world.HasSingleton(e);
                
                // ===== Entity header =====
                DrawEntityHeader(world, e, isSingleton, out bool entityOpen);
                if (!entityOpen)
                    return;

                // ===== 1) Components =====
                DrawEntityComponentsSection(world, e, isSingleton);

                // ===== 2) Contexts =====
                DrawEntityContextsSection(world, e);

                // ===== 3) Binders =====
                DrawEntityBindersSection(world, e);
            }
        }

        /// <summary>
        /// Entity 헤더 + 접기 상태 + 삭제 버튼까지 처리.
        /// </summary>
        void DrawEntityHeader(IWorld world, Entity e, bool isSingleton, out bool isOpen)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var headRect = GUILayoutUtility.GetRect(
                10,
                line + 6f,
                GUILayout.ExpandWidth(true));

            // foldout 상태 초기화
            var open = _entityPanel.EntityFold.GetValueOrDefault(e, false);

            var entityTitle = ZenStringTable.GetEntityTitle(e);
            if (isSingleton)
                entityTitle += ZenStringTable.SINGLETON;

            ZenFoldoutHeader.DrawRow(
                ref open,
                headRect,
                entityTitle,
                nameSpace: string.Empty,
                drawRightButtons: rectRight =>
                {
                    var style = EditorStyles.miniButton;

                    const float wBtn = 20f;
                    var hBtn = Mathf.Ceil(EditorGUIUtility.singleLineHeight + 2f);
                    var yBtn = rectRight.y + Mathf.Max(0f, (rectRight.height - hBtn) * 0.5f);
                    var right = rectRight.xMax - 4f;

                    // 맨 오른쪽: 삭제 X
                    var rDel = new Rect(right - wBtn, yBtn, wBtn, hBtn);
                    using (new EditorGUI.DisabledScope(!_coreState.EditMode))
                    {
                        if (GUI.Button(rDel, "X", style))
                        {
                            string msg;
                            if (isSingleton)
                                msg = ZenStringTable.GetRemoveThisSingletonEntity(e);
                            else
                                msg = ZenStringTable.GetRemoveThisEntity(e);

                            if (EditorUtility.DisplayDialog(
                                    ZenStringTable.RemoveEntity,
                                    msg,
                                    ZenStringTable.Yes,
                                    ZenStringTable.No))
                            {
                                world.ExternalCommandEnqueue(ExternalCommand.DestroyEntity(e));

                                // Find 모드로 선택되어 있던 엔티티였다면 상태 복원
                                if (_findState.IsFindMode && _findState.FoundEntity.Equals(e))
                                {
                                    _entityPanel.EntityFold[_findState.FoundEntity] =
                                        _findState.EntityFoldBackup;
                                    ClearState();
                                }

                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                },
                true,
                false);

            _entityPanel.EntityFold[e] = open;
            isOpen = open;
        }
    }
}
#endif
