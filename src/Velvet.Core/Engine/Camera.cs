using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// Minimal engine-grade camera: provides view and perspective projection matrices.
/// Pure C#; no JS-side camera logic.
/// </summary>
public sealed class Camera
{
	private Vec3 _position;
	private Vec3 _target;
	private Vec3 _up;

	private float _fovYRadians;
	private float _aspectRatio;
	private float _nearPlane;
	private float _farPlane;

	private bool _viewDirty = true;
	private bool _projectionDirty = true;

	private float[] _view = Mat4.Identity();
	private float[] _projection = Mat4.Identity();

	public Camera(
		Vec3 position,
		Vec3 target,
		Vec3 up,
		float fovYRadians,
		float aspectRatio,
		float nearPlane,
		float farPlane)
	{
		_position = position;
		_target = target;
		_up = up;

		FovYRadians = fovYRadians;
		AspectRatio = aspectRatio;
		NearPlane = nearPlane;
		FarPlane = farPlane;

		_viewDirty = true;
		_projectionDirty = true;
	}

	public Vec3 Position
	{
		get => _position;
		set
		{
			_position = value;
			_viewDirty = true;
		}
	}

	public Vec3 Target
	{
		get => _target;
		set
		{
			_target = value;
			_viewDirty = true;
		}
	}

	public Vec3 Up
	{
		get => _up;
		set
		{
			_up = value;
			_viewDirty = true;
		}
	}

	/// <summary>
	/// Vertical field of view in radians.
	/// </summary>
	public float FovYRadians
	{
		get => _fovYRadians;
		set
		{
			if (value <= 0f || value >= MathF.PI)
				throw new ArgumentOutOfRangeException(nameof(value), "FOV must be in (0, PI) radians.");
			_fovYRadians = value;
			_projectionDirty = true;
		}
	}

	public float AspectRatio
	{
		get => _aspectRatio;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Aspect ratio must be > 0.");
			_aspectRatio = value;
			_projectionDirty = true;
		}
	}

	public float NearPlane
	{
		get => _nearPlane;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Near plane must be > 0.");
			_nearPlane = value;
			_projectionDirty = true;
		}
	}

	public float FarPlane
	{
		get => _farPlane;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Far plane must be > 0.");
			_farPlane = value;
			_projectionDirty = true;
		}
	}

	/// <summary>
	/// Column-major view matrix.
	/// </summary>
	public float[] ViewMatrix
	{
		get
		{
			if (_viewDirty)
			{
				_view = Mat4.LookAt(_position, _target, _up);
				_viewDirty = false;
			}

			return _view;
		}
	}

	/// <summary>
	/// Column-major projection matrix.
	/// </summary>
	public float[] ProjectionMatrix
	{
		get
		{
			if (_projectionDirty)
			{
				_projection = Mat4.Perspective(_fovYRadians, _aspectRatio, _nearPlane, _farPlane);
				_projectionDirty = false;
			}

			return _projection;
		}
	}
}
