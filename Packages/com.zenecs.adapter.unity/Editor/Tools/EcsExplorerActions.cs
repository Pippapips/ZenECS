#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.EditorTools
{
    internal static class EcsExplorerActions
    {
        public static bool TryGetEntityMainView(IWorld world, in Entity entity, out GameObject go)
        {
            go = null;

            if (world != null && entity.Id > 0)
            {
                var reg = EntityViewRegistry.For(world);
                if (reg.TryGetMain(entity, out var main) && main && main.IsAlive) go = main.gameObject;
                else if (reg.TryGetPrimary(entity, out var p) && p && p.IsAlive) go = p.gameObject;

                if (!go)
                {
#if UNITY_2022_2_OR_NEWER
                    var links = Object.FindObjectsByType<EntityLink>(FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
#else
                var links = Object.FindObjectsOfType<EntityLink>(true);
#endif
                    var other = entity;
                    var found = links.FirstOrDefault(l => l && l.World == world && l.Entity.Equals(other));
                    if (found) go = found.gameObject;
                }
            }

            return go != null;
        }
        
        public static bool TrySelectEntityMainView(GameObject go, bool focusHierarchy = true, bool frameScene = true)
        {
            if (!go) return false;

            Selection.activeObject = go; EditorGUIUtility.PingObject(go);
            if (focusHierarchy)
            {
                var t = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                if (t != null) EditorWindow.FocusWindowIfItsOpen(t);
            }
            if (frameScene && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            return true;
        }
    }
}
#endif