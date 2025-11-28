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
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;
using ZenECS.EditorUtils;

namespace ZenECS.EditorWindows
{
    /// <summary>
    /// Right side entity inspector panel of the ZenECS Explorer:
    /// - Entity header
    /// - Components list
    /// - Binders list
    /// - Contexts list (readonly)
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        // =====================================================================
        //  ENTITY / COMPONENT / BINDER / CONTEXT 패널
        // =====================================================================

        void DrawOneEntity(IWorld world, Entity e)
        {
            _entityFold.TryAdd(e, false);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // ===== Entity header =====
                var headRect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight + 6f,
                    GUILayout.ExpandWidth(true));
                bool openE = _entityFold[e];

                var entityTitle = $"Entity #{e.Id}:{e.Gen}";
                var isSingleton = world.HasSingleton(e);
                if (isSingleton)
                {
                    entityTitle += " <color=#999900><size=10>SINGLETON</size></color>";
                }

                ZenFoldoutHeader.DrawRow(ref openE, headRect, entityTitle, "", rRight =>
                {
                    var style = EditorStyles.miniButton;

                    const float wBtn = 20f;
                    const float gap = 3f;

                    var hBtn = Mathf.Ceil(EditorGUIUtility.singleLineHeight + 2f);
                    var yBtn = rRight.y + Mathf.Max(0f, (rRight.height - hBtn) * 0.5f);

                    var right = rRight.xMax - 4f;

                    // 맨 오른쪽: 삭제 X
                    var rDel = new Rect(right - wBtn, yBtn, wBtn, hBtn);
                    right = rDel.x - gap;

                    // // 그 왼쪽: 메인 뷰 선택 버튼 (•)
                    // Rect? rSel = null;
                    // if (EcsExplorerActions.TryGetEntityMainView(world, e, out var go))
                    // {
                    //     rSel = new Rect(right - wBtn + 2, yBtn + 1, wBtn, hBtn);
                    //     right = rSel.Value.x - gap;
                    // }

                    using (new EditorGUI.DisabledScope(!_ui.EditMode))
                    {
                        if (GUI.Button(rDel, "X", style))
                        {
                            var msg = $"Remove this entity?\n\nEntity #{e.Id}:{e.Gen}";
                            if (isSingleton)
                            {
                                msg = $"Remove this singleton?\n\nSingleton Entity #{e.Id}:{e.Gen}";
                            }

                            if (EditorUtility.DisplayDialog(
                                    "Remove Entity",
                                    msg,
                                    "Yes", "No"))
                            {
                                world.ExternalCommandEnqueue(ExternalCommand.DestroyEntity(e));
                                
                                _entityFold[_foundEntity] = _findEntityFoldBackup;

                                _entityIdText = "";
                                _findEntityId = null;

                                _entityGenText = "0";
                                _findEntityGen = null;

                                _findWatchedSystemsFold = false;
                                _foundValid = false;
                                _findMode = false;
                                Repaint();
                            }
                        }
                    }

                    // var gcPing = GetSearchIconContent("Ping entity view in Hierarchy");
                    // if (rSel.HasValue && GUI.Button(rSel.Value, gcPing, EditorStyles.iconButton))
                    // {
                    //     EcsExplorerActions.TrySelectEntityMainView(go);
                    // }
                }, true, false);

                _entityFold[e] = openE;

                if (!openE) return;

                // ===== Components Summary line =====
                var line = EditorGUIUtility.singleLineHeight;
                var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

                var compsEnum = world.GetAllComponents(e);
                var arr = compsEnum.ToArray();

// Arrow toggle (open/close all visible components)
                var rArrow = new Rect(r.x + 3, r.y + 1, 18f, r.height - 2);

                const float addWComp = 20f;
                const float gapComp = 6f;

// 오른쪽 끝 Add Component 버튼 영역
                var rAddComp = new Rect(r.xMax - addWComp - 1.5f, r.y + 2, addWComp, r.height);

// 라벨 영역: 화살표 + Add 버튼을 제외한 부분
                var rLabel = new Rect(
                    rArrow.xMax - 1f,
                    r.y,
                    r.width - (rArrow.width + addWComp + gapComp + 4f),
                    r.height
                );

                var allOpen = AreAllComponentsOpen_VisibleOnly(e, arr);

