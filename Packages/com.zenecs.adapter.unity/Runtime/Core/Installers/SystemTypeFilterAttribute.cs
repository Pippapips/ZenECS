#nullable enable
using System;
using UnityEngine;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// PropertyDrawer가 있는 필드의 타입 선택을 제한하기 위한 필터 속성.
    /// 예) [SystemTypeFilter(typeof(ISystem), allowAbstract:false)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SystemTypeFilterAttribute : PropertyAttribute
    {
        public Type BaseType { get; }
        public bool AllowAbstract { get; }
        public SystemTypeFilterAttribute(Type baseType, bool allowAbstract = false)
        {
            BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
            AllowAbstract = allowAbstract;
        }
    }
}