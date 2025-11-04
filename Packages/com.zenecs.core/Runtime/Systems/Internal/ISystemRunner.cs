#nullable enable

namespace ZenECS.Core.Internal
{
    /// <summary>Runs user systems. Minimal placeholder runner in this skeleton.</summary>
    internal interface ISystemRunner
    {
        void BeginFrame(float dt);
        void FixedStep(float fixedDelta);
        void LateFrame(float alpha = 1.0f);
    }
}