                // 전체 컴포넌트 접기/펼치기 토글
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
                if (isSingleton)
                {
                    EditorGUI.LabelField(rLabel, $"Component");
                }
                else
                {
                    EditorGUI.LabelField(rLabel, $"Components: {arr.Length}");
                }

                // Add Component 버튼 (이제 여기!)
                if (!isSingleton)
                {
                    using (new EditorGUI.DisabledScope(!_ui.EditMode))
                    {
                        if (GUI.Button(rAddComp, GetPlusIconContent(), EditorStyles.iconButton))
                        {
                            var all = ZenECS.EditorCommon.ZenComponentPickerWindow.FindAllZenComponents().ToList();
                            var disabled = new HashSet<Type>();
                            foreach (var (tHave, _) in world.GetAllComponents(e))
                                disabled.Add(tHave);

                            ZenComponentPickerWindow.Show(
                                all,
                                disabled,
                                picked =>
                                {
                                    var inst = ZenDefaults.CreateWithDefaults(picked);
                                    if (inst != null)
                                    {
                                        world.ExternalCommandEnqueue(ExternalCommand.AddComponent(e, inst.GetType(), inst));
                                    }

                                    Repaint();
                                },
                                rAddComp, // 이제 Components 줄 오른쪽 Rect 기준으로
                                $"Entity #{e.Id}:{e.Gen} Add Component"
                            );
                        }
                    }
                }

                // 실제 리스트 렌더링
                DrawComponentsList(world, e, isSingleton, arr);

