using System;

namespace ZenECS.Adapter.Unity.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ZenEcsExplorerHiddenAttribute : Attribute
    {
    }
}