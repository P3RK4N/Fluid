using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ColliderPreviewer : Previewer
{
    [SerializeField]
    Vector2 center = Vector2.zero;

    [SerializeField]
    float size = 0.5f;

    [SerializeField]
    Vector2 testPoint = Vector2.zero;

    [SerializeField]
    float testRadius = 0.01f;

    [SerializeField]
    float thickness = 0.01f;

    FluidCollider<Vector2> collider;

    protected override void preDraw()
    {
        collider = new InverseBoxCollider2D(center, size);
    }

    protected override Color draw(Vector2 coords)
    {
        Color pixel = Style.DarkColor;

        var coordResult = collider.getClosestPoint(coords);
        var testResult = collider.getClosestPoint(testPoint);

        // Collider border
        if (coordResult.distance2 < thickness * thickness)
        {
            pixel = Color.gray;
        }
        // Test point
        if (Vector2.Distance(coords, testPoint) < thickness)
        {
            pixel = Style.LightColor;
        }
        // Closest point
        if (Vector2.Distance(coords, testResult.point) < thickness)
        {
            pixel = Color.white;
        }


        return pixel;
    }

    private void OnDrawGizmos()
    {
        var testResult = collider.getClosestPoint(testPoint);

        Gizmos.DrawLine(coordToWorldPos(testResult.point), coordToWorldPos(testResult.point + testResult.normal * 0.02f));
    }

    protected override Vector2 transformCoords(int x, int y)
    {
        return new Vector2((float)x / resolution.x * scale.x, (float)y / resolution.y * scale.x);
    }

    private Vector3 coordToWorldPos(Vector2 coord)
    {
        Vector3 localPosition = new Vector3(0, coord.y, coord.x - 0.5f); // Y up, Z right

        // Apply object transform (scale, rotation, position)
        return transform.TransformPoint(localPosition);
    }
}
