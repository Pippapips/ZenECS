#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Install
{
    [CreateAssetMenu(fileName = "SystemsPreset", menuName = "ZenECS/Systems Preset")]
    public sealed class SystemsPreset : ScriptableObject
    {
        [Tooltip("ISystem 구현 타입(런타임 안전)")]
        [SystemTypeFilter(typeof(ZenECS.Core.Systems.ISystem), allowAbstract:false)]
        public SystemTypeRef[]? _systemTypes;
        
        public IEnumerable<Type> GetValidTypes()
        {
            if (_systemTypes == null) yield break;
            foreach (var tr in _systemTypes)
            {
                var t = tr.Resolve();                          // ← 여기!
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                yield return t;
            }
        }
        
        public void OnValidate()
        {
            if (_systemTypes == null || _systemTypes.Length == 0) return;

            var list = new List<SystemTypeRef>(_systemTypes.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var r in _systemTypes)
            {
                var aqn = r.AssemblyQualifiedName;

                // ✅ ① 새로 추가된 "빈 슬롯"은 무조건 보존 (사용자가 나중에 선택할 수 있게)
                if (string.IsNullOrWhiteSpace(aqn))
                {
                    list.Add(r);
                    continue;
                }

                // ✅ ② 값이 있는 경우에만 유효성/중복 검사
                var t = r.Resolve();
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ZenECS.Core.Systems.ISystem).IsAssignableFrom(t)) continue;
                if (!seen.Add(aqn)) continue; // 중복 제거

                list.Add(r);
            }

            _systemTypes = list.ToArray();
        }
    }
}