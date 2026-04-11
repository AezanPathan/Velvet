namespace Velvet.Core.Rendering.Input;

using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Cameras.Controllers;

public class OrbitInputBinder
{
    private readonly OrbitController _orbit;
    private readonly Camera _camera;

    private bool isDragging;
    private int lastX;
    private int lastY;

    public OrbitInputBinder(OrbitController orbit, Camera camera)
    {
        _orbit = orbit;
        _camera = camera;
    }

    public OrbitController Orbit => _orbit;

    public Camera Camera => _camera;

    public void OnMouseDown(int x, int y)
    {
        isDragging = true;
        lastX = x;
        lastY = y;
    }

    public void OnMouseMove(int x, int y)
    {
        if (!isDragging)
        {
            return;
        }

        var dx = x - lastX;
        var dy = y - lastY;

        _orbit.ApplyYaw(-dx * 0.005f);
        _orbit.ApplyPitch(dy * 0.005f);

        lastX = x;
        lastY = y;
    }

    public void OnMouseUp()
    {
        isDragging = false;
    }

    public void OnWheel(float delta)
    {
        _orbit.ApplyZoomMultiplier(1.0f + delta * 0.001f);
    }

    public void Update()
    {
        _orbit.UpdateCamera(_camera);
    }
}
