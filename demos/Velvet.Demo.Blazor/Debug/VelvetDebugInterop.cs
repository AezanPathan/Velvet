using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Blazor;

namespace Velvet.Demo.Blazor.Debug;

internal sealed class VelvetDebugInterop
{
    private readonly Func<Camera> _getCamera;
    private readonly Func<DirectionalLightState> _getDirectional;
    private readonly Func<PointLightState> _getPoint;
    private readonly Func<Material> _getMaterial;

    private readonly Action<Vector3> _setCameraPosition;
    private readonly Action<Vector3> _setCameraTarget;
    private readonly Action<float, float, float> _setCameraPerspective;

    private readonly Action<bool> _setDirectionalEnabled;
    private readonly Action<Vector3> _setDirectionalDirection;
    private readonly Action<Vector3> _setDirectionalColor;
    private readonly Action<float> _setDirectionalIntensity;

    private readonly Action<bool> _setPointEnabled;
    private readonly Action<Vector3> _setPointPosition;
    private readonly Action<Vector3> _setPointColor;
    private readonly Action<float> _setPointIntensity;
    private readonly Action<float, float, float> _setPointAttenuation;

    private readonly Action<Vector3> _setMaterialColor;
    private readonly Action<float> _setMaterialAmbient;
    private readonly Action<float> _setMaterialDiffuse;
    private readonly Action<bool> _setMaterialUnlit;

    private readonly Func<Task> _pause;
    private readonly Func<Task> _resume;

    public VelvetDebugInterop(
        Func<Camera> getCamera,
        Func<DirectionalLightState> getDirectional,
        Func<PointLightState> getPoint,
        Func<Material> getMaterial,
        Action<Vector3> setCameraPosition,
        Action<Vector3> setCameraTarget,
        Action<float, float, float> setCameraPerspective,
        Action<bool> setDirectionalEnabled,
        Action<Vector3> setDirectionalDirection,
        Action<Vector3> setDirectionalColor,
        Action<float> setDirectionalIntensity,
        Action<bool> setPointEnabled,
        Action<Vector3> setPointPosition,
        Action<Vector3> setPointColor,
        Action<float> setPointIntensity,
        Action<float, float, float> setPointAttenuation,
        Action<Vector3> setMaterialColor,
        Action<float> setMaterialAmbient,
        Action<float> setMaterialDiffuse,
        Action<bool> setMaterialUnlit,
        Func<Task> pause,
        Func<Task> resume)
    {
        _getCamera = getCamera;
        _getDirectional = getDirectional;
        _getPoint = getPoint;
        _getMaterial = getMaterial;

        _setCameraPosition = setCameraPosition;
        _setCameraTarget = setCameraTarget;
        _setCameraPerspective = setCameraPerspective;

        _setDirectionalEnabled = setDirectionalEnabled;
        _setDirectionalDirection = setDirectionalDirection;
        _setDirectionalColor = setDirectionalColor;
        _setDirectionalIntensity = setDirectionalIntensity;

        _setPointEnabled = setPointEnabled;
        _setPointPosition = setPointPosition;
        _setPointColor = setPointColor;
        _setPointIntensity = setPointIntensity;
        _setPointAttenuation = setPointAttenuation;

        _setMaterialColor = setMaterialColor;
        _setMaterialAmbient = setMaterialAmbient;
        _setMaterialDiffuse = setMaterialDiffuse;
        _setMaterialUnlit = setMaterialUnlit;

        _pause = pause;
        _resume = resume;
    }

    [JSInvokable]
    public DebugStateDto GetState()
    {
        var cam = _getCamera();
        var dir = _getDirectional();
        var point = _getPoint();
        var material = _getMaterial();

        return new DebugStateDto
        {
            Camera = new CameraDto
            {
                Position = Vec3Dto.From(cam.Position),
                Target = Vec3Dto.From(cam.Target),
                Forward = Vec3Dto.From(cam.Forward),
                FovYRadians = cam.FovYRadians,
                NearPlane = cam.NearPlane,
                FarPlane = cam.FarPlane,
            },
            DirectionalLight = new DirectionalLightDto
            {
                Enabled = dir.Enabled,
                Direction = Vec3Dto.From(dir.Direction),
                Color = ColorDto.From(dir.Color),
                Intensity = dir.Intensity,
            },
            PointLight = new PointLightDto
            {
                Enabled = point.Enabled,
                Position = Vec3Dto.From(point.Position),
                Color = ColorDto.From(point.Color),
                Intensity = point.Intensity,
                Constant = point.Constant,
                Linear = point.Linear,
                Quadratic = point.Quadratic,
            },
            Material = new MaterialDto
            {
                BaseColor = ColorDto.From(material.AlbedoColor),
                Ambient = material.AmbientStrength,
                Diffuse = material.DiffuseStrength,
                Unlit = material.Unlit,
            }
        };
    }

