using Velvet.Core.Rendering.Cameras;

namespace Velvet.Core.Rendering.Core;

/// <summary>
/// Manages viewport resize requests and applies them asynchronously to the rendering backend and camera.
/// </summary>
public sealed class ResizeController
{
    private const float DprEpsilon = 0.0001f;

    private int _pendingWidth;
    private int _pendingHeight;
    private float _pendingDpr;
    private bool _hasPendingResize;

    private int _viewportWidth;
    private int _viewportHeight;
    private float _viewportDpr = 1f;

    public bool HasPendingResize => _hasPendingResize;

    public void RequestResize(int width, int height, float dpr)
    {
        if (width <= 0 || height <= 0 || dpr <= 0f) return;
        if (_hasPendingResize && _pendingWidth == width && _pendingHeight == height && MathF.Abs(_pendingDpr - dpr) < DprEpsilon) return;
        if (_viewportWidth == width && _viewportHeight == height && MathF.Abs(_viewportDpr - dpr) < DprEpsilon) return;


        _pendingWidth = width;
        _pendingHeight = height;
        _pendingDpr = dpr;
        _hasPendingResize = true;
    }

    public async Task ApplyResizeAsync(Func<int, int, Task> resizeBackendAsync, Camera? camera, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resizeBackendAsync);

        if (!_hasPendingResize) return;


        _hasPendingResize = false;

        var requestedWidth = _pendingWidth;
        var requestedHeight = _pendingHeight;
        var requestedDpr = _pendingDpr;

        var pixelWidth = (int)(requestedWidth * requestedDpr);
        var pixelHeight = (int)(requestedHeight * requestedDpr);

        if (pixelWidth <= 0 || pixelHeight <= 0) return;

        cancellationToken.ThrowIfCancellationRequested();
        await resizeBackendAsync(pixelWidth, pixelHeight).ConfigureAwait(false);

        _viewportWidth = requestedWidth;
        _viewportHeight = requestedHeight;
        _viewportDpr = requestedDpr;

        ApplyViewportToCamera(camera);
    }

    public void ApplyViewportToCamera(Camera? camera)
    {
        if (camera is null || _viewportHeight <= 0) return;

        camera.AspectRatio = (float)_viewportWidth / _viewportHeight;
        camera.UpdateProjection();
    }
}
