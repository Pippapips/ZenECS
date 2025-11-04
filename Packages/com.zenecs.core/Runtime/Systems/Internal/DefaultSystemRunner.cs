#nullable enable
namespace ZenECS.Core.Internal
{
    /// <summary>Placeholder runner. Replace with a real scheduler in your project.</summary>
    internal sealed class DefaultSystemRunner : ISystemRunner
    {
        public void BeginFrame(float dt)
        {
        }

        public void FixedStep(float fixedDelta)
        {
        }

        public void LateFrame(float alpha = 1)
        {
        }
    }
}