using Velvet.Core.Math;

namespace Velvet.Core.Particles;

/// <summary>
/// Fixed-size CPU particle system with a reusable pool.
/// Owns and updates particle simulation data.
/// </summary>
public sealed class ParticleSystem
{
    private readonly Vector3[] _positions;
    private readonly Vector3[] _velocities;
    private readonly float[] _ages;
    private readonly float[] _lifetimes;
    private readonly int[] _activeIndices;
    private readonly int[] _activeSlots;
    private int _activeCount;
    private int _freeCount;
    private readonly int[] _freeStack;
    private readonly Random _random = new();
    private float _emitAccumulator;

    public int Capacity { get; }
    public int ActiveCount => _activeCount;

    public ParticleEmitter Emitter { get; }

    public ParticleSystemSettings Settings { get; } = new();

    public ParticleSystem(int capacity, ParticleEmitter emitter)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        ArgumentNullException.ThrowIfNull(emitter);

        Capacity = capacity;
        Emitter = emitter;

        _positions = new Vector3[capacity];
        _velocities = new Vector3[capacity];
        _ages = new float[capacity];
        _lifetimes = new float[capacity];
        _activeIndices = new int[capacity];
        _activeSlots = new int[capacity];
        _freeStack = new int[capacity];

        for (int i = 0; i < capacity; i++)
        {
            _freeStack[i] = capacity - 1 - i;
        }

        _freeCount = capacity;
    }

    /// <summary>
    /// Advances particle simulation on the CPU.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        Emit(deltaTime);

        int i = 0;
        while (i < _activeCount)
        {
            int idx = _activeIndices[i];
            float age = _ages[idx] + deltaTime;
            if (age >= _lifetimes[idx])
            {
                Kill(idx, i);
                continue;
            }

            _ages[idx] = age;
            _positions[idx] = _positions[idx] + _velocities[idx] * deltaTime;
            i++;
        }
    }

    /// <summary>
    /// Writes the current particle render data into <paramref name="buffer"/>.
    /// Layout per particle: position.xyz, size, color.rgba (8 floats).
    /// Returns the number of active particles written.
    /// </summary>
    public int WriteRenderBuffer(float[] buffer)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (buffer.Length < _activeCount * 8)
            throw new ArgumentException("Render buffer is too small for active particles.", nameof(buffer));

        for (int i = 0; i < _activeCount; i++)
        {
            int idx = _activeIndices[i];
            float t = _lifetimes[idx] <= 0f ? 1f : _ages[idx] / _lifetimes[idx];
            t = MathF.Min(1f, MathF.Max(0f, t));

            float size = Lerp(Settings.StartSize, Settings.EndSize, t);
            Vector4 color = Lerp(Settings.StartColor, Settings.EndColor, t);

            int baseIndex = i * 8;
            var p = _positions[idx];
            buffer[baseIndex + 0] = p.X;
            buffer[baseIndex + 1] = p.Y;
            buffer[baseIndex + 2] = p.Z;
            buffer[baseIndex + 3] = size;
            buffer[baseIndex + 4] = color.X;
            buffer[baseIndex + 5] = color.Y;
            buffer[baseIndex + 6] = color.Z;
            buffer[baseIndex + 7] = color.W;
        }

        return _activeCount;
    }

    private void Emit(float deltaTime)
    {
        var rate = MathF.Max(0f, Emitter.SpawnRate);
        _emitAccumulator += deltaTime * rate;
        int spawnCount = (int)_emitAccumulator;
        if (spawnCount <= 0)
            return;

        _emitAccumulator -= spawnCount;
        for (int i = 0; i < spawnCount; i++)
        {
            if (_freeCount <= 0)
                return;

            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        int idx = _freeStack[--_freeCount];

        _positions[idx] = Emitter.SampleSpawnPosition(_random);
        _velocities[idx] = Emitter.SampleSpawnVelocity(_random);
        _ages[idx] = 0f;
        _lifetimes[idx] = Settings.Lifetime;

        _activeSlots[idx] = _activeCount;
        _activeIndices[_activeCount] = idx;
        _activeCount++;
    }

    private void Kill(int particleIndex, int activeSlot)
    {
        int lastSlot = _activeCount - 1;
        if (activeSlot != lastSlot)
        {
            int swappedIndex = _activeIndices[lastSlot];
            _activeIndices[activeSlot] = swappedIndex;
            _activeSlots[swappedIndex] = activeSlot;
        }

        _activeCount--;
        _freeStack[_freeCount++] = particleIndex;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static Vector4 Lerp(in Vector4 a, in Vector4 b, float t)
        => new(a.X + (b.X - a.X) * t,
               a.Y + (b.Y - a.Y) * t,
               a.Z + (b.Z - a.Z) * t,
               a.W + (b.W - a.W) * t);
}

public sealed class ParticleSystemSettings
{
    public float Lifetime { get; set; } = 1.5f;
    public float StartSize { get; set; } = 8f;
    public float EndSize { get; set; } = 2f;
    public Vector4 StartColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 EndColor { get; set; } = new(1f, 1f, 1f, 0f);
    public ParticleBlendMode BlendMode { get; set; } = ParticleBlendMode.Alpha;
}
