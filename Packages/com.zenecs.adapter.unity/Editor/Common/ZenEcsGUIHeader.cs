#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZenECS.EditorCommon
{
    internal static class ZenEcsGUIHeader
    {
        static GUIStyle _titleStyle;
        static GUIStyle _descStyle;
        static GUIStyle _tagStyle;

        static Texture2D _brandIcon;

        // UPM 패키지 내 경로에 맞게 수정
        const string BrandIconPath =
            "Packages/com.zenecs.adapter.unity/Editor/Art/pippapips_icon.png";

        static readonly Color _bgDark      = new Color(0.10f, 0.16f, 0.21f, 1f);
        static readonly Color _bgLight     = new Color(0.80f, 0.88f, 0.94f, 1f);
        static readonly Color _accentDark  = new Color(0.25f, 0.70f, 0.95f, 1f);
        static readonly Color _accentLight = new Color(0.10f, 0.45f, 0.80f, 1f);

        static readonly Color _tagBgDark   = new Color(0.18f, 0.32f, 0.45f, 1f);
        static readonly Color _tagBgLight  = new Color(0.88f, 0.95f, 1.00f, 1f);

        static void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize  = 12,
                    richText  = false
                };
                _titleStyle.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.86f, 0.95f, 1.00f, 1f)
                    : new Color(0.10f, 0.24f, 0.40f, 1f);
            }

            if (_descStyle == null)
            {
                _descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap  = true,
                    fontSize  = 10
                };
                _descStyle.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.78f, 0.90f, 1.00f, 1f)
                    : new Color(0.12f, 0.28f, 0.45f, 1f);
            }

            if (_tagStyle == null)
            {
                _tagStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 9,
                    padding   = new RectOffset(6, 6, 2, 2),
                    richText  = false
                };
                _tagStyle.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.88f, 0.96f, 1.00f, 1f)
                    : new Color(0.08f, 0.22f, 0.40f, 1f);
            }

            if (_brandIcon == null)
            {
                _brandIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(BrandIconPath);
            }
        }

        // 심플 오버로드들
        public static void DrawHeader(string roleName)
            => DrawHeader(roleName, null, null, null);

        public static void DrawHeader(string roleName, string description)
            => DrawHeader(roleName, description, null, null);

        public static void DrawHeader(
            string roleName,
            string description,
            string[] tags,
            Texture icon = null
        )
        {
            if (string.IsNullOrEmpty(roleName))
                roleName = "Inspector";

            EnsureStyles();

            bool hasDesc = !string.IsNullOrEmpty(description);
            bool hasTags = tags != null && tags.Length > 0;

            const float iconSize   = 64f;
            const float padX       = 10f;
            const float padY       = 6f;
            const float accentW    = 3f;
            const float textGap    = 8f;
            float lineHeight       = EditorGUIUtility.singleLineHeight;

            // 텍스트 블록 높이 대략 계산
            int textLines = 1; // title
            if (hasTags) textLines++;
            if (hasDesc) textLines += 2; // 설명은 두 줄 정도 여유

            float baseTextHeight = textLines * lineHeight + padY * 2f;
            float minHeight      = iconSize + padY * 2f;
            float height         = Mathf.Max(baseTextHeight, minHeight);

            var rect = GUILayoutUtility.GetRect(
                0f, height,
                GUILayout.ExpandWidth(true)
            );

            var bg     = EditorGUIUtility.isProSkin ? _bgDark : _bgLight;
            var accent = EditorGUIUtility.isProSkin ? _accentDark : _accentLight;

            // 배경
            EditorGUI.DrawRect(rect, bg);

            // 왼쪽 accent bar
            var accentRect = new Rect(rect.x, rect.y, accentW, rect.height);
            EditorGUI.DrawRect(accentRect, accent);

            // 상단/하단 라인
            var topRect    = new Rect(rect.x, rect.y, rect.width, 1f);
            var bottomRect = new Rect(rect.x, rect.yMax - 1f, rect.width, 1f);
            EditorGUI.DrawRect(topRect,    new Color(accent.r, accent.g, accent.b, 0.18f));
            EditorGUI.DrawRect(bottomRect, new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.35f : 0.12f));

            // 아이콘
            var useIcon = icon != null ? icon : _brandIcon;
            float innerLeft = rect.x + accentW + padX;

            if (useIcon != null)
            {
                var iconRect = new Rect(
                    innerLeft,
                    rect.y + (rect.height - iconSize) * 0.5f,
                    iconSize,
                    iconSize
                );
                GUI.DrawTexture(iconRect, useIcon, ScaleMode.ScaleToFit, true);
                innerLeft = iconRect.xMax + textGap;
            }

            float innerRight  = rect.xMax - padX;
            float innerWidth  = Mathf.Max(10f, innerRight - innerLeft);
            float cursorY     = rect.y + padY + 20;

            // Title + tags (같은 수평 라인)
            var titleRect = new Rect(innerLeft, cursorY, innerWidth, lineHeight);

            float tagsWidth = 0f;
            if (hasTags)
            {
                foreach (var raw in tags)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    var size = _tagStyle.CalcSize(new GUIContent(raw));
                    tagsWidth += size.x + 4f;
                }
            }

            float titleWidth = hasTags
                ? Mathf.Max(40f, innerWidth - tagsWidth - 6f)
                : innerWidth;

            titleRect.width = titleWidth;
            EditorGUI.LabelField(titleRect, $"ZenECS - {roleName}", _titleStyle);

            // Tags 오른쪽 정렬
            if (hasTags)
            {
                float tagRight = innerLeft + innerWidth;
                var tagBg = EditorGUIUtility.isProSkin ? _tagBgDark : _tagBgLight;

                foreach (var raw in tags)
                {
                    if (string.IsNullOrEmpty(raw)) continue;

                    var content = new GUIContent(raw);
                    var size    = _tagStyle.CalcSize(content);
                    float w     = size.x;
                    float h     = lineHeight - 2f;

                    var tagRect = new Rect(
                        tagRight - w,
                        cursorY - 15,
                        w,
                        h
                    );
                    tagRight -= (w + 4f);

                    EditorGUI.DrawRect(tagRect, tagBg);
                    var borderCol = new Color(accent.r, accent.g, accent.b, 0.35f);
                    EditorGUI.DrawRect(new Rect(tagRect.x, tagRect.y, tagRect.width, 1f), borderCol);
                    EditorGUI.DrawRect(new Rect(tagRect.x, tagRect.yMax - 1f, tagRect.width, 1f), borderCol);

                    GUI.Label(tagRect, content, _tagStyle);
                }
            }

            cursorY += lineHeight + 2f;

            // Description
            if (hasDesc)
            {
                var descRect = new Rect(
                    innerLeft,
                    cursorY,
                    innerWidth,
                    rect.yMax - cursorY - padY
                );
                EditorGUI.LabelField(descRect, description, _descStyle);
            }

            GUILayout.Space(6f);
        }
    }
}
#endif
