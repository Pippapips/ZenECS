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
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    /// <summary>
    /// Contexts 섹션 전용 partial.
    /// ContextApi 래퍼를 통해 World의 Context 관련 메서드를 리플렉션으로 호출합니다.
    /// </summary>
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// Draws the contexts summary line + Add 버튼 + readonly 리스트.
        /// </summary>
        void DrawEntityContextsSection(IWorld world, Entity e)
        {
            if (!ContextApi.TryGetAll(world, e, out var ctxs))
            {
                EditorGUILayout.HelpBox(
                    "Contexts API has been disconnected.",
                    MessageType.None);
                return;
            }

            var line = EditorGUIUtility.singleLineHeight;
            var rc = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));

            var rArrowC = new Rect(rc.x + 3, rc.y + 1, 18f, rc.height - 2);

            const float addW = 20f;
            const float gap = 6f;

            var rAddC = new Rect(rc.xMax - addW - 1.5f, rc.y + 2, addW, rc.height);
            var rLabelC = new Rect(
                rArrowC.xMax - 1f,
                rc.y,
                rc.width - (rArrowC.width + addW + gap + 4f),
                rc.height);

            // 모든 컨텍스트가 열려 있는지 (컴포넌트와 동일 패턴)
            var allOpen_ = AreAllContextsOpen(e, ctxs);

            EditorGUI.BeginChangeCheck();
            var visNextCtx = EditorGUI.Foldout(rArrowC, allOpen_, GUIContent.none, false);
            EditorGUIUtility.AddCursorRect(rArrowC, MouseCursor.Link);
            if (EditorGUI.EndChangeCheck())
            {
                SetAllContextsFold(world, e, visNextCtx);
                Repaint();
                GUIUtility.ExitGUI();
            }

            EditorGUI.LabelField(rLabelC, $"Contexts: {ctxs.Length}");

            // Add Context 버튼 (ContextAssetPickerWindow 사용)
            using (new EditorGUI.DisabledScope(!_coreState.EditMode))
            {
                if (GUI.Button(rAddC, ZenGUIContents.IconPlus(), EditorStyles.iconButton))
                {
                    var disabledCtxTypes = new HashSet<Type>(ctxs.Select(c => c.type));

                    ContextAssetPickerWindow.Show(
                        activatorRectGui: rAddC,
                        onPick: asset =>
                        {
                            ContextApi.AddFromAsset(world, e, asset);
                            Repaint();
                        },
                        disabledContextTypes: disabledCtxTypes,
                        title: $"Entity #{e.Id}:{e.Gen} - Add Context");
                }
            }

            DrawContextsList(world, e, ctxs);
        }

        /// <summary>
        /// 엔티티의 모든 컨텍스트 foldout 상태를 일괄 변경.
        /// </summary>
        void SetAllContextsFold(IWorld world, Entity e, bool open)
        {
            if (!ContextApi.TryGetAll(world, e, out var ctxs))
                return;

            foreach (var (t, _) in ctxs)
            {
                if (t == null) continue;
                var key = new EntityTypeKey(e, t);
                _entityPanel.ContextFold[key] = open;
            }
        }

        /// <summary>
        /// Context 리스트(읽기 전용 필드/프로퍼티)를 렌더링.
        /// </summary>
        void DrawContextsList(IWorld world, Entity e, (Type type, object? boxed)[] ctxs)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (t, inst) in ctxs)
                {
                    if (t == null) continue;

                    var key = new EntityTypeKey(e, t);
                    if (!_entityPanel.ContextFold.TryGetValue(key, out var openC))
                        openC = false;

                    _entityPanel.ContextFold[key] = openC;

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var line = EditorGUIUtility.singleLineHeight;
                        var headRect = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));

                        ZenFoldoutHeader.DrawRow(
                            ref openC,
                            headRect,
                            t.Name,
                            t.Namespace ?? string.Empty,
                            rRight =>
                            {
                                // 오른쪽에는 특별한 버튼 없음
                                GUILayout.Space(0f);
                            },
                            foldable: true,
                            noMarginTitle: false);

                        _entityPanel.ContextFold[key] = openC;

                        if (!openC || inst == null)
                            continue;

                        DrawContextFieldsReadonly(inst, t);
                    }
                }
            }
        }

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

        /// <summary>
        /// Context 객체의 public 필드/프로퍼티를 읽기 전용으로 출력.
        /// </summary>
        void DrawContextFieldsReadonly(object ctxInstance, Type ctxType)
        {
            if (ctxInstance == null) return;

            var members = new List<(string name, Type type, Func<object?> getter)>();

            // 필드
            foreach (var f in ctxType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(f, typeof(HideInInspector), true)) continue;

                var lf = f;
                members.Add((
                    lf.Name,
                    lf.FieldType,
                    () => lf.GetValue(ctxInstance)
                ));
            }

            // 프로퍼티
            foreach (var p in ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                            return lp.GetValue(ctxInstance);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                ));
            }

            if (members.Count == 0)
            {
                EditorGUILayout.LabelField("— (no public fields / properties)");
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var (name, mType, getter) in members)
                {
                    object? value = null;
                    try
                    {
                        value = getter();
                    }
                    catch
                    {
                        // ignore
                    }

                    var rect = GUILayoutUtility.GetRect(
                        10,
                        EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                    var valRect = new Rect(
                        rect.x + EditorGUIUtility.labelWidth,
                        rect.y,
                        rect.width - EditorGUIUtility.labelWidth,
                        rect.height);

                    EditorGUI.LabelField(labelRect, name);

                    if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                    {
                        var obj = value as UnityEngine.Object;
                        var content = EditorGUIUtility.ObjectContent(obj, mType);
                        EditorGUI.LabelField(valRect, content);
                    }
                    else
                    {
                        var text = value != null ? value.ToString() ?? "null" : "null";
                        EditorGUI.LabelField(valRect, text);
                    }
                }
            }
        }
    }
}
#endif
