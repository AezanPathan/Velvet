using Velvet.Core.Math;

namespace Velvet.Core.Particles;

/// <summary>
/// Emits new particles. This class never updates particles after spawn.
/// </summary>
public sealed class ParticleEmitter
{
    public ParticleEmitterShape Shape { get; set; } = ParticleEmitterShape.Point;

    public Vector3 Position { get; set; } = Vector3.Zero;

    public Vector3 BoxExtents { get; set; } = new(0.5f, 0.5f, 0.5f);

    public float SpawnRate { get; set; } = 10f;

    public Vector3 InitialVelocity { get; set; } = Vector3.Zero;

    public Vector3 VelocityMin { get; set; } = Vector3.Zero;
    public Vector3 VelocityMax { get; set; } = Vector3.Zero;

    internal Vector3 SampleSpawnPosition(Random rng)
    {
        if (Shape == ParticleEmitterShape.Point) return Position;

        var x = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.X;
        var y = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.Y;
        var z = (float)(rng.NextDouble() * 2.0 - 1.0) * BoxExtents.Z;
        return new Vector3(Position.X + x, Position.Y + y, Position.Z + z);
    }

    internal Vector3 SampleSpawnVelocity(Random rng)
    {
        var randomX = (float)(rng.NextDouble() * (VelocityMax.X - VelocityMin.X) + VelocityMin.X);
        var randomY = (float)(rng.NextDouble() * (VelocityMax.Y - VelocityMin.Y) + VelocityMin.Y);
        var randomZ = (float)(rng.NextDouble() * (VelocityMax.Z - VelocityMin.Z) + VelocityMin.Z);
        return InitialVelocity + new Vector3(randomX, randomY, randomZ);
    }
}
