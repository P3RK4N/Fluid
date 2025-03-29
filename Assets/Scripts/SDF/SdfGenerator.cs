using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class SdfGenerator : MonoBehaviour
{
    [EditorOnly] public Vector3Int resolution = new Vector3Int(10, 10, 10);

    BoxCollider sdfBounds;

    List<Collider> collidingObjects;

    Texture3D field;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        sdfBounds = GetComponent<BoxCollider>();
        GenerateSdf();
    }

    public void GenerateSdf(Transform tf = null)
    {
        if (tf)
        {
            transform.position = tf.position;
            transform.localScale = tf.localScale;
        }

        var sdfColliders = FindObjectsByType<SdfCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        collidingObjects = new List<Collider>();

        foreach (var sdfCollider in sdfColliders)
        {
            // Check if the collider's bounds intersect with sdfBounds
            Collider otherCollider = sdfCollider.GetComponent<Collider>();
            if (sdfBounds.bounds.Intersects(otherCollider.bounds))
            {
                collidingObjects.Add(otherCollider);
            }
        }

        field = new Texture3D(resolution.x, resolution.y, resolution.z, TextureFormat.R8, false);
        field.filterMode = FilterMode.Bilinear;
        field.wrapMode = TextureWrapMode.Clamp;

        // Step through the bounds of sdfBounds to generate uniform points
        Vector3 boundsMin = sdfBounds.bounds.min;
        Vector3 boundsMax = sdfBounds.bounds.max;
        byte[] fieldValues = new byte[resolution.x * resolution.y * resolution.z];
        int index = 0;

        for (int x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                for (int z = 0; z < resolution.z; z++)
                {
                    // Get the position of the current point in the grid
                    Vector3 point = new Vector3(
                        Mathf.Lerp(boundsMin.z, boundsMax.z, (float)z / resolution.z),
                        Mathf.Lerp(boundsMin.y, boundsMax.y, (float)y / resolution.y),
                        Mathf.Lerp(boundsMin.x, boundsMax.x, (float)x / resolution.x)
                    );


                    // Calculate the distance from the point to the nearest surface of any collider
                    fieldValues[index++] = IsPointInsideOrCloseToCollider(point) ? (byte)255 : (byte)0;
                }
            }
        }

        field.SetPixelData(fieldValues, 0);
    }

    Vector3 positionToUVW(float x, float y, float z)
    {
        Vector3 boundsMin = sdfBounds.bounds.min;
        Vector3 boundsMax = sdfBounds.bounds.max;

        float u = Mathf.InverseLerp(boundsMin.x, boundsMax.x, x);
        float v = Mathf.InverseLerp(boundsMin.y, boundsMax.y, y);
        float w = Mathf.InverseLerp(boundsMin.z, boundsMax.z, z);

        return new Vector3(u, v, w);
    }

    float positionToValue(int x, int y, int z)
    {
        var uvw = positionToUVW(x, y, z);
        return (uint)(field.GetPixelBilinear(uvw.x, uvw.y, uvw.z).r * 255);
    }

    private bool IsPointInsideOrCloseToCollider(Vector3 point)
    {
        foreach (var collider in collidingObjects)
        {
            // Get the closest point on the collider to the test point
            Vector3 closestPoint = collider.ClosestPoint(point);

            // Calculate distance from the point to the closest point on the collider
            float distance = Vector3.Distance(point, closestPoint);

            // You can set a threshold distance for "close enough" (optional)
            float threshold = 0.0f; // Adjust threshold as needed

            if (distance <= threshold)
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 boundsMin = sdfBounds.bounds.min;
        Vector3 boundsMax = sdfBounds.bounds.max;

        // Resolution of 20 per axis
        int resX = 20;
        int resY = 20;
        int resZ = 20;

        // Iterate through the resolution and draw cubes
        for (int x = 0; x < resX; x++)
        {
            for (int y = 0; y < resY; y++)
            {
                for (int z = 0; z < resZ; z++)
                {
                    // Get the position of the current point in the grid
                    Vector3 point = new Vector3(
                        Mathf.Lerp(boundsMin.x, boundsMax.x, (float)x / resX),
                        Mathf.Lerp(boundsMin.y, boundsMax.y, (float)y / resY),
                        Mathf.Lerp(boundsMin.z, boundsMax.z, (float)z / resZ)
                    );

                    // Convert grid position to texture space (UVW)
                    Vector3 uvw = positionToUVW(point.x, point.y, point.z);

                    // Get the value from the field (grayscale value)
                    float value = field.GetPixelBilinear(uvw.x, uvw.y, uvw.z).r;

                    // Set the Gizmo color based on the value (grayscale)
                    Gizmos.color = new Color(value, value, value, value);

                    // Draw a small cube at the point
                    Gizmos.DrawCube(point, Vector3.one * 0.1f);
                }
            }
        }
    }
}
