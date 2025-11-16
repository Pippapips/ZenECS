#if UNITY_EDITOR
#nullable enable
using UnityEditor;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.EditorTools;

namespace ZenECS.EditorInspectors
{
    /// <summary>
    /// EntityLink의 런타임 메타를 인스펙터에서 표시 + ExplorerWindow 연동 버튼 제공.
    /// </summary>
    [CustomEditor(typeof(EntityLink))]
    public sealed class EntityLinkInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var link = (EntityLink)target;

            // (1) 기본 ViewKey 필드는 보기만 가능(런타임에 변경은 코드로)
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("GameObject", link.gameObject, typeof(GameObject), true);
            }

            EditorGUILayout.Space(4);

            // (2) 메타 패널
            DrawMetaBox(link);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!link || link.World == null))
            {
                // ExplorerWindow로 선택
                if (GUILayout.Button("Select Linked Entity in Explorer", GUILayout.Height(24)))
                {
                    if (!TrySelectInExplorer(link))
                        EditorUtility.DisplayDialog("Explorer",
                            "ExplorerWindow를 찾을 수 없거나 SelectEntity 메서드를 호출할 수 없습니다.\n" +
                            "창/이름/네임스페이스를 확인하세요.", "OK");
                }

                // // 하이라키 핑(메인 링크 기준)
                // if (GUILayout.Button("Ping Main View in Hierarchy"))
                // {
                //     if (!TryPingMainView(link))
                //         EditorUtility.DisplayDialog("Hierarchy",
                //             "Main View를 찾지 못했습니다. (등록된 링크가 없거나 비활성일 수 있음)", "OK");
                // }
            }
        }

        private static void DrawMetaBox(EntityLink link)
        {
            var alive = link && link.World != null && link.IsAlive;
            var style = new GUIStyle(EditorStyles.helpBox) { richText = true };
            EditorGUILayout.BeginVertical(style);
            {
                EditorGUILayout.LabelField("EntityLink Metadata", EditorStyles.boldLabel);

                if (!link || link.World == null)
                {
                    EditorGUILayout.HelpBox("World가 연결되지 않았습니다. (링크 미설정)", MessageType.Info);
                }
                else
                {
                    var e = link.Entity;
                    var worldName = SafeWorldName(link.World);
                    EditorGUILayout.LabelField("World", worldName);
                    EditorGUILayout.LabelField("Entity", alive ? $"{e.Id}:{e.Gen}" : "(invalid)");
                    EditorGUILayout.LabelField("IsAlive", alive ? "True" : "False");
                    EditorGUILayout.LabelField("Key", link.Key.ToString());
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static string SafeWorldName(IWorld w)
        {
            try { return string.IsNullOrEmpty(w.Name) ? "(unnamed)" : w.Name; }
            catch { return "(unknown)"; }
        }

        private static bool TrySelectInExplorer(EntityLink? link)
        {
            if (link == null || link.World == null || !link.IsAlive) return false;

            var e = link.Entity;
            return ZenEcsExplorerBridge.TryOpenAndSelect(e.Id, e.Gen);
        }

        private static bool TryPingMainView(EntityLink? link)
        {
            if (link == null || link.World == null) return false;

            var w = link.World;
            var e = link.Entity;

            var reg = EntityViewRegistry.For(w);
            if (reg.TryGetMain(e, out var main) && main && main.IsAlive)
            {
                Selection.activeObject = main.gameObject;
                EditorGUIUtility.PingObject(main.gameObject);
                var hierType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                if (hierType != null) EditorWindow.FocusWindowIfItsOpen(hierType);
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                return true;
            }
            return false;
        }
    }
}
#endif