                // ===== Components 끝난 직후 바로 아래에 추가 =====
                {
                    // ===== Contexts Summary line =====
                    if (ContextApi.TryGetAll(world, e, out var ctxs))
                    {
                        var lineC = EditorGUIUtility.singleLineHeight;
                        var rc = GUILayoutUtility.GetRect(10, lineC, GUILayout.ExpandWidth(true));

                        var rArrowC = new Rect(rc.x + 3, rc.y + 1, 18f, rc.height - 2);

                        const float addW = 20f;
                        const float gap = 6f;
                        var rAddC = new Rect(rc.xMax - addW - 1.5f, rc.y + 2, addW, rc.height);
                        var rLabelC = new Rect(
                            rArrowC.xMax - 1f, rc.y,
                            rc.width - (rArrowC.width + addW + gap + 4f),
                            rc.height
                        );

                        // 모든 컨텍스트가 열려 있는지 (Components와 동일 패턴)
                        var allOpen_ = AreAllContextsOpen(e, ctxs);

                        EditorGUI.BeginChangeCheck();
                        var visNextCtx = EditorGUI.Foldout(rArrowC, allOpen_, GUIContent.none, false);
                        EditorGUIUtility.AddCursorRect(rArrowC, MouseCursor.Link);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 루트 토글 → 전체 컨텍스트 접기/펼치기
                            SetAllContextsFold(world, e, visNextCtx);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        EditorGUI.LabelField(rLabelC, $"Contexts: {ctxs.Length}");

                        // Add Context 버튼 (기존 로직 유지)
                        using (new EditorGUI.DisabledScope(!_ui.EditMode))
                        {
                            if (GUI.Button(rAddC, GetPlusIconContent(), EditorStyles.iconButton))
                            {
                                // 이미 붙어있는 컨텍스트 타입 집합
                                var disabledCtxTypes = new HashSet<Type>(ctxs.Select(c => c.type));

                                ContextAssetPickerWindow.Show(
                                    activatorRectGui: rAddC,
                                    onPick: asset =>
                                    {
                                        ContextApi.AddFromAsset(world, e, asset);
                                        Repaint();
                                    },
                                    disabledContextTypes: disabledCtxTypes,
                                    title: $"Entity #{e.Id}:{e.Gen} - Add Context"
                                );
                            }
                        }

                        // 🔴 항상 DrawContextsList 호출 → 이름/네임스페이스는 언제나 표시
                        DrawContextsList(world, e, ctxs);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Contexts API has been disconnected.",
                            MessageType.None);
                    }
                }


                {
                    // ===== Binders Summary line =====
                    var bindersOk = BinderApi.TryGetAll(world, e, out var binders);
                    if (!bindersOk)
                    {
                        EditorGUILayout.HelpBox(
                            "Binders API has been disconnected.",
                            MessageType.None);
                    }
                    else
                    {
                        var line2 = EditorGUIUtility.singleLineHeight;
                        var r2 = GUILayoutUtility.GetRect(10, line2, GUILayout.ExpandWidth(true));

                        // Fold-all for binders (보이는 것만)
                        var rArrow2 = new Rect(r2.x + 3, r2.y + 1, 18f, r2.height - 2);

                        const float addW = 20f; // 버튼 폭
                        const float gap = 6f;

                        // 오른쪽 끝 Add 버튼 영역
                        var rAdd = new Rect(r2.xMax - addW - 1.5f, r2.y + 2, addW, r2.height);
                        // 라벨은 버튼과 화살표를 제외한 영역
                        var rLabel2 = new Rect(rArrow2.xMax - 1f, r2.y, r2.width - (rArrow2.width + addW + gap + 4f),
                            r2.height);

                        bool allOpenB = AreAllBindersOpen_VisibleOnly(e, binders);

                        // 화살표 토글
                        EditorGUI.BeginChangeCheck();
                        var visNextB = EditorGUI.Foldout(rArrow2, allOpenB, GUIContent.none, false);
                        EditorGUIUtility.AddCursorRect(rArrow2, MouseCursor.Link);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetAllBindersFold(world, e, visNextB);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        // 라벨
                        EditorGUI.LabelField(rLabel2, $"Binders: {binders.Length}");

                        // Add Binder 버튼
                        using (new EditorGUI.DisabledScope(!_ui.EditMode || !BinderApi.CanAdd(world)))
                        {
                            if (GUI.Button(rAdd, GetPlusIconContent(), EditorStyles.iconButton))
                            {
                                // 전체 바인더 타입 수집
                                var allBinders = BinderTypeFinder.All();
                                // 이미 붙어있는 타입은 disabled 처리
                                var disabledB = new HashSet<Type>(binders.Select(x => x.type));

                                ZenBinderPickerWindow.Show(
                                    allBinderTypes: allBinders,
                                    disabled: disabledB,
                                    onPick: picked =>
                                    {
                                        var inst = ZenDefaults.CreateWithDefaults(picked);
                                        if (inst != null) BinderApi.Add(world, e, inst);
                                        Repaint();
                                    },
                                    activatorRectGui: rAdd,
                                    title: $"Entity #{e.Id}:{e.Gen} - Add Binder"
                                );
                            }
                        }

                        // 실제 리스트
                        DrawBindersList(world, e, binders);
                    }
                }
            }
        }

        void DrawComponentsList(IWorld world, Entity e, bool isSingleton, (Type type, object? boxed)[] compsArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in compsArray)
                {
                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                    if (!_componentFold.ContainsKey(ck)) _componentFold[ck] = false;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // ===== Component header =====
                        var headRectC = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
                        bool openC = _componentFold[ck];

                        ZenFoldoutHeader.DrawRow(
                            ref openC,
                            headRectC,
                            t.Name,
                            t.Namespace,
                            rRight =>
                            {
                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                using (new EditorGUI.DisabledScope(!_ui.EditMode))
                                {
                                    if (!isSingleton)
                                    {
                                        if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                        {
                                            if (EditorUtility.DisplayDialog(
                                                    "Remove Component",
                                                    $"Remove this component?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                    "Yes", "No"))
                                            {
                                                world.ExternalCommandEnqueue(ExternalCommand.RemoveComponent(e, t));
                                                
                                                _componentFold.Remove(ck);
                                                Repaint();
                                            }
                                        }

                                        using (new EditorGUI.DisabledScope(!hasFields))
                                        {
                                            if (GUI.Button(rR1, "R", EditorStyles.miniButton))
                                            {
                                                // if (EditorUtility.DisplayDialog(
                                                //         "Reset Component",
                                                //         $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                //         "Yes", "No"))
                                                {
                                                    var def = ZenDefaults.CreateWithDefaults(t);
                                                    world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, def));
                                                    
                                                    Repaint();
                                                }
                                            }
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR2, icon, EditorStyles.iconButton))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                    else
                                    {
                                        using (new EditorGUI.DisabledScope(!hasFields))
                                        {
                                            if (GUI.Button(rR0, "R", EditorStyles.miniButton))
                                            {
                                                // if (EditorUtility.DisplayDialog(
                                                //         "Reset Component",
                                                //         $"Reset to defaults?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}Component",
                                                //         "Yes", "No"))
                                                {
                                                    var def = ZenDefaults.CreateWithDefaults(t);
                                                    world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, def));
                                                    Repaint();
                                                }
                                            }
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR1, icon, EditorStyles.iconButton))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                }
                            },
                            foldable: hasFields,
                            false
                        );

                        _componentFold[ck] = hasFields && openC;

                        // ===== body =====
                        if (!hasFields || !_componentFold[ck]) continue;

                        try
                        {
                            object obj = CopyBox(boxed, t);
                            float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                            bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                            var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
                            var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);

                            EditorGUI.BeginChangeCheck();
                            ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                            if (EditorGUI.EndChangeCheck() && _ui.EditMode)
                            {
                                world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, obj));
                            }
                        }
                        catch (KeyNotFoundException) { }
                    }
                }
            }
        }

        void SetAllComponentsFold(IWorld world, Entity e, bool open)
        {
            var comps = world.GetAllComponents(e);
            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                _componentFold[key] = open;
            }
        }

        bool AreAllComponentsOpen_VisibleOnly(Entity e, (Type type, object? boxed)[] comps)
        {
            bool any = false;
            foreach (var (t, _) in comps)
            {
                if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
                any = true;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}";
                if (!_componentFold.TryGetValue(key, out bool open) || !open)
                    return false;
            }

            return any && true;
        }

        void DrawBindersList(IWorld world, Entity e, (Type type, object? boxed)[] bindersArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in bindersArray)
                {
                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                    if (!_binderFold.ContainsKey(ck)) _binderFold[ck] = false;

                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    bool hasMetaOrFields = boxed != null && CanShowBinderBody(t, boxed);

                    // IBinder + Disabled 판정
                    var binder = boxed as IBinder;
                    bool isDisabled = binder != null && !binder.Enabled;

                    string binderTitle = t.Name;
                    if (isDisabled)
                        binderTitle += " [Disabled]";

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var headRectB = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
                        bool openB = _binderFold[ck];

                        // 헤더 이름 색상: Disabled면 짙은 회색
                        var prevHeaderColor = GUI.color;
                        if (isDisabled)
                        {
                            // 짙은 회색 (0=검정,1=흰색 기준)
                            GUI.color = new Color(0.45f, 0.45f, 0.45f);
                        }

                        ZenFoldoutHeader.DrawRow(
                            ref openB,
                            headRectB,
                            binderTitle,
                            nameSpace: t.Namespace,
                            rRight =>
                            {
                                var style = EditorStyles.miniButton;

                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                // 오른쪽 끝: 삭제 버튼
                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // binder 활성 상태
                                var hasBinder = binder != null;
                                bool isEnabled = hasBinder && binder is { Enabled: true };
                                bool canToggle = hasBinder && _ui.EditMode; // 읽기전용일 땐 토글 비활성

                                var icon = GetSearchIconContent("Ping script asset");
                                if (GUI.Button(rR2, icon, EditorStyles.iconButton))
                                {
                                    PingContextType(t);
                                }

                                // 🔹 Enabled Pause 스타일 (System / Global Pause와 동일)
                                using (new EditorGUI.DisabledScope(!canToggle))
                                {
                                    // CKWORK

                                    // Unity 기본 Pause 아이콘
                                    var pauseContent = EditorGUIUtility.IconContent("PauseButton");
                                    if (pauseContent == null || pauseContent.image == null)
                                        pauseContent = EditorGUIUtility.TrTextContent("⏸");

                                    var pauseStyle = new GUIStyle("Button")
                                    {
                                        alignment = TextAnchor.MiddleCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    var oldBg = GUI.backgroundColor;
                                    var oldCont = GUI.contentColor;

                                    if (hasBinder && !isEnabled)
                                    {
                                        // Unity 툴바 Pause랑 비슷한 파란색
                                        GUI.backgroundColor = EditorGUIUtility.isProSkin
                                            ? new Color(0.24f, 0.48f, 0.90f, 1f)  // Dark Skin
                                            : new Color(0.20f, 0.45f, 0.90f, 1f); // Light Skin

                                        GUI.contentColor = Color.white;
                                    }

                                    if (GUI.Button(rR1, pauseContent, pauseStyle) && canToggle)
                                    {
                                        isEnabled = !isEnabled;
                                        if (binder != null)
                                        {
                                            binder.Enabled = isEnabled;
                                        }
                                    }

                                    GUI.backgroundColor = oldBg;
                                    GUI.contentColor = oldCont;
                                }

                                // 🔸 삭제 버튼 (기존 그대로)
                                using (new EditorGUI.DisabledScope(!_ui.EditMode || !BinderApi.CanRemove(world)))
                                {
                                    var gcDel = new GUIContent("X", "Remove this Binder from Entity");
                                    if (GUI.Button(rR0, gcDel, style))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Binder",
                                                $"Remove this binder?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                "Yes", "No"))
                                        {
                                            BinderApi.Remove(world, e, t);
                                            _binderFold.Remove(ck);
                                            Repaint();
                                        }
                                    }
                                }
                            },
                            foldable: hasMetaOrFields,
                            false
                        );

                        GUI.color = prevHeaderColor;
                        _binderFold[ck] = openB;

                        if (!_binderFold[ck]) continue;

                        // ===== body =====
                        var prevBodyColor = GUI.color;
                        if (isDisabled)
                        {
                            // 바디도 같은 짙은 회색 톤
                            GUI.color = new Color(0.45f, 0.45f, 0.45f);
                        }

                        // 메타 (IBind 관찰 컴포넌트 등)
                        DrawBinderMeta(world, e, boxed);

                        try
                        {
                            if (hasFields)
                            {
                                object obj = CopyBox(boxed, t);
                                float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                                bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);
                                var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
                                var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);

                                EditorGUI.BeginChangeCheck();
                                ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                                if (EditorGUI.EndChangeCheck() && _ui.EditMode && BinderApi.CanReplace(world))
                                    BinderApi.Replace(world, e, obj);
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            // ignore
                        }

                        GUI.color = prevBodyColor;
                    }
                }
            }
        }

        void SetAllBindersFold(IWorld world, Entity e, bool open)
        {
            if (!BinderApi.TryGetAll(world, e, out var arr)) return;
            foreach (var (t, boxed) in arr)
            {
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:BINDER";
                _binderFold[key] = open;
            }
        }

        void DrawBinderMeta(IWorld world, Entity e, object? binderObj)
        {
            if (binderObj == null) return;

            var t = binderObj.GetType();
            var binder = binderObj as IBinder;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // --- Priority 편집 영역 ---
                if (binder != null)
                {
                    // 현재 Priority 값 가져오기 (우선 리플렉션, 실패하면 0)
                    int currentPriority = 0;
                    try
                    {
                        var piPrio = t.GetProperty(
                            "ApplyOrder",
                            BindingFlags.Public | BindingFlags.Instance
                        );
                        if (piPrio != null &&
                            piPrio.PropertyType == typeof(int) &&
                            piPrio.GetIndexParameters().Length == 0)
                        {
                            currentPriority = (int)(piPrio.GetValue(binder) ?? 0);
                        }
                    }
                    catch
                    {
                        // 무시하고 0 사용
                    }

                    using (new EditorGUI.DisabledScope(!_ui.EditMode))
                    {
                        // 한 줄짜리 Rect를 직접 받아서 x 좌표를 제어
                        var lineRect = EditorGUILayout.GetControlRect();

                        int newPriority = currentPriority;
                        bool changedByButton = false;

                        // Observing (IBinds) 가 사용하는 boldLabel의 왼쪽 마진만큼 안쪽에서 시작
                        float startX = lineRect.x + EditorStyles.boldLabel.margin.left + 12;
                        float h = lineRect.height;
                        const float btnW = 22f;
                        const float btnGap = 2f;
                        const float labelGap = 1f;
                        const float labelW = 90f;

                        // [-] / [+] / Label / IntField의 Rect 계산
                        var minusRect = new Rect(startX, lineRect.y, btnW, h);
                        var plusRect = new Rect(minusRect.xMax + btnGap, lineRect.y, btnW, h);
                        var labelRect = new Rect(plusRect.xMax + labelGap - 8, lineRect.y, labelW, h);
                        var fieldRect = new Rect(labelRect.xMax + labelGap, lineRect.y,
                            lineRect.xMax - (labelRect.xMax + labelGap), h);

                        // [-] 버튼
                        if (GUI.Button(minusRect, "-"))
                        {
                            newPriority = currentPriority - 1;
                            changedByButton = true;
                        }

                        // [+] 버튼
                        if (GUI.Button(plusRect, "+"))
                        {
                            newPriority = currentPriority + 1;
                            changedByButton = true;
                        }

                        // "Apply Order" 라벨
                        EditorGUI.LabelField(labelRect, "Apply Order");

                        // 숫자 직접 입력
                        EditorGUI.BeginChangeCheck();
                        newPriority = EditorGUI.IntField(fieldRect, GUIContent.none, newPriority);
                        bool changedByField = EditorGUI.EndChangeCheck();

                        if ((changedByButton || changedByField) && newPriority != currentPriority)
                        {
                            try
                            {
                                binder.SetApplyOrder(newPriority);
                            }
                            catch (MissingMethodException)
                            {
                                Debug.LogWarning(
                                    $"IBinder.SetApplyOrder(int) 가 구현되지 않았습니다: {t.FullName}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }

                            Repaint();
                        }
                    }

                    GUILayout.Space(4);
                }

                // --- Observing (IBinds) 메타 정보는 그대로 유지 ---
                EditorGUILayout.LabelField("Observing (IBinds)", EditorStyles.boldLabel);

                var observed = BinderIntrospection.ExtractObservedComponentTypes(t);

                if (observed.Count == 0)
                {
                    EditorGUILayout.LabelField("— (no IBind<> found)");
                }
                else
                {
                    foreach (var ct in observed)
                    {
                        var has = world.HasComponentBoxed(e, ct);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (has)
                                EditorGUILayout.LabelField(ct.Name, GUILayout.MinWidth(80));
                            else
                                ItalicLabel.DrawLeft(ct.Name, GUILayout.MinWidth(80));

                            GUILayout.FlexibleSpace();

                            if (has)
                            {
                                OkLabel.DrawOK(20f);
                            }
                            else
                            {
                                NotAssignLabel.Draw(20);
                            }
                        }
                    }
                }
            }
        }

        void SetAllContextsFold(IWorld world, Entity e, bool open)
        {
            if (!ContextApi.TryGetAll(world, e, out var ctxs)) return;

            foreach (var (t, _) in ctxs)
            {
                if (t == null) continue;
                var key = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:CTX";
                _contextFold[key] = open;
            }
        }

        void DrawContextsList(IWorld world, Entity e, (Type type, object? boxed)[] ctxs)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, inst) in ctxs)
                {
                    if (t == null) continue;

                    var ck = $"{e.Id}:{e.Gen}:{t.AssemblyQualifiedName}:CTX";

                    if (!_contextFold.TryGetValue(ck, out var open))
                    {
                        open = false;
                        _contextFold[ck] = open;
                    }

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var headRect = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));

                        // 헤더: ZenFoldoutHeader 사용 (컴포넌트/바인더와 동일 폰트 + 네임스페이스)
                        ZenFoldoutHeader.DrawRow(
                            ref open,
                            headRect,
                            t.Name,
                            t.Namespace,
                            rRight =>
                            {
                                var style = EditorStyles.miniButton;

                                const float wBtn = 20f;
                                const float gap = 3f;
                                var hBtn = rRight.height;
                                var yBtn = rRight.y;

                                // 오른쪽 끝: 삭제 버튼
                                var rR0 = new Rect(rRight.xMax - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR1 = new Rect(rR0.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // 그 왼쪽: Binder Enabled(Pause) 토글 버튼
                                var rR2 = new Rect(rR1.x - gap - wBtn, yBtn, wBtn, hBtn);

                                // CKWORK
                                using (new EditorGUI.DisabledScope(!_ui.EditMode))
                                {
                                    if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Context",
                                                $"Remove this context?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                "Yes", "No"))
                                        {
                                            if (inst is IContext ctxInstance)
                                            {
                                                ContextApi.Remove(world, e, ctxInstance);
                                                _contextFold.Remove(ck);
                                            }

                                            Repaint();
                                        }
                                    }

                                    var pingStyle = new GUIStyle(EditorStyles.iconButton)
                                    {
                                        alignment = TextAnchor.LowerCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    var pauseStyle = new GUIStyle("Button")
                                    {
                                        alignment = TextAnchor.MiddleCenter,
                                        padding = new RectOffset(0, 0, 0, 0),
                                        margin = new RectOffset(0, 0, 0, 0)
                                    };

                                    if (inst is IContextReinitialize)
                                    {
                                        if (GUI.Button(rR1, "R", pauseStyle))
                                        {
                                            var s = $"Reinitialize {t.Name}";
                                            if (world.ReinitializeContext(e, (IContext)inst))
                                            {
                                                s += " success";
                                            }
                                            else
                                            {
                                                s += " failed";
                                            }

                                            Debug.Log(s);
                                        }

                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR2, icon, pingStyle))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                    else
                                    {
                                        var icon = GetSearchIconContent("Ping script asset");
                                        if (GUI.Button(rR1, icon, pingStyle))
                                        {
                                            PingContextType(t);
                                        }
                                    }
                                }
                            },
                            foldable: true,
                            noMarginTitle: false // ← 화살표 + 2줄(이름/네임스페이스) 레이아웃
                        );

                        _contextFold[ck] = open;

                        // 닫혀 있으면 필드 목록은 안 그림 (하지만 이름/네임스페이스는 항상 출력됨)
                        if (!open || inst == null)
                            continue;

                        // 실제 컨텍스트 필드/프로퍼티 읽기 전용 렌더링
                        DrawContextFieldsReadonly(inst, t);
                    }
                }
            }
        }

        void DrawContextFieldsReadonly(object ctxInstance, Type ctxType)
        {
            if (ctxInstance == null || ctxType == null)
                return;

            // 공용 인스턴스 멤버 수집: Field + Property(get 가능, 인덱서 제외)
            var members = new List<(string name, Type type, Func<object?> getter)>();

            // 1) 이 타입에 "직접 선언된" public 필드
            foreach (var f in ctxType.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (Attribute.IsDefined(f, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(f, typeof(HideInInspector), inherit: true)) continue;

                var localField = f;
                members.Add((
                    localField.Name,
                    localField.FieldType,
                    () => localField.GetValue(ctxInstance)
                ));
            }

            // 2) 이 타입에 "직접 선언된" public 프로퍼티 (getter만, 인덱서 제외)
            foreach (var p in ctxType.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length != 0) continue;
                if (Attribute.IsDefined(p, typeof(ZenEcsExplorerHiddenAttribute), inherit: true)) continue;
                if (Attribute.IsDefined(p, typeof(HideInInspector), inherit: true)) continue;

                var localProp = p;
                members.Add((
                    localProp.Name,
                    localProp.PropertyType,
                    () => localProp.GetValue(ctxInstance, null)
                ));
            }

            if (members.Count == 0)
                return;

            // 이름 순으로 정렬
            members.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (name, mType, getter) in members)
                {
                    object? value;
                    try
                    {
                        value = getter();
                    }
                    catch
                    {
                        value = null;
                    }

                    // UnityEngine.Object 계열이면 Ping 지원
                    if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                    {
                        var obj = value as UnityEngine.Object;
                        var rect = EditorGUILayout.GetControlRect();

                        // 라벨/값 영역 분리
                        var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                        var valRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                            rect.width - EditorGUIUtility.labelWidth, rect.height);

                        // 이름
                        EditorGUI.LabelField(labelRect, name);

                        // Object 아이콘 + 이름을 레이블로만 표시 (ObjectField 사용 안 함 → 피커 안 뜸)
                        GUIContent content;
                        if (obj != null)
                            content = EditorGUIUtility.ObjectContent(obj, mType);
                        else
                            content = new GUIContent("None", EditorGUIUtility.IconContent("Prefab Icon").image);

                        EditorGUI.LabelField(valRect, content);

                        // 라벨/값을 클릭하면 Ping만 수행
                        if (obj != null &&
                            Event.current.type == EventType.MouseDown &&
                            (labelRect.Contains(Event.current.mousePosition) ||
                             valRect.Contains(Event.current.mousePosition)))
                        {
                            EditorGUIUtility.PingObject(obj);
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        // 일반 값 타입/문자열 등은 그냥 읽기 전용 텍스트
                        string text;
                        if (value == null) text = "null";
                        else if (mType == typeof(float)) text = ((float)value).ToString("0.###");
                        else if (mType == typeof(double)) text = ((double)value).ToString("0.###");
                        else text = value.ToString() ?? "null";

                        EditorGUILayout.LabelField(name, text);
                    }
                }
            }
        }

        // bool HasVisibleFields(Type t)
        // {
        //     // 👉 기존 HasVisibleFields (있다면) 그대로
        // }

        // =====================================================================
        //  NESTED WINDOW: ContextAssetPickerWindow
        // =====================================================================

        /// <summary>
        /// Small picker window used to select a ScriptableObject context asset.
        /// </summary>
        sealed class ContextAssetPickerWindow : EditorWindow
        {
            public static void Show(
                Rect activatorRectGui,
                Action<ContextAsset> onPick,
                IReadOnlyCollection<Type>? disabledContextTypes,
                string title = "Add Context")
            {
                var w = CreateInstance<ContextAssetPickerWindow>();
                w._title = title;
                w._onPick = onPick;
                w._all = LoadAllAssets();
                w._disabledSet = disabledContextTypes != null
                    ? new HashSet<Type>(disabledContextTypes)
                    : new HashSet<Type>();

                var screen = GUIUtility.GUIToScreenRect(activatorRectGui);
                var size = new Vector2(520, 400);
                w.position = new Rect(screen.x, screen.yMax, size.x, size.y);
                w.ShowAsDropDown(screen, size);
                w.Focus();
            }

            string _title = "Add Context";
            private Action<ContextAsset>? _onPick;
            List<ContextAsset> _all = new();
            string _search = "";
            Vector2 _scroll;
            int _hover = -1;

            HashSet<Type> _disabledSet = new(); // 이미 붙어있는 컨텍스트 타입들

            static List<ContextAsset> LoadAllAssets()
            {
                var res = new List<ContextAsset>(64);
                foreach (var guid in AssetDatabase.FindAssets("t:ContextAsset"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var a = AssetDatabase.LoadAssetAtPath<ContextAsset>(path);
                    if (a) res.Add(a);
                }

                return res.OrderBy(a => a.name).ToList();
            }

            void OnGUI()
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                }

                var list = string.IsNullOrWhiteSpace(_search)
                    ? _all
                    : _all.Where(a => a.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = sv.scrollPosition;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var a = list[i];
                        var r = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));

                        if (r.Contains(Event.current.mousePosition)) _hover = i;
                        if (i == _hover) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.15f));

                        // 이 SO가 만들어줄 컨텍스트 타입 추론
                        var ctxType = TryResolveContextType(a);
                        bool disabled = ctxType != null &&
                                        _disabledSet.Any(t =>
                                            t == ctxType ||
                                            t.IsSubclassOf(ctxType) ||
                                            ctxType.IsSubclassOf(t));

                        var style = new GUIStyle(EditorStyles.label) { richText = true };
                        string path = AssetDatabase.GetAssetPath(a);

                        string label;
                        if (disabled)
                        {
                            // 이미 붙어있는 타입은 회색 + 안내
                            label =
                                $"<color=#777777>{a.name} </color>" +
                                $"<size=10><color=#555>[{path}]</color></size>";
                        }
                        else
                        {
                            label =
                                $"{a.name} <size=10><color=#888>[{path}]</color></size>";
                        }

                        using (new EditorGUI.DisabledScope(disabled))
                        {
                            EditorGUI.LabelField(r, label, style);

                            if (!disabled &&
                                Event.current.type == EventType.MouseDown &&
                                r.Contains(Event.current.mousePosition))
                            {
                                _onPick?.Invoke(a);
                                Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    if (list.Count == 0)
                    {
                        GUILayout.FlexibleSpace();
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("No ContextAsset found", EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
            }

            void OnLostFocus() => Close();

            // ContextAsset이 만들어내는 IContext 타입 추론
            static Type? TryResolveContextType(ContextAsset asset)
            {
                if (asset == null) return null;
                var aType = asset.GetType();

                // 1) ContextType 프로퍼티 관례
                var prop = aType.GetProperty("ContextType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (prop != null && typeof(Type).IsAssignableFrom(prop.PropertyType))
                {
                    var v = prop.GetValue(prop.GetGetMethod(true)?.IsStatic == true ? null : asset) as Type;
                    if (v != null && typeof(IContext).IsAssignableFrom(v))
                        return v;
                }

                // 2) GetContextType() 메서드 관례
                var mGet = aType.GetMethod("GetContextType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                    null, Type.EmptyTypes, null);
                if (mGet != null && typeof(Type).IsAssignableFrom(mGet.ReturnType))
                {
                    var v = mGet.Invoke(mGet.IsStatic ? null : asset, Array.Empty<object>()) as Type;
                    if (v != null && typeof(IContext).IsAssignableFrom(v))
                        return v;
                }

                // 3) Create/Build/Instantiate/Make/ToInstance 반환 타입으로 추론
                var names = new[] { "Create", "Build", "Instantiate", "Make", "ToInstance" };
                foreach (var name in names)
                {
                    var methods = aType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(mi => mi.Name == name);
                    foreach (var mi in methods)
                    {
                        var rt = mi.ReturnType;
                        if (typeof(IContext).IsAssignableFrom(rt))
                            return rt;
                    }
                }

                return null;
            }
        }
    }
}
#endif
