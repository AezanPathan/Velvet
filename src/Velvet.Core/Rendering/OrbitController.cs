using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// Minimal orbit controller for camera manipulation.
/// Orbits around a target point with yaw (horizontal) and pitch (vertical) rotation.
/// Updates camera position based on internal state; does not handle input directly.
/// Three.js–style OrbitControls behavior without smoothing or inertia.
/// </summary>
public class OrbitController
{
	private Vector3 _target;
	private float _yaw;        // Radians; rotation around up axis (Y)
	private float _pitch;      // Radians; elevation from horizontal plane
	private float _distance;   // Distance from target to camera

	private float _minDistance;
	private float _maxDistance;
	private float _minPitch;   // Radians (typically ≈ -85°)
	private float _maxPitch;   // Radians (typically ≈ +85°)

	/// <summary>
	/// Creates an orbit controller with default pitch clamping at ±85°.
	/// </summary>
	public OrbitController(
		Vector3 target,
		float yaw = 0f,
		float pitch = 0f,
		float distance = 5f,
		float minDistance = 0.1f,
		float maxDistance = 1000f)
	{
		if (distance <= 0f)
			throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be > 0.");
		if (minDistance <= 0f)
			throw new ArgumentOutOfRangeException(nameof(minDistance), "Min distance must be > 0.");
		if (maxDistance <= minDistance)
			throw new ArgumentOutOfRangeException(nameof(maxDistance), "Max distance must be > min distance.");

		_target = target;
		_yaw = yaw;
		_pitch = pitch;
		_distance = distance;
		_minDistance = minDistance;
		_maxDistance = maxDistance;

		// Default pitch clamping: ±85° to avoid gimbal lock and camera flipping.
		// Convert 85 degrees to radians: 85° * π/180 ≈ 1.4835 rad
		_minPitch = -1.4835f;
		_maxPitch = 1.4835f;

		// Clamp initial pitch.
		ClampPitch();
	}

	/// <summary>
	/// The point around which the camera orbits.
	/// </summary>
	public Vector3 Target
	{
		get => _target;
		set => _target = value;
	}

	/// <summary>
	/// Horizontal rotation in radians (around the up/Y axis).
	/// </summary>
	public float Yaw
	{
		get => _yaw;
		set => _yaw = value;
	}

	/// <summary>
	/// Vertical rotation in radians (elevation from the horizontal plane).
	/// Automatically clamped to minPitch/maxPitch when set.
	/// </summary>
	public float Pitch
	{
		get => _pitch;
		set
		{
			_pitch = value;
			ClampPitch();
		}
	}

	/// <summary>
	/// Distance from target to camera.
	/// </summary>
	public float Distance
	{
		get => _distance;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Distance must be > 0.");

			_distance = value;
			ClampDistance();
		}
	}

	/// <summary>
	/// Minimum allowed pitch in radians (default ≈ -85°).
	/// </summary>
	public float MinPitch
	{
		get => _minPitch;
		set => _minPitch = value;
	}

	/// <summary>
	/// Maximum allowed pitch in radians (default ≈ +85°).
	/// </summary>
	public float MaxPitch
	{
		get => _maxPitch;
		set => _maxPitch = value;
	}

	/// <summary>
	/// Minimum allowed distance (for zoom limits).
	/// </summary>
	public float MinDistance
	{
		get => _minDistance;
		set
		{
			if (value <= 0f)
				throw new ArgumentOutOfRangeException(nameof(value), "Min distance must be > 0.");
			_minDistance = value;
			ClampDistance();
		}
	}

	/// <summary>
	/// Maximum allowed distance (for zoom limits).
	/// </summary>
	public float MaxDistance
	{
		get => _maxDistance;
		set
		{
			if (value <= _minDistance)
				throw new ArgumentOutOfRangeException(nameof(value), "Max distance must be > min distance.");
			_maxDistance = value;
			ClampDistance();
		}
	}

	/// <summary>
	/// Applies a yaw change (horizontal rotation) in radians.
	/// Positive values rotate counterclockwise (when viewed from above).
	/// </summary>
	public void ApplyYaw(float deltaYaw)
	{
		_yaw += deltaYaw;
	}

	/// <summary>
	/// Applies a pitch change (vertical rotation) in radians.
	/// Positive values rotate upward; automatically clamped to prevent flipping.
	/// </summary>
	public void ApplyPitch(float deltaPitch)
	{
		_pitch += deltaPitch;
		ClampPitch();
	}

	/// <summary>
	/// Applies a distance change (zoom).
	/// Positive values move camera away from target; automatically clamped.
	/// </summary>
	public void ApplyZoom(float deltaDistance)
	{
		_distance += deltaDistance;
		ClampDistance();
	}

	/// <summary>
	/// Applies a multiplicative zoom (e.g., 0.95 zooms in, 1.05 zooms out).
	/// Automatically clamped to distance limits.
	/// </summary>
	public void ApplyZoomMultiplier(float multiplier)
	{
		if (multiplier <= 0f)
			throw new ArgumentOutOfRangeException(nameof(multiplier), "Zoom multiplier must be > 0.");

		_distance *= multiplier;
		ClampDistance();
	}

	/// <summary>
	/// Updates the camera position and target based on current controller state.
	/// Call this every frame after applying input.
	/// </summary>
	public void UpdateCamera(Camera camera)
	{
		ArgumentNullException.ThrowIfNull(camera);

		// Set the camera target.
		camera.Target = _target;

		// Compute camera position using spherical coordinates.
		// Position = Target + distance * (cos(pitch)*sin(yaw), sin(pitch), cos(pitch)*cos(yaw))
		var cosPitch = MathF.Cos(_pitch);
		var sinPitch = MathF.Sin(_pitch);
		var sinYaw = MathF.Sin(_yaw);
		var cosYaw = MathF.Cos(_yaw);

		var offset = new Vector3(
			cosPitch * sinYaw,
			sinPitch,
			cosPitch * cosYaw
		) * _distance;

		camera.Position = _target + offset;
	}

	/// <summary>
	/// Resets the controller to a default state facing the target.
	/// </summary>
	public void Reset(Vector3 target, float distance = 5f)
	{
		_target = target;
		_yaw = 0f;
		_pitch = 0f;
		_distance = distance;
		ClampDistance();
	}

	private void ClampPitch()
	{
		if (_pitch < _minPitch)
			_pitch = _minPitch;
		if (_pitch > _maxPitch)
			_pitch = _maxPitch;
	}

	private void ClampDistance()
	{
		if (_distance < _minDistance)
			_distance = _minDistance;
		if (_distance > _maxDistance)
			_distance = _maxDistance;
	}
}
