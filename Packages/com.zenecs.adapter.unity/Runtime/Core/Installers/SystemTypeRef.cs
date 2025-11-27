#nullable enable
using System;
using UnityEngine;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// 런타임 안전한 타입 참조(AssemblyQualifiedName 보관).
    /// Editor에선 MonoScript 피커로 선택, 런타임에선 Type으로 해석.
    /// </summary>
    [Serializable]
    public struct SystemTypeRef
    {
        [SerializeField] string _assemblyQualifiedName; // e.g. "MyGame.MovementSystem, MyGame.Assembly"

        public string AssemblyQualifiedName
        {
            readonly get => _assemblyQualifiedName;
            set => _assemblyQualifiedName = value;
        }

        /// <summary>저장된 이름을 실제 Type으로 해석(실패 시 null)</summary>
        public readonly Type? Resolve() =>
            string.IsNullOrWhiteSpace(_assemblyQualifiedName)
                ? null
                : Type.GetType(_assemblyQualifiedName, throwOnError: false);
        
        public override readonly string ToString() => _assemblyQualifiedName ?? string.Empty;

        public static SystemTypeRef FromType(Type t)
            => new SystemTypeRef { _assemblyQualifiedName = t.AssemblyQualifiedName };
    }
}