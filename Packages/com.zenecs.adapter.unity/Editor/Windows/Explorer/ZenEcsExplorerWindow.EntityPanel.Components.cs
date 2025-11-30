#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Components 섹션 전용 partial.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// Draws the “Components” summary line + Add 버튼 + 리스트.
        /// </summary>
        void DrawEntityComponentsSection(IWorld world, Entity e, bool isSingleton)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

            var comps = world.GetAllComponents(e).ToArray();

            // 화살표 (전체 접기/펼치기)
            var rArrow = new Rect(r.x + 3, r.y + 1, 18f, r.height - 2);

            const float addW = 20f;
            const float gap = 6f;

            // 오른쪽 Add Component 버튼
            var rAddComp = new Rect(r.xMax - addW - 1.5f, r.y + 2, addW, r.height);
            // 라벨 영역
            var rLabel = new Rect(
                rArrow.xMax - 1f,
                r.y,
                r.width - (rArrow.width + addW + gap + 4f),
                r.height);

            // 모든 컴포넌트(그릴 수 있는 것 기준)가 열려 있는지
            bool allOpen = AreAllComponentsOpen_VisibleOnly(e, comps);

            // Fold-all 토글
            EditorGUI.BeginChangeCheck();
            var visNext = EditorGUI.Foldout(rArrow, allOpen, GUIContent.none, false);
            EditorGUIUtility.AddCursorRect(rArrow, MouseCursor.Link);
            if (EditorGUI.EndChangeCheck())
            {
                SetAllComponentsFold(world, e, visNext);
                Repaint();
                GUIUtility.ExitGUI();
            }

            // 라벨
            EditorGUI.LabelField(rLabel, $"Components: {comps.Length}");

            // Add Component 버튼
            using (new EditorGUI.DisabledScope(!_coreState.EditMode))
            {
                if (GUI.Button(rAddComp, ZenGUIContents.IconPlus(), EditorStyles.iconButton))
                {
                    var allTypes = ZenComponentPickerWindow.FindAllZenComponents().ToList();
                    var disabled = new HashSet<Type>();
                    foreach (var (tHave, _) in world.GetAllComponents(e))
                        disabled.Add(tHave);

                    ZenComponentPickerWindow.Show(
                        allTypes,
                        disabled,
                        picked =>
                        {
                            var inst = ZenDefaults.CreateWithDefaults(picked);
                            if (inst != null)
                            {
                                world.ExternalCommandEnqueue(
                                    ExternalCommand.AddComponent(e, inst.GetType(), inst));
                            }

                            Repaint();
                        },
                        rAddComp,
                        ZenStringTable.GetAddComponent(e));
                }
            }

            // 실제 리스트
            DrawComponentsList(world, e, isSingleton, comps);
        }

        /// <summary>
        /// Component foldout + body list.
        /// </summary>
        void DrawComponentsList(
            IWorld world,
            Entity e,
            bool isSingleton,
            (Type type, object? boxed)[] compsArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in compsArray)
                {
                    var key = new EntityTypeKey(e, t);
                    if (!_entityPanel.ComponentFold.TryGetValue(key, out var openC))
                        openC = false;

                    _entityPanel.ComponentFold[key] = openC;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // ===== Component header =====
                        var headRectC = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));

                        ZenFoldoutHeader.DrawRow(
                            ref openC,
                            headRectC,
                            t.Name,
                            t.Namespace ?? string.Empty,
                            rRight =>
                            {
                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                using (new EditorGUI.DisabledScope(!_coreState.EditMode))
                                {
                                    // Remove
                                    if (!isSingleton)
                                    {
                                        if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                        {
                                            if (EditorUtility.DisplayDialog(
                                                    "Remove Component",
                                                    $"Remove this component?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                    "Yes",
                                                    "No"))
                                            {
                                                world.ExternalCommandEnqueue(
                                                    ExternalCommand.RemoveComponent(e, t));
                                            }
                                        }
                                    }

                                    // Reset
                                    if (GUI.Button(rR1, "R", EditorStyles.miniButton))
                                    {
                                        var inst = ZenDefaults.CreateWithDefaults(t);
                                        if (inst != null)
                                        {
                                            world.ExternalCommandEnqueue(
                                                ExternalCommand.ReplaceComponent(e, t, inst));
                                        }
                                    }
                                }

                                // Ping 타입
                                if (GUI.Button(rR2, ZenGUIContents.IconPing(), EditorStyles.iconButton))
                                {
                                    PingSystemTypeNoSelect(t);
                                }
                            },
                            foldable: hasFields,
                            noMarginTitle: false);

                        _entityPanel.ComponentFold[key] = openC;

                        if (!openC || !hasFields || boxed == null)
                            continue;

                        // ===== Component body =====
                        try
                        {
                            object obj = CopyBox(boxed, t);
                            float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                            bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                            var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
                            var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);

                            EditorGUI.BeginChangeCheck();
                            ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                            if (EditorGUI.EndChangeCheck() && _coreState.EditMode)
                            {
                                world.ExternalCommandEnqueue(
                                    ExternalCommand.ReplaceComponent(e, t, obj));
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            // 컴포넌트 타입이 레지스트리에 없는 경우는 무시
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 엔티티의 모든 컴포넌트(폼으로 그릴 수 있는 것만)의 foldout 상태를 일괄 변경.
        /// </summary>
        void SetAllComponentsFold(IWorld world, Entity e, bool open)
        {
            var comps = world.GetAllComponents(e);
            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t))
                    continue;

                var key = new EntityTypeKey(e, t);
                _entityPanel.ComponentFold[key] = open;
            }
        }

        /// <summary>
        /// 그릴 수 있는 컴포넌트들이 모두 펼쳐져 있는지 확인.
        /// 하나도 없으면 false.
        /// </summary>
        bool AreAllComponentsOpen_VisibleOnly(Entity e, (Type type, object? boxed)[] comps)
        {
            bool any = false;

            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t))
                    continue;

                any = true;

                var key = new EntityTypeKey(e, t);
                if (!_entityPanel.ComponentFold.TryGetValue(key, out var openC) || !openC)
                    return false;
            }

            return any && true;
        }
    }
}
#endif
