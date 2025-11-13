namespace ZenECS.Adapter.Unity.Attributes
{
    // Component 필드 편집을 불가하게 하는 속성
    [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Class)]
    public sealed class ZenReadOnlyInInspectorAttribute : System.Attribute
    {
        public ZenReadOnlyInInspectorAttribute()
        {
        }
    }
}