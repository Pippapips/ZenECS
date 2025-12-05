#if UNITY_EDITOR
#nullable enable
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Hierarchy에서 EntityLink가 달린 GameObject 오른쪽 끝에
    /// 눈에 잘 띄는 "E" 마크를 표시.
    /// </summary>
    [InitializeOnLoad]
    public static class EntityLinkHierarchyMarker
    {
        static EntityLinkHierarchyMarker()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (go == null) return;

            if (!go.TryGetComponent<EntityLink>(out _))
                return;

            const float paddingRight = 4f;
            const float width = 22f;
            const float height = 14f;

            // 오른쪽 끝 기준으로 작은 배지 영역 계산
            var r = new Rect(
                selectionRect.xMax - width - paddingRight,
                selectionRect.y + (selectionRect.height - height) * 0.5f,
                width,
                height
            );

            // 다크/라이트 스킨에 따라 색 조금 다르게
            Color bg = EditorGUIUtility.isProSkin
                ? new Color(0.29f, 0.22f, 1f, 0.9f)    // Pro: 쨍한 파란색
                : new Color(0.18f, 0.11f, 0.9f, 0.95f); // Light: 조금 더 진한 파란색

            Color border = new Color(0f, 0f, 0f, 0.6f);
            Color text = Color.white;

            // 배경(살짝 둥근 느낌 나게 두 번 그리기)
            var bgRect = r;
            bgRect.xMin += 0.5f;
            bgRect.xMax -= 0.5f;
            bgRect.yMin += 0.5f;
            bgRect.yMax -= 0.5f;

            // 바깥 테두리
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMin - 1, bgRect.width + 2, 1), border);
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMax,     bgRect.width + 2, 1), border);
            EditorGUI.DrawRect(new Rect(bgRect.xMin - 1, bgRect.yMin, 1, bgRect.height),       border);
            EditorGUI.DrawRect(new Rect(bgRect.xMax,     bgRect.yMin, 1, bgRect.height),       border);

            // 안쪽 채우기
            EditorGUI.DrawRect(bgRect, bg);

            // 텍스트 스타일
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = text },
                clipping = TextClipping.Clip
            };

            GUI.Label(bgRect, "E", style);
        }
    }
}
#endif
