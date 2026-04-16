namespace Velvet.Core.Rendering.Input;

using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;

public class OrbitInputBinder
{
    private readonly OrbitController _orbit;
    private readonly Camera _camera;

    private bool _isDragging;
    private int _lastX;
    private int _lastY;

    public OrbitInputBinder(OrbitController orbit, Camera camera)
    {
        _orbit = orbit;
        _camera = camera;
    }

    public OrbitController Orbit => _orbit;

    public Camera Camera => _camera;

    public void OnMouseDown(int x, int y)
    {
        _isDragging = true;
        _lastX = x;
        _lastY = y;
    }

    public void OnMouseMove(int x, int y)
    {
        if (!_isDragging)
        {
            return;
        }

        var dx = x - _lastX;
        var dy = y - _lastY;

        _orbit.ApplyYaw(-dx * 0.005f);
        _orbit.ApplyPitch(dy * 0.005f);

        _lastX = x;
        _lastY = y;
    }

    public void OnMouseUp()
    {
        _isDragging = false;
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
