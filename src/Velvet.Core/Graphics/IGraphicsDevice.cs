using System.Threading.Tasks;

namespace Velvet.Core.Graphics;

public interface IGraphicsDevice
{
    Task<int> InitializeAsync();

    // OPTIONAL: Add draw/clear/resize based on your engine’s needs:
    // Task DrawMeshAsync(int meshId, int programId);
    // Task ClearAsync(float r, float g, float b, float a);
    // Task ResizeAsync(int width, int height);
}
