using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity
{
    public interface ISystemPresetResolver
    {
        /// <summary>
        /// SharedContextMarkerAsset에 대응하는 IContext 인스턴스를 리턴한다.
        /// 없으면 null.
        /// </summary>
        List<ISystem> InstantiateSystems(List<Type> types);
    }
}