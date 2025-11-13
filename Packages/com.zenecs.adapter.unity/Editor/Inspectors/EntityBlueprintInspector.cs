#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core.Binding;
using ZenECS.EditorCommon;

namespace ZenECS.EditorInspectors
{
    [CustomEditor(typeof(EntityBlueprint))]
    public sealed class EntityBlueprintInspector : Editor
    {
        // ───────── Components (BlueprintData) ─────────
        SerializedProperty _dataProp;        // _data
        SerializedProperty _compEntriesProp; // _data.entries
        ReorderableList _componentsList;

        // ───────── Contexts (managed reference) ─────────
        SerializedProperty _contextsProp; // _contexts (List<IContext>)
        ReorderableList _contextsList;

        // ───────── Binders (managed reference) ─────────
        SerializedProperty _bindersProp; // _binders (List<IBinder>)
        ReorderableList _bindersList;

        readonly Dictionary<string, bool> _fold = new();
        const float PAD = 6f;
        
        GUIStyle _obsMissingStyle;   // 회색 이탤릭
        bool _stylesReady;
        
        void OnEnable()
        {
            // Components
            _dataProp = serializedObject.FindProperty("_data");
            _compEntriesProp = _dataProp?.FindPropertyRelative("entries");
            if (_compEntriesProp != null && _compEntriesProp.isArray)
                BuildComponentsList();

            // Contexts
            _contextsProp = serializedObject.FindProperty("_contexts");
            if (_contextsProp != null && _contextsProp.isArray)
                BuildContextsList();

            // Binders
            _bindersProp = serializedObject.FindProperty("_binders");
            if (_bindersProp != null && _bindersProp.isArray)
                BuildBindersList();
        }
        
        void EnsureStylesReady()
        {
            if (_stylesReady) return;

            // OnGUI에서만 호출되도록 (Event.current != null 보장)
            var baseStyle = EditorStyles.miniLabel ?? EditorStyles.label ?? GUI.skin?.label ?? new GUIStyle();
            _obsMissingStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Italic,
                richText  = false,
                normal =
                {
                    textColor = Color.darkGray
                }
            };

            _stylesReady = true;
        }

        public override void OnInspectorGUI()
        {
            EnsureStylesReady(); // ← 반드시 가장 먼저

            serializedObject.Update();

            if (_componentsList != null)
            {
                EditorGUILayout.Space(3);
                _componentsList.DoLayoutList();
            }

            if (_contextsList != null)
            {
                EditorGUILayout.Space(8);
                _contextsList.DoLayoutList();
            }

            if (_bindersList != null)
            {
                EditorGUILayout.Space(8);
                _bindersList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // =====================================================================
        // Components (BlueprintData 그대로)
        // =====================================================================
        void BuildComponentsList()
        {
            _componentsList = new ReorderableList(serializedObject, _compEntriesProp, true, true, true, true);

            _componentsList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Components (BlueprintData)", EditorStyles.boldLabel);

            _componentsList.onAddDropdownCallback = (rect, list) =>
            {
                var all = ZenComponentPickerWindow.FindAllZenComponents().ToList();
                var disabled = new HashSet<Type>();

                for (int i = 0; i < _compEntriesProp.arraySize; i++)
                {
                    var tname = _compEntriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("typeName").stringValue;
                    var rt = BlueprintData.Resolve(tname);
                    if (rt != null) disabled.Add(rt);
                }

                ZenComponentPickerWindow.Show(
                    all, disabled,
                    onPick: pickedType =>
                    {
                        serializedObject.Update();
                        int idx = _compEntriesProp.arraySize;
                        _compEntriesProp.InsertArrayElementAtIndex(idx);
                        var elem = _compEntriesProp.GetArrayElementAtIndex(idx);
                        elem.FindPropertyRelative("typeName").stringValue = pickedType.AssemblyQualifiedName;

                        var inst = ZenDefaults.CreateWithDefaults(pickedType);
                        elem.FindPropertyRelative("json").stringValue = ComponentJson.Serialize(inst, pickedType);

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    },
                    activatorRectGui: rect,
                    title: "Add Component"
                );
            };

            _componentsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _compEntriesProp.arraySize)
                    _compEntriesProp.DeleteArrayElementAtIndex(rl.index);
            };

            _componentsList.elementHeightCallback = (index) =>
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(index);
                var tname = e.FindPropertyRelative("typeName").stringValue;
                var key = $"comp:{index}:{tname}";

                float headerH = EditorGUIUtility.singleLineHeight + 6f;
                bool open = _fold.TryGetValue(key, out var o) ? o : true;
                if (!open) return headerH;

                var t = BlueprintData.Resolve(tname);
                bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);
                if (!hasFields) return headerH;

