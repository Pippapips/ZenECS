#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Binders 섹션 전용 partial.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// Draws binders summary line + Add 버튼 + 리스트.
        /// </summary>
        void DrawEntityBindersSection(IWorld world, Entity e)
        {
            var bindersOk = BinderApi.TryGetAll(world, e, out var binders);
            if (!bindersOk)
            {
                EditorGUILayout.HelpBox(
                    "Binders API has been disconnected.",
                    MessageType.None);
                return;
            }

            var line = EditorGUIUtility.singleLineHeight;
            var r2 = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

            // Fold-all for binders (보이는 것만)
            var rArrow2 = new Rect(r2.x + 3, r2.y + 1, 18f, r2.height - 2);

            const float addW = 20f;
            const float gap = 6f;

            var rAdd = new Rect(r2.xMax - addW - 1.5f, r2.y + 2, addW, r2.height);
            var rLabel2 = new Rect(
                rArrow2.xMax - 1f,
                r2.y,
                r2.width - (rArrow2.width + addW + gap + 4f),
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
            using (new EditorGUI.DisabledScope(!_coreState.EditMode || !BinderApi.CanAdd(world)))
            {
                if (GUI.Button(rAdd, ZenGUIContents.IconPlus(), EditorStyles.iconButton))
                {
                    var allBinders = BinderTypeFinder.All();
                    var disabledB = new HashSet<Type>(binders.Select(x => x.type));

                    ZenBinderPickerWindow.Show(
                        allBinderTypes: allBinders,
                        disabled: disabledB,
                        onPick: picked =>
                        {
                            var inst = ZenDefaults.CreateWithDefaults(picked);
                            if (inst != null)
                                BinderApi.Add(world, e, inst);
                            Repaint();
                        },
                        activatorRectGui: rAdd,
                        title: $"Entity #{e.Id}:{e.Gen} - Add Binder");
                }
            }

            DrawBindersList(world, e, binders);
        }

        /// <summary>
        /// 모든 바인더의 foldout 상태를 일괄 변경.
        /// </summary>
        void SetAllBindersFold(IWorld world, Entity e, bool open)
        {
            if (!BinderApi.TryGetAll(world, e, out var arr)) return;
            foreach (var (t, _) in arr)
            {
                var key = new EntityTypeKey(e, t);
                _entityPanel.BinderFold[key] = open;
            }
        }

        /// <summary>
        /// Binder foldout + 메타 정보 렌더링.
        /// </summary>
        void DrawBindersList(
            IWorld world,
            Entity e,
            (Type type, object? boxed)[] bindersArray)
        {
            var line = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, boxed) in bindersArray)
                {
                    var key = new EntityTypeKey(e, t);
                    if (!_entityPanel.BinderFold.TryGetValue(key, out var openB))
                        openB = false;

                    _entityPanel.BinderFold[key] = openB;

                    bool hasBody = CanShowBinderBody(t, boxed);

                    var binder = boxed as IBinder;
                    bool isDisabled = binder is { Enabled: false };

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var headRectB = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));

                        var prevHeaderColor = GUI.color;
                        if (isDisabled)
                            GUI.color = new Color(0.65f, 0.65f, 0.65f);

                        ZenFoldoutHeader.DrawRow(
                            ref openB,
                            headRectB,
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
                                    // Remove Binder
                                    if (GUI.Button(rR0, "X", EditorStyles.miniButton))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                                "Remove Binder",
                                                $"Remove this binder?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                                "Yes",
                                                "No"))
                                        {
                                            BinderApi.Remove(world, e, t);
                                        }
                                    }

                                    // Reset Binder
                                    if (GUI.Button(rR1, "R", EditorStyles.miniButton))
                                    {
                                        var inst = ZenDefaults.CreateWithDefaults(t);
                                        if (inst != null)
                                        {
                                            BinderApi.Replace(world, e, inst);
                                        }
                                    }
                                }

                                // Ping binder type
                                if (GUI.Button(rR2, ZenGUIContents.IconPing(), EditorStyles.iconButton))
                                {
                                    PingSystemTypeNoSelect(t);
                                }
                            },
                            foldable: hasBody,
                            noMarginTitle: false);

                        GUI.color = prevHeaderColor;
                        _entityPanel.BinderFold[key] = openB;

                        if (!openB || !hasBody || boxed == null)
                            continue;

                        var prevBodyColor = GUI.color;
                        if (isDisabled)
                            GUI.color = new Color(0.85f, 0.85f, 0.85f);

                        DrawBinderMeta(world, e, boxed);

                        GUI.color = prevBodyColor;
                    }
                }
            }
        }

        /// <summary>
        /// Binder 메타 정보 (Priority + Observed IBind&lt;>) 렌더링.
        /// </summary>
        void DrawBinderMeta(IWorld world, Entity e, object binderObj)
        {
            var t = binderObj.GetType();
            var binder = binderObj as IBinder;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // --- Priority 편집(선택적으로) ---
                if (binder != null)
                {
                    int currentPriority = 0;
                    try
                    {
                        var pi = t.GetProperty("Priority",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        if (pi != null && pi.CanRead && pi.PropertyType == typeof(int))
                        {
                            currentPriority = (int)(pi.GetValue(binderObj) ?? 0);
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    int newPriority = currentPriority;
                    bool changedByButton = false;

                    var lineRect = GUILayoutUtility.GetRect(
                        10,
                        EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    const float btnW = 18f;
                    const float btnGap = 2f;
                    const float labelGap = 1f;
                    const float labelW = 90f;

                    var h = lineRect.height;

                    var minusRect = new Rect(lineRect.x, lineRect.y, btnW, h);
                    var plusRect = new Rect(minusRect.xMax + btnGap, lineRect.y, btnW, h);
                    var labelRect = new Rect(plusRect.xMax + labelGap - 8, lineRect.y, labelW, h);
                    var fieldRect = new Rect(
                        labelRect.xMax + labelGap,
                        lineRect.y,
                        lineRect.xMax - (labelRect.xMax + labelGap),
                        h);

                    if (GUI.Button(minusRect, "-"))
                    {
                        newPriority = currentPriority - 1;
                        changedByButton = true;
                    }

                    if (GUI.Button(plusRect, "+"))
                    {
                        newPriority = currentPriority + 1;
                        changedByButton = true;
                    }

                    EditorGUI.LabelField(labelRect, "Apply Order");

                    EditorGUI.BeginChangeCheck();
                    newPriority = EditorGUI.IntField(fieldRect, GUIContent.none, newPriority);
                    bool changedByField = EditorGUI.EndChangeCheck();

                    if ((changedByButton || changedByField) && newPriority != currentPriority)
                    {
                        try
                        {
                            var pi = t.GetProperty("Priority",
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                            if (pi != null && pi.CanWrite && pi.PropertyType == typeof(int))
                            {
                                pi.SetValue(binderObj, newPriority);
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }

                EditorGUILayout.Space(4f);

                // --- Observing (IBind<>) 메타 정보 ---
                EditorGUILayout.LabelField("Observing (IBinds)", EditorStyles.boldLabel);

                var observed = BinderIntrospection.ExtractObservedComponentTypes(t);

                using (new EditorGUI.IndentLevelScope())
                {
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
                                    OkLabel.DrawOK(20f);
                                else
                                    NotAssignLabel.Draw(20f);
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
