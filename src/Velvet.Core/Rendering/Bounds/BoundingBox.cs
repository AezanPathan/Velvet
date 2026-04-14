using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Bounds;

/// <summary>
/// Axis-aligned bounding box (AABB) in 3D space.
/// Stores minimum and maximum extents of the box.
/// </summary>
public struct BoundingBox
{
    /// <summary>
    /// Minimum corner of the bounding box.
    /// </summary>
    public Vector3 Min { get; set; }

    /// <summary>
    /// Maximum corner of the bounding box.
    /// </summary>
    public Vector3 Max { get; set; }

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Center point of the bounding box.
    /// </summary>
    public Vector3 Center => new(
        (Min.X + Max.X) * 0.5f,
        (Min.Y + Max.Y) * 0.5f,
        (Min.Z + Max.Z) * 0.5f);

    /// <summary>
    /// Size (dimensions) of the bounding box.
    /// </summary>
    public Vector3 Size => new(
        Max.X - Min.X,
        Max.Y - Min.Y,
        Max.Z - Min.Z);

    /// <summary>
    /// Radius of the bounding box (distance from center to farthest corner).
    /// </summary>
    public float Radius => (Size * 0.5f).Length;

    /// <summary>
    /// Create a bounding box from a single point.
    /// </summary>
    public static BoundingBox FromPoint(Vector3 point)
        => new(point, point);

    /// <summary>
    /// Expand the bounding box to include another point.
    /// </summary>
    public void Expand(Vector3 point)
    {
        Min = new Vector3(
            System.MathF.Min(Min.X, point.X),
            System.MathF.Min(Min.Y, point.Y),
            System.MathF.Min(Min.Z, point.Z));

        Max = new Vector3(
            System.MathF.Max(Max.X, point.X),
            System.MathF.Max(Max.Y, point.Y),
            System.MathF.Max(Max.Z, point.Z));
    }

    /// <summary>
    /// Expand the bounding box to include another bounding box.
    /// </summary>
    public void Expand(BoundingBox other)
    {
        Expand(other.Min);
        Expand(other.Max);
    }
}
