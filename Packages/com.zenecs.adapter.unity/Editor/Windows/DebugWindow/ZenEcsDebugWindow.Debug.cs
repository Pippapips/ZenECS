#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public enum EEntitySection
    {
        Components,
        Contexts,
        Binders,
    }

    public sealed partial class ZenEcsDebugWindow
    {
        private Dictionary<EEntitySection, List<string>> _debugMap = new()
        {
            [EEntitySection.Components] = new List<string>()
            {
                "AComponent",
                "BComponent",
                "CComponent",
            },
            [EEntitySection.Contexts] = new List<string>()
            {
                "AContext",
                "BContext",
                "CContext",
            },
            [EEntitySection.Binders] = new List<string>()
            {
                "ABinder",
                "BBinder",
                "CBinder",
            },
        };

        private readonly Dictionary<EEntitySection, bool> _debugGroupFold = new();

        private Vector2 _debugScroll;
        private bool _debugFold;

        readonly struct EntityTypeKey : IEquatable<EntityTypeKey>
        {
            public readonly int Id;
            public readonly int Gen;
            public readonly Type Type;

            public EntityTypeKey(Entity entity, Type type)
            {
                Id = entity.Id;
                Gen = entity.Gen;
                Type = type;
            }

            public bool Equals(EntityTypeKey other)
                => Id == other.Id && Gen == other.Gen && Type == other.Type;

            public override bool Equals(object? obj)
                => obj is EntityTypeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id;
                    hashCode = (hashCode * 397) ^ Gen;
                    hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private readonly Dictionary<EntityTypeKey, bool> _debugComponentFold = new();
        private readonly Dictionary<EntityTypeKey, bool> _debugContextFold = new();
        private readonly Dictionary<EntityTypeKey, bool> _debugBinderFold = new();

        void DrawRightDebug()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_debugScroll))
                {
                    _debugScroll = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        // 상단 Close 버튼
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(ZenStringTable.BtnClose, GUILayout.Width(80)))
                            {
                                return;
                            }
                        }

                        // 결과 표시
                        DrawDebugResult();
                    }
                }
            }
        }

        private void DrawDebugResult()
        {
            EditorGUILayout.LabelField("DebugView", ZenGUIStyles.LabelBold14);

            GUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUI.indentLevel = 0;

                DebugFoldoutHeader(ref _debugFold, "Entity", null, null, ZenGUIStyles.SystemFoldout);
                
                if (_debugFold)
                {
                    EditorGUI.indentLevel++;

                    var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
                    var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                    var rL2 = new Rect(rL1.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                    
                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                    var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                    if (GUI.Button(rL0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R2 clicked");
                    }

                    if (GUI.Button(rL1, "▼", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rL2, "▲", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }
                    
                    DebugDrawGroupLeaf(EEntitySection.Components, "Components", _debugMap, ZenGUIStyles.SystemFoldout);
                    DebugDrawGroupLeaf(EEntitySection.Contexts, "Contexts", _debugMap, ZenGUIStyles.SystemFoldout);
                    DebugDrawGroupLeaf(EEntitySection.Binders, "Binders", _debugMap, ZenGUIStyles.SystemFoldout);

                    EditorGUI.indentLevel++;
                }
            }
        }

        bool DebugFoldoutHeader(
            ref bool isOpen,
            string label,
            string? rightLabel = null,
            Action? rightGui = null,
            GUIStyle? style = null)
        {
            style ??= EditorStyles.foldoutHeader;

            isOpen = EditorGUILayout.Foldout(isOpen, label, true, style);

            if (!string.IsNullOrEmpty(rightLabel))
            {
                EditorGUILayout.LabelField(rightLabel, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            }

            rightGui?.Invoke();

            return isOpen;
        }

        void DebugDrawGroupLeaf(
            EEntitySection section,
            string label,
            Dictionary<EEntitySection, List<string>> map,
            GUIStyle foldStyle)
        {
            if (!map.TryGetValue(section, out var list) || list.Count == 0)
                return;

            bool open = _debugGroupFold.GetValueOrDefault(section, true);
            open = EditorGUILayout.Foldout(open, label, true, foldStyle);
            _debugGroupFold[section] = open;
            if (!open) return;

            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            if (section == EEntitySection.Components)
            {
                DebugDrawComponents();
            }
            else if (section == EEntitySection.Contexts)
            {
                DebugDrawContexts();
            }
            else if (section == EEntitySection.Binders)
            {
                DebugDrawBinders();
            }

            // foreach (string? s in list)
            //     DebugDrawSystemRow(s);

            EditorGUI.indentLevel = prevIndent;
        }

        void DebugDrawComponents()
        {
            if (_kernel == null || _world == null) return;
            int id = 1;
            int gen = 0;
            bool valid = _world.IsAlive(id, gen);
            if (!valid) return;

            {
                var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                    GUILayout.ExpandWidth(true));

                // Indent 반영
                rowRect = EditorGUI.IndentedRect(rowRect);

                const float iconW = 20f; // 돋보기 / X 공통 폭
                const float gap = 1f;

                var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
                var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                var rL2 = new Rect(rL1.x + gap + iconW, rowRect.y, iconW, rowRect.height);

                var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                if (GUI.Button(rL0, ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
                {
                    Debug.Log("R2 clicked");
                }

                if (GUI.Button(rL1, "▼", ZenGUIStyles.ButtonMCNormal10))
                {
                    Debug.Log("R1 clicked");
                }

                if (GUI.Button(rL2, "▲", ZenGUIStyles.ButtonMCNormal10))
                {
                    Debug.Log("R0 clicked");
                }
            }
            
            ZenGUIContents.DrawLine();

            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                var contents = _world.GetAllComponents(e).ToArray();
                foreach (var (t, boxed) in contents)
                {
                    var hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                    if (!hasFields || boxed == null)
                    {
                        var prevIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(foldoutName + " <no-fields>", ZenGUIStyles.LabelMLNormal10);
                        EditorGUI.indentLevel = prevIndent;
                    }
                    else
                    {
                        var key = new EntityTypeKey(e, t);
                        var isOpen = _debugComponentFold.GetValueOrDefault(key, false);
                        isOpen = EditorGUILayout.Foldout(isOpen, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        _debugComponentFold[key] = isOpen;

                        if (isOpen)
                        {
                            var prevIndent = EditorGUI.indentLevel;
                            EditorGUI.indentLevel++;
                            
                            using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
                            {
                                try
                                {
                                    object obj = CopyBox(boxed, t);
                                    float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                                    bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                                    var body = GUILayoutUtility.GetRect(0, bodyH, GUILayout.ExpandWidth(true));
                                    var bodyInner = new Rect(body.x, body.y, body.width, body.height + 4f);

                                    EditorGUI.BeginChangeCheck();
                                    ZenComponentFormGUI.DrawSmallForm(bodyInner, obj, t);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        _world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, obj));
                                    }
                                }
                                catch (KeyNotFoundException)
                                {
                                    // 컴포넌트 타입이 레지스트리에 없는 경우는 무시
                                }
                            }
                            
                            EditorGUI.indentLevel = prevIndent;
                        }
                    }

                    // === 한 줄 Rect 계산 ===
                    var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
                    var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                    var rL2 = new Rect(rL1.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                    
                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                    var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                    if (GUI.Button(rL2, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
                    {
                        Debug.Log("R2 clicked");
                    }

                    if (GUI.Button(rL1, "R", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rL0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }

                    ZenGUIContents.DrawLine();

                    // 아래쪽 여유
                    GUILayout.Space(2);
                }
            }
        }

        void DebugDrawContexts()
        {
            if (_kernel == null || _world == null) return;
            int id = 1;
            int gen = 0;
            bool valid = _world.IsAlive(id, gen);
            if (!valid) return;

            ZenGUIContents.DrawLine();
            
            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                var contents = _world.GetAllContexts(e);
                foreach (var (t, boxed) in contents)
                {
                    var ctxType = t;
                    var members = new List<(string name, Type type, Func<object?> getter)>();

                    // 필드
                    foreach (var f in ctxType.GetFields(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly))
                    {
                        if (Attribute.IsDefined(f, typeof(ZenEcsExplorerHiddenAttribute), true)) continue;
                        if (Attribute.IsDefined(f, typeof(HideInInspector), true)) continue;

                        var lf = f;
                        members.Add((
                            lf.Name,
                            lf.FieldType,
                            () => lf.GetValue(boxed)
                        ));
                    }

                    // 프로퍼티
                    foreach (var p in ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                            BindingFlags.DeclaredOnly))
                    {
                        if (!p.CanRead) continue;
                        if (p.GetIndexParameters().Length > 0) continue;
                        if (Attribute.IsDefined(p, typeof(ZenEcsExplorerHiddenAttribute), true)) continue;
                        if (Attribute.IsDefined(p, typeof(HideInInspector), true)) continue;

                        var lp = p;
                        members.Add((
                            lp.Name,
                            lp.PropertyType,
                            () =>
                            {
                                try
                                {
                                    return lp.GetValue(boxed);
                                }
                                catch
                                {
                                    return null;
                                }
                            }
                        ));
                    }

                    bool hasFields = members.Count > 0;
                    string ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    string foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                    if (!hasFields)
                    {
                        int prevIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(foldoutName + " <no-fields>", ZenGUIStyles.LabelMLNormal10);
                        EditorGUI.indentLevel = prevIndent;
                    }
                    else
                    {
                        var key = new EntityTypeKey(e, t);
                        var isOpen = _debugContextFold.GetValueOrDefault(key, false);
                        isOpen = EditorGUILayout.Foldout(isOpen, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        _debugContextFold[key] = isOpen;
                        if (isOpen)
                        {
                            int prevIndent = EditorGUI.indentLevel;
                            EditorGUI.indentLevel++;

                            using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
                            {
                                #region CONTENTS
                                foreach (var (ctxName, mType, getter) in members)
                                {
                                    object? value = null;
                                    try
                                    {
                                        value = getter();
                                    }
                                    catch
                                    {
                                        /* ignore */
                                    }

                                    var rect = GUILayoutUtility.GetRect(
                                        0,
                                        EditorGUIUtility.singleLineHeight,
                                        GUILayout.ExpandWidth(true));

                                    var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                                    var valRect = new Rect(
                                        rect.x + EditorGUIUtility.labelWidth,
                                        rect.y,
                                        rect.width - EditorGUIUtility.labelWidth,
                                        rect.height);

                                    EditorGUI.LabelField(labelRect, ctxName);

                                    if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                                    {
                                        var obj = value as UnityEngine.Object;
                                        var content = EditorGUIUtility.ObjectContent(obj, mType);

                                        // 링크 커서
                                        EditorGUIUtility.AddCursorRect(valRect, MouseCursor.Link);

                                        // 여기에서 LabelField 대신 Button으로 그린다 = hover 색 적용됨
                                        if (GUI.Button(valRect, content, ZenGUIStyles.LinkLabel))
                                        {
                                            if (obj != null)
                                            {
                                                Selection.activeObject = obj;
                                                EditorGUIUtility.PingObject(obj);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var text = value != null ? (value.ToString() ?? "null") : "null";
                                        EditorGUI.LabelField(valRect, text);
                                    }
                                }
                                #endregion
                            }

                            EditorGUI.indentLevel = prevIndent;
                        }
                    }
                    
                    // === 한 줄 Rect 계산 ===
                    var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                    var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                    if (GUI.Button(rR2, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
                    {
                        Debug.Log("R2 clicked");
                    }

                    if (GUI.Button(rR1, "R", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }

                    ZenGUIContents.DrawLine();

                    // 아래쪽 여유
                    GUILayout.Space(2);
                }
            }
        }

        void DebugDrawBinders()
        {
            if (_kernel == null || _world == null) return;
            int id = 1;
            int gen = 0;
            bool valid = _world.IsAlive(id, gen);
            if (!valid) return;

            ZenGUIContents.DrawLine();
            
            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                var contents = _world.GetAllBinders(e);
                foreach (var (t, boxed) in contents)
                {
                    var hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    string ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    string foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                    if (!hasFields)
                    {
                        int prevIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(foldoutName + " <no-fields>", ZenGUIStyles.LabelMLNormal10);
                        EditorGUI.indentLevel = prevIndent;
                    }
                    else
                    {
                        var key = new EntityTypeKey(e, t);
                        var isOpen = _debugBinderFold.GetValueOrDefault(key, false);
                        isOpen = EditorGUILayout.Foldout(isOpen, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        _debugBinderFold[key] = isOpen;
                        if (isOpen)
                        {
                            int prevIndent = EditorGUI.indentLevel;
                            EditorGUI.indentLevel++;

                            using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
                            {
                                #region CONTENTS
             
                                // apply order
                                // observing binds
                                
                                #endregion
                            }

                            EditorGUI.indentLevel = prevIndent;
                        }
                    }
                    
                    // === 한 줄 Rect 계산 ===
                    var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);
                    var rR2 = new Rect(rR1.x - gap - iconW, rowRect.y, iconW, rowRect.height);

                    if (GUI.Button(rR2, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
                    {
                        Debug.Log("R2 clicked");
                    }

                    if (GUI.Button(rR1, "R", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }

                    ZenGUIContents.DrawLine();

                    // 아래쪽 여유
                    GUILayout.Space(2);
                }
            }
        }

        #region EXPLORER_INTERNAL 

        static object CopyBox(object? src, Type t)
        {
            if (src == null) return SafeNew.New(t);
            if (t.IsValueType) return src;
            var dst = SafeNew.New(t);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(dst, f.GetValue(src));
            return dst;
        }

        static class SafeNew
        {
            public static object New(Type t)
            {
                if (t.IsValueType) return Activator.CreateInstance(t);
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null) return Activator.CreateInstance(t);
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
            }
        }
        
        #endregion

        #region SYSTEM_DEBUG
        
        bool DrawFoldoutWithRightButtons(
            ref bool isOpen,
            string label,
            Action rightButtonsGui,
            GUIStyle foldStyle)
        {
            var rowRect = GUILayoutUtility.GetRect(
                0,
                EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true));

            rowRect = EditorGUI.IndentedRect(rowRect);

            const float buttonWidth = 20f;
            const float gap = 2f;

            // 오른쪽 버튼들 자리 확보
            var rightRect = new Rect(
                rowRect.xMax - buttonWidth,
                rowRect.y,
                buttonWidth,
                rowRect.height);

            // foldout은 오른쪽 버튼 영역을 제외한 왼쪽만 사용
            var foldRect = new Rect(
                rowRect.x,
                rowRect.y,
                rowRect.width - buttonWidth - gap,
                rowRect.height);

            // toggleOnLabelClick은 true로 써도 괜찮음 (foldRect 안에서만 먹음)
            isOpen = EditorGUI.Foldout(foldRect, isOpen, label, true, foldStyle);

            // 오른쪽 버튼은 foldout rect와 안 겹치므로 클릭이 정상 작동
            GUILayout.BeginArea(rightRect);
            rightButtonsGui?.Invoke();
            GUILayout.EndArea();

            return isOpen;
        }

        void DebugDrawSystemRow(string s)
        {
            // === 한 줄 Rect 계산 ===
            var rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

            // Indent 반영
            rowRect = EditorGUI.IndentedRect(rowRect);

            const float iconW = 24f; // 돋보기 / X 공통 폭
            const float gap = 1f;

            var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
            var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
            var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y, iconW, rowRect.height);
            var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y, iconW, rowRect.height);

            // 가운데: System 버튼
            float sysX = rL1.xMax + gap;
            float sysRight = rR1.x - gap;
            float sysW = Mathf.Max(0f, sysRight - sysX);
            var sysRect = new Rect(sysX, rowRect.y, sysW, rowHeight);

            // ===== Pause (Enabled 토글) =====
            using (new EditorGUI.DisabledScope(false))
            {
                var oldBg = GUI.backgroundColor;
                var oldCont = GUI.contentColor;

                // bg/content color
                // if (false)
                // {
                //     GUI.backgroundColor = EditorGUIUtility.isProSkin
                //         ? new Color(0.24f, 0.48f, 0.90f, 1f)
                //         : new Color(0.20f, 0.45f, 0.90f, 1f);
                //     GUI.contentColor = Color.white;
                // }

                if (GUI.Button(rL0, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    Debug.Log("L0 clicked");
                }

                if (GUI.Button(rL1, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    Debug.Log("L1 clicked");
                }

                GUI.backgroundColor = oldBg;
                GUI.contentColor = oldCont;
            }

            bool selected = false;
            bool clicked = GUI.Toggle(sysRect, selected, s, ZenGUIStyles.ButtonMLNormal10);
            if (clicked && !selected)
            {
                ClearState();
                //_systemTree.SelectedSystemIndex = index;
            }

            if (GUI.Button(rR1, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
            {
                Debug.Log("R1 clicked");
            }

            using (new EditorGUI.DisabledScope(false))
            {
                if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                {
                    Debug.Log("R0 clicked");
                }
            }
        }
        
        #endregion
    }
}