                var jsonProp = e.FindPropertyRelative("json");
                var obj = ComponentJson.Deserialize(jsonProp.stringValue, t);
                float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                return headerH + 6f + Mathf.Max(0f, bodyH) + 6f;
            };

            _componentsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(index);
                var pType = e.FindPropertyRelative("typeName");
                var pJson = e.FindPropertyRelative("json");
                var t = BlueprintData.Resolve(pType.stringValue);

                var key = $"comp:{index}:{pType.stringValue}";
                if (!_fold.ContainsKey(key)) _fold[key] = true;

                bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);

                var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
                bool openBefore = _fold[key];
                bool openNow = EditorGUI.Foldout(rHead, openBefore, t != null ? t.Name : "(Missing Type)", true,
                    EditorStyles.foldoutHeader);
                _fold[key] = hasFields && openNow;

                if (!hasFields || !_fold[key]) return;

                var top = rHead.yMax + 6f;
                var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - 6f);

                if (t != null)
                {
                    var obj = ComponentJson.Deserialize(pJson.stringValue, t);
                    EditorGUI.BeginChangeCheck();
                    ZenComponentFormGUI.DrawObject(rBody, obj, t, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Edit Blueprint Component");
                        pJson.stringValue = ComponentJson.Serialize(obj, t);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }
            };
        }

        // =====================================================================
        // Contexts (managed reference: IContext 인스턴스 그대로 편집)
        // =====================================================================
        void BuildContextsList()
        {
            _contextsList = new ReorderableList(serializedObject, _contextsProp, true, true, true, true);
            _contextsList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Contexts (managed reference, shared per-entity)", EditorStyles.boldLabel);

            _contextsList.onAddDropdownCallback = (rect, list) =>
            {
                // 1) 타입 수집: IContext 파생 + 비추상 + 파라미터 없는 생성자
                IEnumerable<Type> AllContexts()
                    => UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(ZenECS.Core.Binding.IContext))
                        .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

                // 2) 이미 추가된 타입 비활성
                var disabled = new HashSet<Type>();
                for (int i = 0; i < _contextsProp.arraySize; i++)
                {
                    var p = _contextsProp.GetArrayElementAtIndex(i);
                    var inst = p?.managedReferenceValue;
                    if (inst != null) disabled.Add(inst.GetType());
                }

                // 3) Picker 팝업
                ZenContextPickerWindow.Show(
                    allContextTypes: AllContexts(),
                    disabled: disabled,
                    onPick: pickedType =>
                    {
                        serializedObject.Update();
                        int idx = _contextsProp.arraySize;
                        _contextsProp.InsertArrayElementAtIndex(idx);
                        var elem = _contextsProp.GetArrayElementAtIndex(idx);
                        elem.managedReferenceValue = Activator.CreateInstance(pickedType);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    },
                    activatorRectGui: rect,
                    title: "Add Context"
                );
            };

            _contextsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _contextsProp.arraySize)
                    _contextsProp.DeleteArrayElementAtIndex(rl.index);
            };

            _contextsList.elementHeightCallback = (index) =>
            {
                var p = _contextsProp.GetArrayElementAtIndex(index);
                float headerH = EditorGUIUtility.singleLineHeight + PAD;

                string key = $"ctx:{index}";
                bool open = _fold.TryGetValue(key, out var o) ? o : true;
                if (!open) return headerH;

                return headerH + PAD + EditorGUI.GetPropertyHeight(p, true) + PAD;
            };

            _contextsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _contextsProp.GetArrayElementAtIndex(index);
                var inst = p?.managedReferenceValue;
                string title = inst != null ? inst.GetType().Name : "(None)";

                string key = $"ctx:{index}";
                if (!_fold.ContainsKey(key)) _fold[key] = true;

                var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
                bool open = EditorGUI.Foldout(rHead, _fold[key], title, true, EditorStyles.foldoutHeader);
                _fold[key] = open;
                if (!open) return;

                var top = rHead.yMax + PAD;
                var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - PAD);

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rBody, p, includeChildren: true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            };
        }

        // =====================================================================
        // Binders (managed reference: 누락 컨텍스트 경고 배지 포함)
        // =====================================================================
        void BuildBindersList()
        {
            _bindersList = new ReorderableList(serializedObject, _bindersProp, true, true, true, true);
            _bindersList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Binders (managed reference)", EditorStyles.boldLabel);

            _bindersList.onAddDropdownCallback = (rect, list) =>
            {
                // 1) 전체 바인더 타입 수집
                IEnumerable<Type> AllBinders()
                    => UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(IBinder))
                        .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

                // 2) 이미 추가된 바인더 타입은 disabled로 표시
                var disabled = new HashSet<Type>();
                for (int i = 0; i < _bindersProp.arraySize; i++)
                {
                    var p = _bindersProp.GetArrayElementAtIndex(i);
                    var inst = p?.managedReferenceValue;
                    if (inst != null) disabled.Add(inst.GetType());
                }

                // 3) ZenBinderPickerWindow로 검색/선택 UI 표시
                ZenBinderPickerWindow.Show(
                    allBinderTypes: AllBinders(),
                    disabled: disabled,
                    onPick: pickedType =>
                    {
                        serializedObject.Update();
                        int idx = _bindersProp.arraySize;
                        _bindersProp.InsertArrayElementAtIndex(idx);
                        var elem = _bindersProp.GetArrayElementAtIndex(idx);
                        elem.managedReferenceValue = Activator.CreateInstance(pickedType);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    },
                    activatorRectGui: rect,
                    title: "Add Binder"
                );
            };

            _bindersList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _bindersProp.arraySize)
                    _bindersProp.DeleteArrayElementAtIndex(rl.index);
            };

            _bindersList.elementHeightCallback = (index) =>
            {
                var p = _bindersProp.GetArrayElementAtIndex(index);
                var inst = p?.managedReferenceValue;
                var t = inst?.GetType();
                float headerH = EditorGUIUtility.singleLineHeight + PAD;
                // // pill 라벨들 계산
                // var provided = GetProvidedContextTypes(_contextsProp);
                // var required = inst != null ? ExtractContextFieldTypes(inst) : Array.Empty<Type>();
                // var pillLabels = required.Select(rt => rt.Name).ToList();
                // float pillsH = CalcBadgesHeight(pillLabels, EditorGUIUtility.currentViewWidth - 40f);
                // if (pillsH < 18f) pillsH = 18f;
                float pillsH = 0f; // 컨텍스트 배지 없음
                string key = $"binder:{index}";
                bool open = _fold.TryGetValue(key, out var o) ? o : true;
                if (!open)
                    return headerH + pillsH + PAD;
                // 바디(필드 폼) + 관찰 메타(IBind) 영역
                float bodyH = EditorGUI.GetPropertyHeight(p, GUIContent.none, true);
                float metaH = 0f;
                if (t != null && ExtractObservedComponentTypes(t).Count > 0)
                    metaH = EditorGUIUtility.singleLineHeight * (1 + ExtractObservedComponentTypes(t).Count) - 30f;
                return headerH + PAD + pillsH + PAD + metaH + PAD + bodyH + PAD;
            };

            _bindersList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _bindersProp.GetArrayElementAtIndex(index);
                var inst = p?.managedReferenceValue;
                var t = inst?.GetType();
                string title = t != null ? t.Name : "(None)";
                string key = $"binder:{index}";
                if (!_fold.ContainsKey(key)) _fold[key] = true;
                // 1) 헤더
                var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
                bool open = EditorGUI.Foldout(rHead, _fold[key], title, true, EditorStyles.foldoutHeader);
                _fold[key] = open;
                // 2) pill 배지 (필요 컨텍스트 vs 제공 컨텍스트)
                var provided = GetProvidedContextTypes(_contextsProp);
                var required = inst != null ? ExtractContextFieldTypes(inst) : Array.Empty<Type>();
                //var badgeTop = rHead.yMax + 4f;
                //var rPills = new Rect(rect.x + 8, badgeTop, rect.width - 16, Mathf.Max(18f, rect.yMax - badgeTop));
                //DrawContextPills(rPills, required, provided);
                var rPills = new Rect(rect.x + 8, rHead.yMax, 0, 0); // 자리 없앰
                if (!open) return;
                // 3) Observing (IBinds) 메타 블럭
                
                // observed = IBind<T...>에서 뽑은 관찰 대상 타입 리스트
                var observed = t != null ? ExtractObservedComponentTypes(t) : Array.Empty<Type>();

                if (observed.Count > 0)
                {
                    // ✅ Blueprint에 실제로 들어있는 컴포넌트 타입: entries(typeName)에서 해석
                    var providedTypes = GetProvidedComponentTypesFromEntries(_compEntriesProp); // _data.entries
                    var providedSet   = new HashSet<Type>(providedTypes);

                    var yMeta = rPills.yMax + 6f;
                    var metaTitle = new Rect(rect.x + 12, yMeta, rect.width - 24, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(metaTitle, "Observing (IBinds)", EditorStyles.boldLabel);
                    yMeta = metaTitle.yMax + 2f;

                    foreach (var ct in observed)
                    {
                        var line = new Rect(rect.x + 16, yMeta, rect.width - 32, EditorGUIUtility.singleLineHeight);

                        // 정확 타입 매칭(보통 struct 컴포넌트) — 필요시 IsAssignableFrom으로 바꿔도 됨
                        bool added = providedSet.Contains(ct);
                        var style  = added ? EditorStyles.miniLabel : _obsMissingStyle;

                        if (added) EditorGUI.LabelField(line, $"• {ct.Name}", style);
                        else EditorGUI.LabelField(line, $"• {ct.Name} <not-assigned>", style);
                        yMeta += EditorGUIUtility.singleLineHeight + 1f;
                    }
                }
                
                // 4) 바디(바인더 필드 편집 폼)
                //var bodyTop = Mathf.Max(rPills.yMax, (observed.Count > 0 ? rPills.yMax + 40f : rPills.yMax)) + 6f;
                var bodyTop = rHead.yMax + (observed.Count > 0 ? 40f : 6f);
                var rBody = new Rect(rect.x + 8, bodyTop, rect.width - 16, rect.yMax - bodyTop - PAD);
                EditorGUI.BeginChangeCheck();
                //EditorGUI.PropertyField(rBody, p, includeChildren: true);
                DrawBinderBody(rBody, p);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            };
        }

        // =====================================================================
        // Badge helpers
        // =====================================================================
        static List<Type> GetProvidedContextTypes(SerializedProperty contextsProp)
        {
            var list = new List<Type>();
            if (contextsProp == null || !contextsProp.isArray) return list;

            for (int i = 0; i < contextsProp.arraySize; i++)
            {
                var p = contextsProp.GetArrayElementAtIndex(i);
                var inst = p?.managedReferenceValue;
                if (inst != null) list.Add(inst.GetType());
            }

            return list;
        }

        static bool IsSatisfied(Type required, IEnumerable<Type> providedTypes)
        {
            foreach (var pt in providedTypes)
                if (required.IsAssignableFrom(pt))
                    return true;
            return false;
        }

        // === Binder 메타 추출: IBind<T...> 에서 관찰 컴포넌트 타입 수집 ===
        static IReadOnlyList<Type> ExtractObservedComponentTypes(Type binderType)
        {
            static bool IsBindsInterface(Type t)
                => t.IsInterface && t.IsGenericType && t.Name.StartsWith("IBind", StringComparison.Ordinal);

            var set = new HashSet<Type>();
            foreach (var itf in binderType.GetInterfaces())
            {
                if (!IsBindsInterface(itf)) continue;
                foreach (var ga in itf.GetGenericArguments())
                {
                    if (ga.IsAbstract) continue;
                    if (ga.Namespace?.EndsWith(".Editor", StringComparison.Ordinal) == true) continue;
                    set.Add(ga);
                }
            }
            return set.OrderBy(t => t.Name).ToArray();
        }

