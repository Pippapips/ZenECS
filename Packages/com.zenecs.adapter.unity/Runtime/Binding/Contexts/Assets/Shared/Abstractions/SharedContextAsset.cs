#nullable enable
using System;
using ZenECS.Core.Binding;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Binding.Contexts.Assets
{
    public abstract class SharedContextAsset : ContextAsset
    {
        /// <summary>
        /// 이 마커가 가리키는 공유 컨텍스트 타입.
        /// 예: typeof(UIRootContext)
        /// </summary>
        public abstract Type ContextType { get; }
    }
}