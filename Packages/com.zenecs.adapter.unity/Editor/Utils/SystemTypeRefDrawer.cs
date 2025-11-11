#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Util;

[CustomPropertyDrawer(typeof(SystemTypeRef))]
public class SystemTypeRefDrawer : PropertyDrawer
{
    // 타입→MonoScript 역탐색 캐시 (에디터 세션 동안 유지)
    private static readonly Dictionary<Type, MonoScript> _typeToScript = new();
    private static int _reentryGuard; // 드문 재귀 보호

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var aqnProp = property.FindPropertyRelative("_assemblyQualifiedName");
        var aqn = aqnProp.stringValue;
        var currentType = string.IsNullOrEmpty(aqn) ? null : Type.GetType(aqn, false);

        // 필터 읽기
        var filter = attribute as SystemTypeFilterAttribute;
        var baseType = filter?.BaseType;
        var allowAbstract = filter?.AllowAbstract ?? false;

        EditorGUI.BeginProperty(position, label, property);
        var fieldRect = EditorGUI.PrefixLabel(position, label);

        // 현재 타입을 MonoScript로 역탐색(없을 수 있음)
        var currentScript = ResolveMonoScript(currentType);

        EditorGUI.BeginChangeCheck();
        var picked = EditorGUI.ObjectField(fieldRect, currentScript, typeof(MonoScript), false) as MonoScript;
        if (EditorGUI.EndChangeCheck())
        {
            if (picked == null)
            {
                aqnProp.stringValue = string.Empty;
            }
            else
            {
                var t = picked.GetClass();
                if (!IsValidType(t, baseType, allowAbstract, out var msg))
                {
                    EditorUtility.DisplayDialog("타입 선택 오류", msg, "확인");
                    // 기존 값 유지
                }
                else
                {
                    aqnProp.stringValue = t!.AssemblyQualifiedName;
                    Cache(t, picked);
                }
            }
        }

        // 하단 정보 표시
        var infoRect = new Rect(position.x, position.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
        if (currentType != null)
        {
            var ok = IsValidType(currentType, baseType, allowAbstract, out _);
            var style = ok ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel;
            EditorGUI.LabelField(infoRect, currentType.FullName, style);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight * 1.6f;

    private static bool IsValidType(Type? t, Type? baseType, bool allowAbstract, out string message)
    {
        if (t == null) { message = "유효한 타입이 아닙니다."; return false; }
        if (!allowAbstract && t.IsAbstract) { message = "추상 타입은 선택할 수 없습니다."; return false; }
        if (baseType != null && !baseType.IsAssignableFrom(t))
        {
            message = $"선택한 타입이 '{baseType.Name}'을(를) 구현/상속하지 않습니다.";
            return false;
        }
        message = string.Empty;
        return true;
    }

    private static void Cache(Type t, MonoScript ms)
    {
        if (t != null && ms != null) _typeToScript[t] = ms;
    }

    private static MonoScript? ResolveMonoScript(Type? t)
    {
        if (t == null) return null;
        if (_typeToScript.TryGetValue(t, out var cached) && cached != null) return cached;

        // 드문 경우 재귀 보호
        if (_reentryGuard > 0) return null;
        _reentryGuard++;

        try
        {
            // 1) 이름으로 빠르게 후보 검색 후 2) GetClass() 정확 매칭
            //    (동명이인 클래스 대비를 위해 반드시 GetClass() == t 확인)
            var query = $"t:MonoScript {t.Name}";
            var guids = AssetDatabase.FindAssets(query);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;
                var cls = ms.GetClass();
                if (cls == t)
                {
                    _typeToScript[t] = ms;
                    return ms;
                }
            }

            // 3) 이름으로 못 찾았으면 전체 스캔(비상 경로)
            var all = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var g in all)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;
                var cls = ms.GetClass();
                if (cls == t)
                {
                    _typeToScript[t] = ms;
                    return ms;
                }
            }
        }
        finally { _reentryGuard--; }

        return null;
    }
}
#endif
