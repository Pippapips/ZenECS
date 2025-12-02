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

            DebugDrawComponents();
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
            
            // ===== Pause (Enabled 토글) =====
            using (new EditorGUI.DisabledScope(false))
            {
                var e = (Entity)Activator.CreateInstance(typeof(Entity), id, gen);
                var comps = _world.GetAllComponents(e).ToArray();
                foreach (var (t, boxed) in comps)
                {
                    string ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    string fname = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";
                    var key = new EntityTypeKey(e, t);
                    var isOpen = _debugComponentFold.GetValueOrDefault(key, true);
                    isOpen = EditorGUILayout.Foldout(isOpen, fname, false, ZenGUIStyles.SystemFoldout);
                    _debugComponentFold[key] = isOpen;
                    bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    if (!isOpen || !hasFields || boxed == null) continue;
                    
                    // === 한 줄 Rect 계산 ===
                    var rowHeight = EditorGUIUtility.singleLineHeight;
                    var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));

                    // Indent 반영
                    rowRect = EditorGUI.IndentedRect(rowRect);

                    const float iconW = 20f; // 돋보기 / X 공통 폭
                    const float gap = 1f;

                    var rL0 = new Rect(rowRect.x, rowRect.y, iconW, rowRect.height);
                    var rL1 = new Rect(rL0.x + gap + iconW, rowRect.y, iconW, rowRect.height);
                    var rR0 = new Rect(rowRect.xMax - iconW, rowRect.y - rowHeight + 4, iconW, rowRect.height);
                    var rR1 = new Rect(rR0.x - gap - iconW, rowRect.y - rowHeight + 4, iconW, rowRect.height);

                    // if (GUI.Button(rL0, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                    // {
                    //     Debug.Log("L0 clicked");
                    // }
                    // if (GUI.Button(rL1, ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                    // {
                    //     Debug.Log("L1 clicked");
                    // }
                    
                    if (GUI.Button(rR1, ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
                    {
                        Debug.Log("R1 clicked");
                    }

                    if (GUI.Button(rR0, "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        Debug.Log("R0 clicked");
                    }
                    
                    int prevIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;

                    try
                    {
                        object obj = CopyBox(boxed, t);
                        float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                        bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                        var body = GUILayoutUtility.GetRect(0, bodyH - rowHeight, GUILayout.ExpandWidth(true));
                        var bodyInner = new Rect(body.x, body.y - rowHeight + 4f, body.width, body.height + 4f);

                        EditorGUI.BeginChangeCheck();
                        ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _world.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, obj));
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        // 컴포넌트 타입이 레지스트리에 없는 경우는 무시
                    }

                    EditorGUI.indentLevel = prevIndent;

                    EditorGUILayout.Space(2);
                }
            }
        }
        
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
        
    }
}