// === Binder에서 [Context] 필드 타입 수집 ===
        static Type[] ExtractContextFieldTypes(object binderInstance)
        {
            if (binderInstance == null) return Array.Empty<Type>();
            var t = binderInstance.GetType();
            var list = new List<Type>(8);

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // [Context] 특성 붙은 필드만 인정
                var hasAttr = f.GetCustomAttributes(inherit: true)
                    .Any(a => a.GetType().Name is "ContextAttribute");
                if (!hasAttr) continue;

                var ft = f.FieldType;
                if (typeof(ZenECS.Core.Binding.IContext).IsAssignableFrom(ft))
                    list.Add(ft);
            }
            return list.Distinct().ToArray();
        }

// === pill 배지 한 줄(또는 래핑) 높이 계산 ===
        static float CalcBadgesHeight(IReadOnlyList<string> labels, float width, float minPill = 60f)
        {
            if (labels == null || labels.Count == 0) return 18f;
            float h = 18f, x = 0f, pad = 4f;
            foreach (var s in labels)
            {
                var w = Mathf.Clamp(GUI.skin.label.CalcSize(new GUIContent(s)).x + 16f, minPill, width);
                if (x + w > width)
                {
                    h += 20f;
                    x = 0f;
                }
                x += w + pad;
            }
            return h;
        }

