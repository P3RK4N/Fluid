
using UnityEngine;
using static UnityEditor.PlayerSettings;

class InverseBoxCollider2D : FluidCollider<Vector2>
{
    public readonly Vector2 center;
    public readonly float size;

    public readonly Vector2 min, max;

    public InverseBoxCollider2D(Vector2 center, float size)
    {
        this.center = center;
        this.size = size;

        Vector2 halfSize = new Vector2(size / 2, size / 2);
        min = center - halfSize;
        max = center + halfSize;
    }

    public override ColliderQueryResult getClosestPoint(Vector2 point)
    {
        ColliderQueryResult result = default;

        // Clamp inside
        result.point.x = Mathf.Clamp(point.x, min.x, max.x);
        result.point.y = Mathf.Clamp(point.y, min.y, max.y);

        // Project onto box
        float dxMin = result.point.x - min.x;
        float dxMax = max.x - result.point.x;
        float dyMin = result.point.y - min.y;
        float dyMax = max.y - result.point.y;

        if (dxMin < dxMax && dxMin < dyMin && dxMin < dyMax)
        {
            result.point.x = min.x;
            result.normal = Vector2.right;
            result.distance2 = Vector2.SqrMagnitude(point - result.point);
        }
        else if (dxMax < dyMin && dxMax < dyMax)
        {
            result.point.x = max.x;
            result.normal = Vector2.left;
            result.distance2 = Vector2.SqrMagnitude(point - result.point);
        }
        else if (dyMin < dyMax)
        {
            result.point.y = min.y;
            result.normal = Vector2.up;
            result.distance2 = Vector2.SqrMagnitude(point - result.point);
        }
        else
        {
            result.point.y = max.y;
            result.normal = Vector2.down;
            result.distance2 = Vector2.SqrMagnitude(point - result.point);
        }

        return result;
    }

    public override bool isPenetrating(Vector2 point, float radius, ColliderQueryResult result)
    {
        return Vector2.Dot(point - result.point, result.normal) < 0.0f || result.distance2 < radius * radius;
    }

}
