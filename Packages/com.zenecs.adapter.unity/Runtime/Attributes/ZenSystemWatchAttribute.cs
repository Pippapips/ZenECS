using System;
using System.Diagnostics;

namespace ZenECS.Adapter.Unity.Attributes
{
    /// <summary>
    /// 간단 관제 쿼리: AllOf(모두 포함) 조합 기준으로 엔티티를 수집.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenSystemWatchAttribute : Attribute
    {
        public readonly Type[] AllOf;
        public ZenSystemWatchAttribute(params Type[] allOf) { AllOf = allOf; }
    }
}