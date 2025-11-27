using System;

namespace ZenECS.Adapter.Unity
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ZenEcsExplorerHiddenAttribute : Attribute
    {
    }
}