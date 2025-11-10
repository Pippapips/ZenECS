namespace ZenECS.Adapter.Unity.Attributes
{
    [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Class)]
    public sealed class ZenReadOnlyInInspectorAttribute : System.Attribute
    {
        public ZenReadOnlyInInspectorAttribute()
        {
        }
    }
}