    [JSInvokable]
    public void SetCameraPosition(float x, float y, float z)
        => _setCameraPosition(new Vector3(x, y, z));

    [JSInvokable]
    public void SetCameraTarget(float x, float y, float z)
        => _setCameraTarget(new Vector3(x, y, z));

    [JSInvokable]
    public void SetCameraPerspective(float fovYRadians, float nearPlane, float farPlane)
        => _setCameraPerspective(fovYRadians, nearPlane, farPlane);

    [JSInvokable]
    public void SetDirectionalEnabled(bool enabled)
        => _setDirectionalEnabled(enabled);

    [JSInvokable]
    public void SetDirectionalDirection(float x, float y, float z)
        => _setDirectionalDirection(new Vector3(x, y, z));

    [JSInvokable]
    public void SetDirectionalColor(float r, float g, float b)
        => _setDirectionalColor(new Vector3(r, g, b));

    [JSInvokable]
    public void SetDirectionalIntensity(float intensity)
        => _setDirectionalIntensity(intensity);

    [JSInvokable]
    public void SetPointEnabled(bool enabled)
        => _setPointEnabled(enabled);

    [JSInvokable]
    public void SetPointPosition(float x, float y, float z)
        => _setPointPosition(new Vector3(x, y, z));

    [JSInvokable]
    public void SetPointColor(float r, float g, float b)
        => _setPointColor(new Vector3(r, g, b));

    [JSInvokable]
    public void SetPointIntensity(float intensity)
        => _setPointIntensity(intensity);

    [JSInvokable]
    public void SetPointAttenuation(float constant, float linear, float quadratic)
        => _setPointAttenuation(constant, linear, quadratic);

    [JSInvokable]
    public void SetMaterialColor(float r, float g, float b)
        => _setMaterialColor(new Vector3(r, g, b));

    [JSInvokable]
    public void SetMaterialAmbient(float ambient)
        => _setMaterialAmbient(ambient);

    [JSInvokable]
    public void SetMaterialDiffuse(float diffuse)
        => _setMaterialDiffuse(diffuse);

    [JSInvokable]
    public void SetMaterialUnlit(bool unlit)
        => _setMaterialUnlit(unlit);

    [JSInvokable]
    public Task PauseAsync() => _pause();

    [JSInvokable]
    public Task ResumeAsync() => _resume();
}

internal sealed class DebugStateDto
{
    public CameraDto Camera { get; set; } = new();
    public DirectionalLightDto DirectionalLight { get; set; } = new();
    public PointLightDto PointLight { get; set; } = new();
    public MaterialDto Material { get; set; } = new();
}

internal sealed class MaterialDto
{
    public ColorDto BaseColor { get; set; } = new();
    public float Ambient { get; set; }
    public float Diffuse { get; set; }
    public bool Unlit { get; set; }
}

internal sealed class CameraDto
{
    public Vec3Dto Position { get; set; } = new();
    public Vec3Dto Target { get; set; } = new();
    public Vec3Dto Forward { get; set; } = new();

    public float FovYRadians { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
}

internal sealed class DirectionalLightDto
{
    public bool Enabled { get; set; }
    public Vec3Dto Direction { get; set; } = new();
    public ColorDto Color { get; set; } = new();
    public float Intensity { get; set; }
}

internal sealed class PointLightDto
{
    public bool Enabled { get; set; }
    public Vec3Dto Position { get; set; } = new();
    public ColorDto Color { get; set; } = new();
    public float Intensity { get; set; }

    public float Constant { get; set; }
    public float Linear { get; set; }
    public float Quadratic { get; set; }
}

internal sealed class Vec3Dto
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public static Vec3Dto From(in Vector3 v) => new() { x = v.X, y = v.Y, z = v.Z };
}

internal sealed class ColorDto
{
    public float r { get; set; }
    public float g { get; set; }
    public float b { get; set; }

    public static ColorDto From(in Vector3 v) => new() { r = v.X, g = v.Y, b = v.Z };
}

internal sealed class DirectionalLightState
{
    public bool Enabled { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }

    public DirectionalLightState(bool enabled, Vector3 direction, Vector3 color, float intensity)
    {
        Enabled = enabled;
        Direction = direction;
        Color = color;
        Intensity = intensity;
    }
}

internal sealed class PointLightState
{
    public bool Enabled { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }

    public float Constant { get; set; }
    public float Linear { get; set; }
    public float Quadratic { get; set; }

    public PointLightState(
        bool enabled,
        Vector3 position,
        Vector3 color,
        float intensity,
        float constant,
        float linear,
        float quadratic)
    {
        Enabled = enabled;
        Position = position;
        Color = color;
        Intensity = intensity;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }
}
