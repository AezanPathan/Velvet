using System;
using Velvet.Core.Math;

namespace Velvet.Core.Particles;

/// <summary>
/// Emits new particles. This class never updates particles after spawn.
/// </summary>
public sealed class ParticleEmitter
{
    public ParticleEmitterShape Shape { get; set; } = ParticleEmitterShape.Point;

    /// <summary>
    /// World-space origin of the emitter.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Half-extents of the box when <see cref="Shape"/> is <see cref="ParticleEmitterShape.Box"/>.
    /// </summary>
    public Vector3 BoxExtents { get; set; } = new(0.5f, 0.5f, 0.5f);

    /// <summary>
    /// Spawn rate in particles per second.
    /// </summary>
    public float SpawnRate { get; set; } = 10f;

    /// <summary>
    /// Initial velocity applied to spawned particles.
    /// </summary>
    public Vector3 InitialVelocity { get; set; } = Vector3.Zero;

    internal Vector3 SampleSpawnPosition(Random rng)
    {
        if (Shape == ParticleEmitterShape.Point)
            return Position;

        var x = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.X;
        var y = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.Y;
        var z = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.Z;
        return new Vector3(Position.X + x, Position.Y + y, Position.Z + z);
    }
}
