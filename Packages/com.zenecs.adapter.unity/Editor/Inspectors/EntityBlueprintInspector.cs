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
        SerializedProperty _dataProp; // _data
        SerializedProperty _compEntriesProp; // _data.entries
        ReorderableList _componentsList;

        // ───────── Contexts (ContextAsset refs) ─────────
        SerializedProperty _contextsProp; // _contextAssets (List<ContextAsset>)
        ReorderableList _contextsList;

        // ───────── Binders (managed reference) ─────────
        SerializedProperty _bindersProp; // _binders (List<IBinder>)
        ReorderableList _bindersList;

        readonly Dictionary<string, bool> _fold = new();
        const float PAD = 6f;

        GUIStyle _obsOkStyle; // Assigned된 컴포넌트 이름
        GUIStyle _obsMissingStyle; // Not-assigned 컴포넌트 이름
        GUIStyle _obsCheckStyle; // ✔ 아이콘
        GUIStyle _obsXStyle; // ✕ 아이콘
        GUIStyle _namespaceStyle;
        bool _stylesReady;

        void OnEnable()
        {
            // Components
            _dataProp = serializedObject.FindProperty("_data");
            _compEntriesProp = _dataProp?.FindPropertyRelative("entries");
            if (_compEntriesProp != null && _compEntriesProp.isArray)
                BuildComponentsList();

            // Contexts (SO refs: _contextAssets)
            _contextsProp = serializedObject.FindProperty("_contextAssets");
            if (_contextsProp != null && _contextsProp.isArray)
                BuildContextsList();

            // Binders (managed reference)
            _bindersProp = serializedObject.FindProperty("_binders");
            if (_bindersProp != null && _bindersProp.isArray)
                BuildBindersList();
        }

        void EnsureStylesReady()
        {
            if (_stylesReady) return;

            var baseStyle = EditorStyles.miniLabel ?? EditorStyles.label ?? GUI.skin?.label ?? new GUIStyle();

            _obsMissingStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Italic,
                richText = false,
                normal = { textColor = Color.gray }
            };

            _obsOkStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Normal,
                richText = false,
                normal = { textColor = Color.white }
            };

            _obsCheckStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true
            };

            _obsXStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true
            };

            // 네임스페이스 스타일
            _namespaceStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Normal,
                richText = false,
            };

            // ProSkin(다크테마)일 때는 밝은 회색
            if (EditorGUIUtility.isProSkin)
            {
                _namespaceStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f); // #C7C7C7 정도
            }
            else
            {
                // 라이트 테마에서는 중간 정도 회색
                _namespaceStyle.normal.textColor = new Color(0.25f, 0.25f, 0.25f);
            }

            _stylesReady = true;
        }

        public override void OnInspectorGUI()
        {
            ZenEcsEditorHeader.DrawHeader(
                "Entity Blueprint",
                "Defines components, contexts, and binders used to spawn a fully configured entity.",
                new[]
                {
                    "Runtime Blueprint",
                    "Components + Contexts + Binders"
                }
            );
            
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
        // Helpers: expand/collapse all
        // =====================================================================
        void SetComponentsFoldAll(bool open)
        {
            if (_compEntriesProp == null || !_compEntriesProp.isArray)
                return;

            for (int i = 0; i < _compEntriesProp.arraySize; i++)
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(i);
                var tname = e.FindPropertyRelative("typeName").stringValue;
                var key = $"comp:{i}:{tname}";
                _fold[key] = open;
            }
        }

        void SetBindersFoldAll(bool open)
        {
            if (_bindersProp == null || !_bindersProp.isArray)
                return;

            for (int i = 0; i < _bindersProp.arraySize; i++)
            {
                var key = $"binder:{i}";
                _fold[key] = open;
            }
        }

        // =====================================================================
        // Components (BlueprintData 그대로)
        // =====================================================================
        void BuildComponentsList()
        {
            _componentsList = new ReorderableList(serializedObject, _compEntriesProp, true, true, true, true);

            _componentsList.drawHeaderCallback = r =>
            {
                const float buttonWidth = 24f;
                const float buttonGap = 2f;

                var labelRect = new Rect(r.x, r.y, r.width - (buttonWidth * 2f + buttonGap * 2f), r.height);
                EditorGUI.LabelField(labelRect, "Components (BlueprintData)", EditorStyles.boldLabel);

                var expandRect = new Rect(r.xMax - (buttonWidth * 2f + buttonGap), r.y, buttonWidth, r.height);
                var collapseRect = new Rect(r.xMax - buttonWidth, r.y, buttonWidth, r.height);

                if (GUI.Button(expandRect, new GUIContent("▼", "Expand all component entries"), EditorStyles.miniButton))
                {
                    SetComponentsFoldAll(true);
                }

                if (GUI.Button(collapseRect, new GUIContent("▲", "Collapse all component entries"), EditorStyles.miniButton))
                {
                    SetComponentsFoldAll(false);
                }
            };

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

                // 네임스페이스 한 줄 높이
                float nsH = 0f;
                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                {
                    // 1줄 + 위/아래 여백 약간
                    nsH = EditorGUIUtility.singleLineHeight + 4f;
                }

                var jsonProp = e.FindPropertyRelative("json");
                var obj = ComponentJson.Deserialize(jsonProp.stringValue, t);
                float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);

                return headerH // Foldout 헤더
                       + nsH // 네임스페이스 라인
                       + 6f // 헤더/네임스페이스와 바디 사이 여백
                       + Mathf.Max(0f, bodyH)
                       + 6f; // 마지막 여백
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

                // ─ 헤더: Foldout + Reset(R) + Ping(돋보기) ─
                float line = EditorGUIUtility.singleLineHeight;
                var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, line);

                const float btnW = 20f; // Reset / Ping 둘 다 같은 폭

                // 오른쪽 끝: 돋보기
                var rPing = new Rect(rHead.xMax - btnW, rHead.y, btnW, rHead.height);
                // 그 왼쪽: Reset 버튼 (R)
                var rReset = new Rect(rHead.xMax - btnW * 2f - 2f, rHead.y, btnW, rHead.height);
                // 나머지 영역: Foldout
                var rFold = new Rect(
                    rHead.x,
                    rHead.y,
                    rHead.width - (btnW * 2f + 6f),
                    rHead.height
                );

                string label = t != null ? t.Name : "(Missing Type)";

                bool openBefore = _fold[key];
                bool openNow = EditorGUI.Foldout(
                    rFold,
                    openBefore,
                    label,
                    true,
                    EditorStyles.foldoutHeader
                );
                _fold[key] = hasFields && openNow;

                // Reset(R) 버튼: 디폴트 값으로 초기화
                using (new EditorGUI.DisabledScope(t == null))
                {
                    if (GUI.Button(rReset, new GUIContent("R", "Reset component to defaults"),
                            EditorStyles.miniButton) && t != null)
                    {
                        Undo.RecordObject(target, "Reset Blueprint Component");

                        // ZenDefaults로 새 인스턴스 생성 후 다시 JSON 직렬화
                        var inst = ZenDefaults.CreateWithDefaults(t);
                        pJson.stringValue = ComponentJson.Serialize(inst, t);

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }

                // 이름 옆 Ping 버튼 (돋보기)
                using (new EditorGUI.DisabledScope(t == null))
                {
                    var gcPing = GetSearchIconContent("Ping component script in Project");
                    if (GUI.Button(rPing, gcPing, EditorStyles.iconButton) && t != null)
                    {
                        PingTypeSource(t);
                    }
                }

                if (!hasFields || !_fold[key]) return;

                // ─ 네임스페이스 + 바디는 기존 코드 유지 ─
                float y = rHead.yMax;

                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                {
                    EnsureStylesReady(); // _namespaceStyle 사용
                    y += 2f;
                    var nsRect = new Rect(rect.x + 8, y, rect.width - 16, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(nsRect, t.Namespace, _namespaceStyle);
                    y += EditorGUIUtility.singleLineHeight + 2f;
                }
                else
                {
                    y += 6f;
                }

                var rBody = new Rect(rect.x + 8, y, rect.width - 16, rect.yMax - y - 6f);

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
        // Contexts (ContextAsset refs)
        // =====================================================================
        void BuildContextsList()
        {
            _contextsList = new ReorderableList(serializedObject, _contextsProp, true, true, true, true);

            _contextsList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Contexts (Context Assets)", EditorStyles.boldLabel);

            // 간단히: onAdd는 빈 슬롯 하나 추가하고, 사용자가 직접 SO를 드래그/선택
            _contextsList.onAddCallback = rl =>
            {
                int idx = _contextsProp.arraySize;
                _contextsProp.InsertArrayElementAtIndex(idx);
                var elem = _contextsProp.GetArrayElementAtIndex(idx);
                elem.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            };

            _contextsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _contextsProp.arraySize)
                    _contextsProp.DeleteArrayElementAtIndex(rl.index);
            };

            _contextsList.elementHeightCallback = index =>
            {
                float line = EditorGUIUtility.singleLineHeight;
                float h = line; // ObjectField

                // var p = _contextsProp.GetArrayElementAtIndex(index);
                // var obj = p.objectReferenceValue;
                // if (obj != null)
                // {
                //     var path = AssetDatabase.GetAssetPath(obj);
                //     if (!string.IsNullOrEmpty(path))
                //     {
                //         h += line + 4f; // 경로 한 줄
                //     }
                //     else
                //     {
                //         h += line + 4f; // "(Not saved asset)" 한 줄
                //     }
                // }

                return h + 4f;
            };

            _contextsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _contextsProp.GetArrayElementAtIndex(index);
                var obj = p.objectReferenceValue;

                float line = EditorGUIUtility.singleLineHeight;

                // 1) Context SO ObjectField
                var rField = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, line);
                EditorGUI.PropertyField(rField, p, GUIContent.none);

                if (obj == null)
                    return;

                // // 2) SO 경로 라인
                // var path = AssetDatabase.GetAssetPath(obj);
                // if (string.IsNullOrEmpty(path))
                // {
                //     path = "(Not saved asset)";
                // }
                // else
                // {
                //     path = $"[{path}]";
                // }
                //
                // // 필드 시작 위치와 맞추고, 짙은 회색 miniLabel 스타일
                // var rPath = new Rect(rect.x + 8, rField.yMax + 2f, rect.width - 16, line);
                //
                // var prevColor = GUI.color;
                // GUI.color = new Color(0.45f, 0.45f, 0.45f); // 짙은 회색
                // EditorGUI.LabelField(rPath, path, EditorStyles.miniLabel);
                // GUI.color = prevColor;
            };
        }

        // =====================================================================
        // Binders (managed reference: 누락 컨텍스트 경고 배지 포함)
        // =====================================================================
        void BuildBindersList()
        {
            _bindersList = new ReorderableList(serializedObject, _bindersProp, true, true, true, true);
            _bindersList.drawHeaderCallback = r =>
            {
                const float buttonWidth = 24f;
                const float buttonGap = 2f;

                var labelRect = new Rect(r.x, r.y, r.width - (buttonWidth * 2f + buttonGap * 2f), r.height);
                EditorGUI.LabelField(labelRect, "Binders (managed reference)", EditorStyles.boldLabel);

                var expandRect = new Rect(r.xMax - (buttonWidth * 2f + buttonGap), r.y, buttonWidth, r.height);
                var collapseRect = new Rect(r.xMax - buttonWidth, r.y, buttonWidth, r.height);

                if (GUI.Button(expandRect, new GUIContent("▼", "Expand all binder entries"), EditorStyles.miniButton))
                {
                    SetBindersFoldAll(true);
                }

                if (GUI.Button(collapseRect, new GUIContent("▲", "Collapse all binder entries"), EditorStyles.miniButton))
                {
                    SetBindersFoldAll(false);
                }
            };

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

            _bindersList.elementHeightCallback = index =>
            {
                const float pad = 6f;
                float line = EditorGUIUtility.singleLineHeight;

                if (_bindersProp == null || index < 0 || index >= _bindersProp.arraySize)
                    return line + pad * 2f;

                var p = _bindersProp.GetArrayElementAtIndex(index);
                var inst = p?.managedReferenceValue;
                var t = inst?.GetType();

                float headerH = line + pad; // 헤더 한 줄

                string key = $"binder:{index}";
                bool open = _fold.TryGetValue(key, out var o) ? o : true;
                if (!open)
                    return headerH + pad;

                // 네임스페이스 한 줄
                float nsH = 0f;
                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                    nsH = line + 4f;
                else
                    nsH = pad;

                // Priority / AttachOrder 두 줄
                float orderH = 0f;
                if (inst is IBinder) orderH += line;
                if (inst is IAttachOrderMarker) orderH += line + 4f; // 약간 여백

                // Observing(IBinds) 메타 높이
                float metaH = 0f;
                var observed = t != null ? ExtractObservedComponentTypes(t) : Array.Empty<Type>();
                if (observed.Count > 0)
                {
                    // 제목 한 줄 + 항목들
                    metaH = line; // "Observing (IBinds)"
                    metaH += observed.Count * (line + 1f); // 각 컴포넌트 라인
                    metaH += pad;
                }

                float bodyH = EditorGUI.GetPropertyHeight(p, GUIContent.none, true);

                return headerH + pad + nsH + orderH + metaH + bodyH + pad;
            };

            _bindersList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _bindersProp.GetArrayElementAtIndex(index);
                var inst = p?.managedReferenceValue;
                var t = inst?.GetType();
                string title = t != null ? t.Name : "(None)";
                string key = $"binder:{index}";
                if (!_fold.ContainsKey(key)) _fold[key] = true;

                float line = EditorGUIUtility.singleLineHeight;
                float x = rect.x;
                float y = rect.y + 3f;
                float w = rect.width;

                // IBinder / IAttachOrderMarker 캐스팅 (아래에서도 쓸 거라 미리 캐스팅)
                var binder = inst as IBinder;
                var marker = inst as IAttachOrderMarker;

                // ─ 1) 헤더: Foldout + Reset(R) + 돋보기 Ping ─
                var rHead = new Rect(x + 4f, y, w - 8f, line);

                const float btnW = 20f;
                // 오른쪽 끝: 돋보기
                var rPing = new Rect(rHead.xMax - btnW, rHead.y, btnW, rHead.height);
                // 그 왼쪽: Reset(R)
                var rReset = new Rect(rHead.xMax - btnW * 2f - 2f, rHead.y, btnW, rHead.height);
                // 나머지: Foldout
                var rFold = new Rect(rHead.x, rHead.y, rHead.width - (btnW * 2f + 6f), rHead.height);

                bool open = EditorGUI.Foldout(rFold, _fold[key], title, true, EditorStyles.foldoutHeader);
                _fold[key] = open;

                // Reset 버튼: IBinder.Reset(withPriority: true)
                using (new EditorGUI.DisabledScope(binder == null))
                {
                    if (GUI.Button(rReset, new GUIContent("R", "Reset binder (including priority)"),
                            EditorStyles.miniButton)
                        && binder != null)
                    {
                        binder.ResetApplyOrderAndAttachOrder();
                        Repaint();
                    }
                }

                // 돋보기 Ping 버튼: Binder 소스 Ping
                using (new EditorGUI.DisabledScope(t == null))
                {
                    var gcPing = GetSearchIconContent("Ping binder script in Project");
                    if (GUI.Button(rPing, gcPing, EditorStyles.iconButton) && t != null)
                    {
                        PingTypeSource(t); // Project에서 해당 스크립트 Ping만
                    }
                }

                if (!open) return;

                y = rHead.yMax;

                // ─ 2) 네임스페이스 한 줄 (Components 섹션과 동일 스타일) ─
                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                {
                    EnsureStylesReady(); // _namespaceStyle 준비
                    y += 2f;
                    var nsRect = new Rect(rect.x + 8f, y, rect.width - 16f, line);
                    EditorGUI.LabelField(nsRect, t.Namespace, _namespaceStyle);
                    y += line + 2f;
                }
                else
                {
                    y += 6f;
                }

                // ─ 3) Priority / AttachOrder 편집 (+/- 버튼 포함) ─
                // 현재 값
                int curPriority = binder != null ? binder.ApplyOrder : 0;
                int curAttach = marker != null ? marker.AttachOrder : 0;
                int newPriority = curPriority;
                int newAttach = curAttach;
                bool changedOrder = false;

                const float btnWidth = 18f;
                const float btnGap = 2f;

                if (marker != null)
                {
                    var rAttachBase = new Rect(rect.x + 12f, y, rect.width - 24f, line);

                    var rAttachField = new Rect(
                        rAttachBase.x,
                        rAttachBase.y,
                        rAttachBase.width - (btnWidth * 2f + btnGap * 2f),
                        rAttachBase.height
                    );
                    var rAttachMinus = new Rect(rAttachField.xMax + btnGap, rAttachBase.y, btnWidth, rAttachBase.height);
                    var rAttachPlus = new Rect(rAttachMinus.xMax + btnGap, rAttachBase.y, btnWidth, rAttachBase.height);

                    EditorGUI.BeginChangeCheck();
                    newAttach = EditorGUI.IntField(
                        rAttachField,
                        new GUIContent("Attach Order", "Bind order among binders (lower attaches first)"),
                        newAttach
                    );
                    if (EditorGUI.EndChangeCheck())
                        changedOrder = true;

                    if (GUI.Button(rAttachMinus, "-", EditorStyles.miniButton))
                    {
                        newAttach -= 1;
                        changedOrder = true;
                    }

                    if (GUI.Button(rAttachPlus, "+", EditorStyles.miniButton))
                    {
                        newAttach += 1;
                        changedOrder = true;
                    }

                    y += line;
                }

                if (binder != null)
                {
                    var rPriBase = new Rect(rect.x + 12f, y, rect.width - 24f, line);

                    // IntField 영역: 오른쪽에 버튼 두 개 자리 남겨두기
                    var rPriField = new Rect(
                        rPriBase.x,
                        rPriBase.y,
                        rPriBase.width - (btnWidth * 2f + btnGap * 2f),
                        rPriBase.height
                    );
                    var rPriMinus = new Rect(rPriField.xMax + btnGap, rPriBase.y, btnWidth, rPriBase.height);
                    var rPriPlus = new Rect(rPriMinus.xMax + btnGap, rPriBase.y, btnWidth, rPriBase.height);

                    // 숫자 입력
                    EditorGUI.BeginChangeCheck();
                    newPriority = EditorGUI.IntField(
                        rPriField,
                        new GUIContent("Apply Order", "Apply order (lower runs first)"),
                        newPriority
                    );
                    if (EditorGUI.EndChangeCheck())
                        changedOrder = true;

                    // -1 버튼
                    if (GUI.Button(rPriMinus, "-", EditorStyles.miniButton))
                    {
                        newPriority -= 1;
                        changedOrder = true;
                    }

                    // +1 버튼
                    if (GUI.Button(rPriPlus, "+", EditorStyles.miniButton))
                    {
                        newPriority += 1;
                        changedOrder = true;
                    }

                    y += line + 4f;
                }

                if (changedOrder && binder != null)
                {
                    // Undo는 신경 안쓰고 바로 값만 반영
                    binder.SetApplyOrderAndAttachOrder(newPriority, newAttach);
                }

                // ─ 4) Observing (IBinds) 메타 블럭 (✔ / ✕ 포함) ─
                var observed = t != null ? ExtractObservedComponentTypes(t) : Array.Empty<Type>();

                if (observed.Count > 0)
                {
                    var providedTypes = GetProvidedComponentTypesFromEntries(_compEntriesProp); // _data.entries
                    var providedSet = new HashSet<Type>(providedTypes);

                    var metaTitle = new Rect(rect.x + 12f, y, rect.width - 24f, line);
                    EditorGUI.LabelField(metaTitle, "Observing (IBinds)", EditorStyles.boldLabel);
                    y = metaTitle.yMax + 2f;

                    foreach (var ct in observed)
                    {
                        var lineRect = new Rect(rect.x + 16f, y, rect.width - 32f, line);

                        var left = new Rect(lineRect.x, lineRect.y, lineRect.width - 24f, lineRect.height);
                        var right = new Rect(lineRect.xMax - 20f, lineRect.y, 20f, lineRect.height);

                        bool hasComp = providedSet.Contains(ct);

                        EnsureStylesReady(); // _obsOkStyle / _obsMissingStyle 등

                        if (hasComp)
                        {
                            EditorGUI.LabelField(left, $"• {ct.Name}", _obsOkStyle);

                            var checkStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                alignment = TextAnchor.MiddleRight,
                                richText = true
                            };
                            EditorGUI.LabelField(
                                right,
                                new GUIContent("<b><color=#2ECC71>✔</color></b>"),
                                checkStyle
                            );
                        }
                        else
                        {
                            EditorGUI.LabelField(left, $"• {ct.Name}", _obsMissingStyle);

                            var xStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                alignment = TextAnchor.MiddleRight,
                                richText = true
                            };
                            EditorGUI.LabelField(
                                right,
                                new GUIContent("<b><color=#E74C3C>✕</color></b>"),
                                xStyle
                            );
                        }

                        y += line + 1f;
                    }

                    y += PAD;
                }

                // ─ 5) 바인더 본문 ─
                var rBody = new Rect(rect.x + 8f, y, rect.width - 16f, rect.yMax - y - PAD);
                EditorGUI.BeginChangeCheck();
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
                // NOTE: 현재 contextsProp는 ContextAsset(Object ref)이므로
                // managedReferenceValue는 대부분 null일 것.
                // 나중에 pill 배지를 부활시킬 때 ContextAsset → IContext 타입 매핑을
                // 별도 메타 정보로 풀어주면 됨.
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

        static void DrawContextPills(Rect area, Type[] required, IReadOnlyList<Type> providedTypes)
        {
            // pill UI 현재 비활성화 상태 (필요시 ContextAsset 기반으로 다시 구현)
        }

        void DrawBinderBody(Rect area, SerializedProperty root)
        {
            var y = area.y;
            var x = area.x;
            var w = area.width;

            var iter = root.Copy();
            var end = root.GetEndProperty();

            bool enterChildren = true;
            while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
            {
                float h = EditorGUI.GetPropertyHeight(iter, GUIContent.none, true);
                var r = new Rect(x, y, w, h);
                EditorGUI.PropertyField(r, iter, GUIContent.none, true);
                y += h + 2f;
                enterChildren = false;
            }
        }

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

        static void PingTypeSource(Type t)
        {
            if (t == null)
                return;

            var guids = AssetDatabase.FindAssets($"t:MonoScript {t.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;

                if (ms.GetClass() == t)
                {
                    EditorGUIUtility.PingObject(ms);
                    //Selection.activeObject = ms;
                    break;
                }
            }
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            // Unity 기본 검색 아이콘
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("Search Icon");

            // 혹시 아이콘을 못 찾았을 경우 텍스트로 fallback
            if (gc == null)
                gc = new GUIContent("🔍", tooltip);
            else
                gc.tooltip = tooltip;

            return gc;
        }
    }
}
#endif
