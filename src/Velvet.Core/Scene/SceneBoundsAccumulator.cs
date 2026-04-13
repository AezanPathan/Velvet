using Velvet.Core.Rendering;

namespace Velvet.Core.Scene;

internal static class SceneBoundsAccumulator
{
    internal static void Expand(ref BoundingBox? bounds, in BoundingBox candidate)
    {
        if (!bounds.HasValue)
        {
            bounds = candidate;
            return;
        }

        var value = bounds.Value;
        value.Expand(candidate);
        bounds = value;
    }
}
