namespace Velvet.Core.Rendering.Input;

using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;

public class TouchOrbitBinder
{
    private readonly OrbitController _orbit;
    private readonly Camera _camera;

    public TouchOrbitBinder(OrbitController orbit, Camera camera)
    {
        _orbit = orbit;
        _camera = camera;
    }

    public OrbitController Orbit => _orbit;

    public Camera Camera => _camera;

    public void OnTouchRotate(float deltaX, float deltaY)
    {
        _orbit.ApplyYaw(-deltaX * 0.005f);
        _orbit.ApplyPitch(deltaY * 0.005f);
    }

    public void OnTouchZoom(float delta)
    {
        var multiplier = 1.0f + (delta * 0.001f);
        if (multiplier <= 0f)
        {
            multiplier = 0.01f;
        }

        _orbit.ApplyZoomMultiplier(multiplier);
    }

    public void Update()
    {
        _orbit.UpdateCamera(_camera);
    }
}