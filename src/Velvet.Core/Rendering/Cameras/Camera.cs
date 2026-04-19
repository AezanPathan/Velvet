namespace Velvet.Core.Rendering.Cameras;

using Velvet.Core.Rendering.Bounds;
using Velvet.Core.Math;

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
	private bool _viewProjectionDirty = true;

	private float[] _view = Matrix4.Identity.Data;
	private float[] _projection = Matrix4.Identity.Data;
	private float[] _viewProjection = Matrix4.Identity.Data;

	public Camera()
	{
		_position = new Vector3(0f, 0f, 5f);
		_target = Vector3.Zero;
		_up = Vector3.UnitY;

		SetPerspective(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
	}

	public Camera(Vector3 position,
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

		SetPerspective(fovYRadians, aspectRatio, nearPlane, farPlane);
	}

	public Vector3 Position
	{
		get => _position;
		set
		{
			_position = value;
			_viewDirty = true;
			_viewProjectionDirty = true;
		}
	}

	public Vector3 Target
	{
		get => _target;
		set
		{
			_target = value;
			_viewDirty = true;
			_viewProjectionDirty = true;
		}
	}

	public Vector3 Forward => (_target - _position).Normalized();

	public Vector3 Up
	{
		get => _up;
		set
		{
			_up = value;
			_viewDirty = true;
			_viewProjectionDirty = true;
		}
	}

	public float Fov
	{
		get => _fovYRadians;
		set
		{
			if (value <= 0f || value >= MathF.PI)
				throw new ArgumentOutOfRangeException(nameof(value), "FOV must be in (0, PI) radians.");

			_fovYRadians = value;
			_projectionDirty = true;
			_viewProjectionDirty = true;
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
			_viewProjectionDirty = true;
		}
	}

	public float NearPlane
	{
		get => _nearPlane;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Near plane must be > 0.");
			if (_farPlane > 0f && value >= _farPlane)
				throw new ArgumentOutOfRangeException(nameof(value), "Near plane must be < far plane.");

			_nearPlane = value;
			_projectionDirty = true;
			_viewProjectionDirty = true;
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
			_viewProjectionDirty = true;
		}
	}

	public float[] ViewMatrix
	{
		get
		{
			if (_viewDirty)
			{
				_view = Matrix4.LookAt(_position, _target, _up).Data;
				_viewDirty = false;
				_viewProjectionDirty = true;
			}

			return _view;
		}
	}

	public float[] ProjectionMatrix
	{
		get
		{
			if (_projectionDirty)
			{
				_projection = Matrix4.Perspective(
					_fovYRadians,
					_aspectRatio,
					_nearPlane,
					_farPlane).Data;

				_projectionDirty = false;
				_viewProjectionDirty = true;
			}

			return _projection;
		}
	}

	public float[] ViewProjectionMatrix
	{
		get
		{
			if (_viewProjectionDirty)
			{
				_viewProjection = Matrix.Multiply(ProjectionMatrix, ViewMatrix);
				_viewProjectionDirty = false;
			}

			return _viewProjection;
		}
	}

	public void SetPerspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
	{
		if (fovYRadians <= 0f || fovYRadians >= MathF.PI)
			throw new ArgumentOutOfRangeException(nameof(fovYRadians));
		if (aspectRatio <= 0f)
			throw new ArgumentOutOfRangeException(nameof(aspectRatio));
		if (nearPlane <= 0f)
			throw new ArgumentOutOfRangeException(nameof(nearPlane));
		if (farPlane <= nearPlane)
			throw new ArgumentOutOfRangeException(nameof(farPlane));

		_fovYRadians = fovYRadians;
		_aspectRatio = aspectRatio;
		_nearPlane = nearPlane;
		_farPlane = farPlane;
		_projectionDirty = true;
		_viewProjectionDirty = true;
	}

	public void SetViewportSize(float width, float height)
	{
		if (width <= 0f || height <= 0f)
			throw new ArgumentOutOfRangeException("Viewport dimensions must be > 0.");

		AspectRatio = width / height;
	}

	public void UpdateProjection()
	{
		_projectionDirty = true;
		_viewProjectionDirty = true;
	}

	public void Frame(BoundingBox bounds, float frameMultiplier = 1.2f)
	{
		if (frameMultiplier <= 0f)
			throw new ArgumentOutOfRangeException(nameof(frameMultiplier), "Frame multiplier must be > 0.");

		Target = bounds.Center;

		// distance = radius / tan(fov / 2)
		var halfFovY = _fovYRadians * 0.5f;
		var tanHalfFov = MathF.Tan(halfFovY);
		var distance = bounds.Radius / tanHalfFov;

		distance *= frameMultiplier;

		var forward = Forward;
		Position = _target - (forward * distance);

		var nearPlane = MathF.Max(0.01f, distance * 0.01f);
		var farPlane = distance * 10f;

		SetPerspective(_fovYRadians, _aspectRatio, nearPlane, farPlane);
	}

	public static Camera CreatePerspective(Vector3 position,
		Vector3 target,
		float fovYRadians,
		float aspectRatio,
		float nearPlane = 0.1f,
		float farPlane = 100f)
	{
		return new Camera(position,
			target,
			Vector3.UnitY,
			fovYRadians,
			aspectRatio,
			nearPlane,
			farPlane);
	}
}
