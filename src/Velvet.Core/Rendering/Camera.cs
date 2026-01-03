using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// perspective camera.
/// Produces view and projection matrices (column-major).
/// Pure C#; no rendering, no JS, no UI concerns.
/// </summary>
public sealed class Camera
{
	// --- View state (world space) ---

	private Vector3 _position;
	private Vector3 _target;
	private Vector3 _up;

	// --- Projection state ---

	private float _fovYRadians;
	private float _aspectRatio;
	private float _nearPlane;
	private float _farPlane;

	// --- Dirty flags ---

	private bool _viewDirty = true;
	private bool _projectionDirty = true;

	// --- Cached matrices (column-major) ---

	private float[] _view = Matrix.Identity();
	private float[] _projection = Matrix.Identity();

	/// <summary>
	/// Creates a camera with fully specified perspective parameters.
	/// Use <see cref="CreatePerspective"/> for cleaner call sites.
	/// </summary>
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

	/// <summary>
	/// Convenience factory for a standard perspective camera.
	/// </summary>
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

	// --- View properties ---

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
	/// Normalized forward direction derived from Position and Target.
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

	// --- Projection properties ---

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
			if (_farPlane > 0f && value >= _farPlane)
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
	/// Updates all perspective parameters atomically.
	/// </summary>
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
	}

	/// <summary>
	/// Updates aspect ratio from a viewport size.
	/// </summary>
	public void SetViewportSize(float width, float height)
	{
		if (width <= 0f || height <= 0f)
			throw new ArgumentOutOfRangeException("Viewport dimensions must be > 0.");

		AspectRatio = width / height;
	}

	// --- Matrices ---

	/// <summary>
	/// Column-major view matrix.
	/// Recomputed only when view state changes.
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
	/// Recomputed only when projection parameters change.
	/// </summary>
	public float[] ProjectionMatrix
	{
		get
		{
			if (_projectionDirty)
			{
				_projection = Matrix.Perspective(
					_fovYRadians,
					_aspectRatio,
					_nearPlane,
					_farPlane);

				_projectionDirty = false;
			}

			return _projection;
		}
	}
}
