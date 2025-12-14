#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
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
                return ZenAssetDatabase.FindAndLoadAllAssets<ContextAsset>();
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
        
        private static void drawContextsMenus(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
                {
                    // var allContexts = ZenUtil.ContextTypeFinder.All();
                    // var disabledC = new HashSet<Type>(w.GetAllContexts(e).Select(c => c.type));
                    //
                    // ZenContextPickerWindow.Show(
                    //     allContextTypes: allContexts,
                    //     disabled: disabledC,
                    //     onPick: picked =>
                    //     {
                    //         var inst = ZenDefaults.CreateWithDefaults(picked);
                    //         if (inst != null)
                    //         {
                    //             switch (inst)
                    //             {
                    //                 case SharedContextAsset markerAsset:
                    //                 {
                    //                     var resolver = ZenEcsUnityBridge.SharedContextResolver;
                    //                     if (resolver != null)
                    //                     {
                    //                         var ctx = resolver.Resolve(markerAsset);
                    //                         if (ctx != null)
                    //                             w.RegisterContext(e, ctx);
                    //                     }
                    //
                    //                     break;
                    //                 }
                    //                 case PerEntityContextAsset perEntityAsset:
                    //                 {
                    //                     var ctx = perEntityAsset.Create();
                    //                     w.RegisterContext(e, ctx);
                    //                     break;
                    //                 }
                    //             }
                    //         }
                    //     },
                    //     activatorRectGui: rects[0],
                    //     title: $"Entity #{e.Id}:{e.Gen} - Add Context");

                    var ctxs = w.GetAllContexts(e);
                    var disabledCtxTypes = new HashSet<Type>(ctxs.Select(c => c.type));

                    ContextAssetPickerWindow.Show(
                        activatorRectGui: rects[0],
                        onPick: asset =>
                        {
                            switch (asset)
                            {
                                case SharedContextAsset markerAsset:
                                {
                                    var resolver = ZenEcsUnityBridge.SharedContextResolver;
                                    if (resolver != null)
                                    {
                                        var ctx = resolver.Resolve(markerAsset);
                                        if (ctx != null)
                                            w.RegisterContext(e, ctx);
                                    }

                                    break;
                                }
                                case PerEntityContextAsset perEntityAsset:
                                {
                                    var ctx = perEntityAsset.Create();
                                    w.RegisterContext(e, ctx);
                                    break;
                                }
                            }
                        },
                        disabledContextTypes: disabledCtxTypes,
                        title: $"Entity #{e.Id}:{e.Gen} - Add Context");
                }
            }

            if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.ExpandAll(EEntitySection.Contexts);
            }

            if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.CollapseAll(EEntitySection.Contexts);
            }
        }

        private static void drawContextMenus(IWorld w, Entity e, Type t, bool canEdit, ref EntityFoldoutInfo foldoutInfo, object? ctx = null, bool indent = false)
        {
            if (indent) EditorGUI.indentLevel++;

            var rects = new Rect[2];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Context",
                            $"Remove this context?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                            "Yes",
                            "No"))
                    {
                        if (ctx != null)
                        {
                            w.RemoveContext(e, (IContext)ctx);
                            foldoutInfo.RemoveFoldout(EEntitySection.Contexts, t);
                        }
                    }
                }
            }

            if (GUI.Button(rects[1], ZenGUIContents.IconPing(), EditorStyles.iconButton))
            {
                ZenUtil.PingType(t);
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawContexts(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var contents = w.GetAllContexts(e).ToArray();
            foreach (var (t, boxed) in contents)
            {
                var ctxType = t;
                var members = new List<(string name, Type type, Func<object?> getter)>();

                // 필드
                foreach (var f in ctxType.GetFields(BindingFlags.Public | BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly))
                {
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

                var hasFields = members.Count > 0;
                var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                if (!hasFields)
                {
                    EditorGUILayout.LabelField(foldoutName, ZenGUIStyles.LabelMLNormal10);
                    drawContextMenus(w, e, t, canEdit, ref foldoutInfo, boxed);
                }
                else
                {
                    var open = foldoutInfo.GetFoldout(EEntitySection.Contexts, t, false);
                    open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                    foldoutInfo.SetFoldout(EEntitySection.Contexts, t, open);

                    if (open)
                    {
                        drawContextContent(w, e, canEdit, members);
                    }

                    drawContextMenus(w, e, t, canEdit, ref foldoutInfo, boxed, true);
                }

                ZenGUIContents.DrawLine();

                GUILayout.Space(4);
            }
        }

        private static void drawContextContent(IWorld w, Entity e, bool canEdit, List<(string name, Type type, Func<object?> getter)> members)
        {
            var prevIndent = EditorGUI.indentLevel;
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
}
#endif