// === “필요 컨텍스트” pill 배지 그리기 (초록=있음 / 빨강=없음) ===
        static void DrawContextPills(Rect area, Type[] required, IReadOnlyList<Type> providedTypes)
        {
            // const float rowH = 18f;
            // float x = area.x, y = area.y, pad = 4f;
            //
            // if (required == null || required.Length == 0)
            // {
            //     GUI.Label(new Rect(x, y, area.width, rowH), "No [Context] fields", EditorStyles.miniLabel);
            //     return;
            // }
            //
            // foreach (var req in required)
            // {
            //     string label = req.Name;
            //     bool ok = IsSatisfied(req, providedTypes);
            //
            //     var content = new GUIContent(label, ok ? "Context present" : "Missing context");
            //     var size = GUI.skin.label.CalcSize(content);
            //     var w = Mathf.Clamp(size.x + 16f, 60f, area.width);
            //
            //     if (x + w > area.xMax)
            //     {
            //         x = area.x;
            //         y += rowH + 2f;
            //     }
            //
            //     var r = new Rect(x, y, w, rowH);
            //     var bg = ok ? new Color(0.20f, 0.65f, 0.20f, 0.22f) : new Color(0.85f, 0.25f, 0.25f, 0.22f);
            //     EditorGUI.DrawRect(r, bg);
            //     var outline = new Color(0f, 0f, 0f, 0.15f);
            //     EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), outline);
            //     EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), outline);
            //     EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), outline);
            //     EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), outline);
            //
            //     GUI.Label(r, content, EditorStyles.miniBoldLabel);
            //     x += w + pad;
            // }
        }

        void DrawBinderBody(Rect area, SerializedProperty root)
        {
            var y = area.y;
            var x = area.x;
            var w = area.width;

            // root의 끝 위치 잡기
            var iter = root.Copy();
            var end = root.GetEndProperty();

            // 첫 NextVisible(true)로 루트 진입 → 이후부터 자식만 렌더
            bool enterChildren = true;
            while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
            {
                // 루트 자체("Element 0")는 이미 건너뛰었으니 전부 자식
                float h = EditorGUI.GetPropertyHeight(iter, GUIContent.none, true);
                var r = new Rect(x, y, w, h);
                EditorGUI.PropertyField(r, iter, GUIContent.none, true);
                y += h + 2f;
                enterChildren = false;
            }
        }

        // BlueprintData.Resolve와 동일 컨셉의 로컬 리졸브
        static Type ResolveTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            var t = Type.GetType(typeName, throwOnError: false);
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var tt = asm.GetType(typeName, false);
                if (tt != null) return tt;
            }
            return null;
        }

        // _data.entries(SerializedProperty)에서 제공된 컴포넌트 타입 수집
        static IReadOnlyList<Type> GetProvidedComponentTypesFromEntries(SerializedProperty compEntriesProp)
        {
            if (compEntriesProp == null || !compEntriesProp.isArray) return Array.Empty<Type>();

            var list = new List<Type>(compEntriesProp.arraySize);
            for (int i = 0; i < compEntriesProp.arraySize; i++)
            {
                var e = compEntriesProp.GetArrayElementAtIndex(i);
                var typeNameProp = e.FindPropertyRelative("typeName");
                var tn = typeNameProp != null ? typeNameProp.stringValue : null;
                var t = ResolveTypeName(tn);
                if (t != null) list.Add(t);
            }
            return list;
        }


        // 블루프린트에 현재 추가된 컴포넌트 타입 수집
        static IReadOnlyList<Type> GetProvidedComponentTypes(SerializedProperty componentsProp)
        {
            if (componentsProp == null || !componentsProp.isArray) return Array.Empty<Type>();
            var list = new List<Type>(componentsProp.arraySize);
            for (int i = 0; i < componentsProp.arraySize; i++)
            {
                var e = componentsProp.GetArrayElementAtIndex(i);
                var val = e.managedReferenceValue;
                if (val != null) list.Add(val.GetType());
            }
            return list;
        }

    }
}
#endif