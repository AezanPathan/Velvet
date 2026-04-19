namespace Velvet.Core.Rendering.Culling;

using Velvet.Core.Math;
using Velvet.Core.Rendering.Bounds;

public sealed class Frustum
{
    public readonly struct Plane
    {
        public Plane(Vector3 normal, float distance)
        {
            Normal = normal;
            Distance = distance;
        }

        public Vector3 Normal { get; }

        public float Distance { get; }
    }

    private readonly Plane[] _planes = new Plane[6];

    public void UpdateFromMatrix(float[] viewProjectionMatrix)
    {
        ArgumentNullException.ThrowIfNull(viewProjectionMatrix);
        if (viewProjectionMatrix.Length != 16)
            throw new ArgumentException("View-projection matrix must be 4x4.", nameof(viewProjectionMatrix));

        var m = viewProjectionMatrix;

        var r0x = m[0];
        var r0y = m[4];
        var r0z = m[8];
        var r0w = m[12];

        var r1x = m[1];
        var r1y = m[5];
        var r1z = m[9];
        var r1w = m[13];

        var r2x = m[2];
        var r2y = m[6];
        var r2z = m[10];
        var r2w = m[14];

        var r3x = m[3];
        var r3y = m[7];
        var r3z = m[11];
        var r3w = m[15];

        _planes[0] = CreatePlane(r3x + r0x, r3y + r0y, r3z + r0z, r3w + r0w);
        _planes[1] = CreatePlane(r3x - r0x, r3y - r0y, r3z - r0z, r3w - r0w);
        _planes[2] = CreatePlane(r3x - r1x, r3y - r1y, r3z - r1z, r3w - r1w);
        _planes[3] = CreatePlane(r3x + r1x, r3y + r1y, r3z + r1z, r3w + r1w);
        _planes[4] = CreatePlane(r3x + r2x, r3y + r2y, r3z + r2z, r3w + r2w);
        _planes[5] = CreatePlane(r3x - r2x, r3y - r2y, r3z - r2z, r3w - r2w);
    }

    public bool Intersects(BoundingBox box)
    {
        for (var i = 0; i < _planes.Length; i++)
        {
            var plane = _planes[i];
            var n = plane.Normal;

            var x = n.X >= 0f ? box.Max.X : box.Min.X;
            var y = n.Y >= 0f ? box.Max.Y : box.Min.Y;
            var z = n.Z >= 0f ? box.Max.Z : box.Min.Z;

            if ((n.X * x) + (n.Y * y) + (n.Z * z) + plane.Distance < 0f)
            {
                return false;
            }
        }

        return true;
    }

    private static Plane CreatePlane(float x, float y, float z, float distance)
    {
        var length = MathF.Sqrt((x * x) + (y * y) + (z * z));
        if (length <= float.Epsilon)
            return new Plane(Vector3.Zero, 0f);

        var inv = 1f / length;
        return new Plane(new Vector3(x * inv, y * inv, z * inv), distance * inv);
    }
}