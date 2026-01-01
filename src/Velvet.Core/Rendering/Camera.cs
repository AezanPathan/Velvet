using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// Minimal engine-grade camera: provides view and perspective projection matrices.
/// Pure C#; no JS-side camera logic.
/// </summary>
public sealed class Camera
{
	private Vector3 _position;
	private Vector3 _target;
	private Vector3 _up;

	private float _fovYRadians;
	private float _aspectRatio;
	private float _nearPlane;
	private float _farPlane;

	private bool _viewDirty = true;
	private bool _projectionDirty = true;

	private float[] _view = Matrix.Identity();
	private float[] _projection = Matrix.Identity();

	public Camera(
		Vector3 position,
		Vector3 target,
		Vector3 up,
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

	public Vector3 Position
	{
		get => _position;
		set
		{
			_position = value;
			_viewDirty = true;
		}
	}

	public Vector3 Target
	{
		get => _target;
		set
		{
			_target = value;
			_viewDirty = true;
		}
	}

	/// <summary>
	/// Forward direction (normalized), derived from <see cref="Position"/> and <see cref="Target"/>.
	/// </summary>
	public Vector3 Forward => (_target - _position).Normalized();

	public Vector3 Up
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

	/// <summary>
	/// Field of view (vertical) in radians.
	/// Alias for <see cref="FovYRadians"/>.
	/// </summary>
	public float FieldOfViewRadians
	{
		get => FovYRadians;
		set => FovYRadians = value;
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

	/// <summary>
	/// Convenience helper to update aspect ratio from a viewport size.
	/// </summary>
	public void SetViewportSize(float width, float height)
	{
		if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width), "Viewport width must be > 0.");
		if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height), "Viewport height must be > 0.");
		AspectRatio = width / height;
	}

	public float NearPlane
	{
		get => _nearPlane;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Near plane must be > 0.");
			if (_farPlane > 0f && _farPlane <= value)
				throw new ArgumentOutOfRangeException(nameof(value), "Near plane must be < far plane.");
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
			if (_nearPlane > 0f && value <= _nearPlane)
				throw new ArgumentOutOfRangeException(nameof(value), "Far plane must be > near plane.");
			_farPlane = value;
			_projectionDirty = true;
		}
	}

	/// <summary>
	/// Sets all perspective parameters in one call (avoids transient invalid states).
	/// </summary>
	public void SetPerspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
	{
		if (fovYRadians <= 0f || fovYRadians >= MathF.PI)
			throw new ArgumentOutOfRangeException(nameof(fovYRadians), "FOV must be in (0, PI) radians.");
		if (aspectRatio <= 0f)
			throw new ArgumentOutOfRangeException(nameof(aspectRatio), "Aspect ratio must be > 0.");
		if (nearPlane <= 0f)
			throw new ArgumentOutOfRangeException(nameof(nearPlane), "Near plane must be > 0.");
		if (farPlane <= nearPlane)
			throw new ArgumentOutOfRangeException(nameof(farPlane), "Far plane must be > near plane.");

		_fovYRadians = fovYRadians;
		_aspectRatio = aspectRatio;
		_nearPlane = nearPlane;
		_farPlane = farPlane;
		_projectionDirty = true;
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
				_view = Matrix.LookAt(_position, _target, _up);
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
				_projection = Matrix.Perspective(_fovYRadians, _aspectRatio, _nearPlane, _farPlane);
				_projectionDirty = false;
			}

			return _projection;
		}
	}
}
