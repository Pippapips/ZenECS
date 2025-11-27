#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Install
{
    [CreateAssetMenu(fileName = "SystemsPreset", menuName = "ZenECS/Systems Preset")]
    public sealed class SystemsPreset : ScriptableObject
    {
        [Header("Systems")] [Tooltip("ISystem 구현 타입(런타임 안전)")] [SystemTypeFilter(typeof(ISystem), allowAbstract: false)]
        public SystemTypeRef[]? systemTypes;

        public IEnumerable<Type> GetValidTypes()
        {
            if (systemTypes == null) yield break;
            foreach (var tr in systemTypes)
            {
                var t = tr.Resolve(); // ← 여기!
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                yield return t;
            }
        }

#if UNITY_EDITOR
        // 에디터 편의용 클린업: 빈 슬롯은 남기고, 값이 있는 항목만 유효성/중복 검사
        private void OnValidate()
        {
            if (systemTypes == null || systemTypes.Length == 0) return;

            var list = new List<SystemTypeRef>(systemTypes.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var r in systemTypes)
            {
                var aqn = r.AssemblyQualifiedName;

                // 빈 칸은 보존(추후 선택 용도)
                if (string.IsNullOrWhiteSpace(aqn))
                {
                    list.Add(r);
                    continue;
                }

                var t = r.Resolve();
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                if (!seen.Add(aqn)) continue;

                list.Add(r);
            }

            systemTypes = list.ToArray();
        }
#endif
    }
}