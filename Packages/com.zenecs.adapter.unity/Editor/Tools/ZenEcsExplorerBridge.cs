#if UNITY_EDITOR
#nullable enable
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.EditorTools
{
    /// <summary>
    /// ExplorerWindow와 느슨하게 연결하기 위한 브리지.
    /// - 타입 이름: "ExplorerWindow" 또는 "EcsExplorerWindow" 검색
    /// - 인스턴스 획득: EditorWindow.GetWindow(type, true, title, true)
    /// - 메서드: public void SelectEntity(int entityId, int entityGen)
    /// </summary>
    internal static class ZenEcsExplorerBridge
    {
        private static Type? _cachedType;
        private static MethodInfo? _cachedSelectMethod;

        /// <summary>ExplorerWindow를 열어 SelectEntity(id, gen)을 호출.</summary>
        public static bool TryOpenAndSelect(IWorld w, int entityId, int entityGen)
        {
            var t = ResolveExplorerType();
            if (t == null) return false;

            var win = EditorWindow.GetWindow(t, utility: false, title: "ZenECS Explorer", focus: true);
            if (win == null)
            {
                // 포커스 실패 시 마지막 수단
                win = ScriptableObject.CreateInstance(t) as EditorWindow;
                if (win == null) return false;
                win.Show();
            }

            var mi = ResolveSelectEntityMethod(t);
            if (mi == null) return false;

            try
            {
                mi.Invoke(win, new object[] { w, entityId, entityGen });
                win.Repaint();
                return true;
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogError($"[ExplorerBridge] SelectEntity 호출 중 예외: {tie.InnerException?.Message ?? tie.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExplorerBridge] SelectEntity 호출 실패: {ex.Message}");
                return false;
            }
        }

        private static Type? ResolveExplorerType()
        {
            if (_cachedType != null) return _cachedType;

            // 우선순위: 명확한 이름 먼저
            string[] candidates = { "ExplorerWindow", "EcsExplorerWindow", "ZenEcsExplorerWindow" };
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? t = null;
                try
                {
                    t = asm.GetTypes()
                           .FirstOrDefault(x =>
                               typeof(EditorWindow).IsAssignableFrom(x) &&
                               candidates.Contains(x.Name));
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var le in e.LoaderExceptions)
                        Debug.LogWarning($"[ExplorerBridge] Type load warning: {le.Message}");
                    continue;
                }

                if (t != null)
                {
                    _cachedType = t;
                    break;
                }
            }

            if (_cachedType == null)
                Debug.LogWarning("[ExplorerBridge] ExplorerWindow 타입을 찾지 못했습니다. (이름: ExplorerWindow/EcsExplorerWindow)");

            return _cachedType;
        }

        private static MethodInfo? ResolveSelectEntityMethod(Type? t)
        {
            if (_cachedSelectMethod != null) return _cachedSelectMethod;

            if (t == null)
            {
                Debug.LogWarning($"[ExplorerBridge] {t} is null");
                return null;
            }
            _cachedSelectMethod = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                   .FirstOrDefault(m =>
                                   {
                                       if (m.Name != "SelectEntity") return false;
                                       var ps = m.GetParameters();
                                       return ps.Length == 3
                                              && ps[0].ParameterType == typeof(IWorld)
                                              && ps[1].ParameterType == typeof(int)
                                              && ps[2].ParameterType == typeof(int);
                                   });

            if (_cachedSelectMethod == null)
                Debug.LogWarning("[ExplorerBridge] SelectEntity(IWorld, int, int) 메서드를 찾지 못했습니다.");

            return _cachedSelectMethod;
        }
    }
}
#endif
