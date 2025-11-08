#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Internal.Systems
{
    /// <summary>Runs user systems. Minimal placeholder runner in this skeleton.</summary>
    internal interface ISystemRunner
    {
        void Build(IEnumerable<ISystem>? systems, Action<string>? warn);

        void Initialize(IWorld w);
        void Shutdown(IWorld w);
        
        void BeginFrame(IWorld w, float dt);
        void FixedStep(IWorld w, float fixedDelta);
        void LateFrame(IWorld w, float dt, float alpha = 1.0f);
    }
}