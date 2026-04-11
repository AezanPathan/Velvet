namespace Velvet.Core.Rendering.Cameras.Controllers;

using System;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Cameras;

public class OrbitController
{
	private Vector3 _target;
	private float _yaw;
	private float _pitch;
	private float _distance;

	private float _minDistance;
	private float _maxDistance;
	private float _minPitch;
	private float _maxPitch;

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

		_minPitch = -1.4835f;
		_maxPitch = 1.4835f;

		ClampPitch();
	}

	public Vector3 Target
	{
		get => _target;
		set => _target = value;
	}

	public float Yaw
	{
		get => _yaw;
		set => _yaw = value;
	}

	public float Pitch
	{
		get => _pitch;
		set
		{
			_pitch = value;
			ClampPitch();
		}
	}

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

	public float MinPitch
	{
		get => _minPitch;
		set => _minPitch = value;
	}

	public float MaxPitch
	{
		get => _maxPitch;
		set => _maxPitch = value;
	}

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

	public void ApplyYaw(float deltaYaw)
	{
		_yaw += deltaYaw;
	}

	public void ApplyPitch(float deltaPitch)
	{
		_pitch += deltaPitch;
		ClampPitch();
	}

	public void ApplyZoom(float deltaDistance)
	{
		_distance += deltaDistance;
		ClampDistance();
	}

	public void ApplyZoomMultiplier(float multiplier)
	{
		if (multiplier <= 0f)
			throw new ArgumentOutOfRangeException(nameof(multiplier), "Zoom multiplier must be > 0.");

		_distance *= multiplier;
		ClampDistance();
	}

	public void UpdateCamera(Camera camera)
	{
		ArgumentNullException.ThrowIfNull(camera);

		camera.Target = _target;

		// Spherical coordinates around target.
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