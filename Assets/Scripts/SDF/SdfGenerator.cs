using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class SdfGenerator : MonoBehaviour
{
    [EditorOnly] public ComputeShader sdfCompute;
    [EditorOnly] public int resolution = 10;
    public bool enableDebugView = false;

    BoxCollider sdfBounds;
    List<Collider> collidingObjects;

    ComputeBuffer sphereBuffer;
    ComputeBuffer boxBuffer;
    ComputeBuffer verticesBuffer;
    ComputeBuffer indicesBuffer;
    ComputeBuffer normalsBuffer;

    public RenderTexture field { get; private set; }

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

        field = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
        field.enableRandomWrite = true;
        field.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        field.filterMode = FilterMode.Bilinear;
        field.wrapMode = TextureWrapMode.Clamp;
        field.volumeDepth = resolution;
        field.Create();

        computeDistance();

        // Step through the bounds of sdfBounds to generate uniform points
        //Vector3 boundsMin = sdfBounds.bounds.min;
        //Vector3 boundsMax = sdfBounds.bounds.max;
        //float halfPixel = 0.5f / resolution;

        //byte[] fieldValues = new byte[resolution * resolution * resolution];
        //int index = 0;

        //for (int x = 0; x < resolution; x++)
        //{
        //    for (int y = 0; y < resolution; y++)
        //    {
        //        for (int z = 0; z < resolution; z++)
        //        {
        //            // Get the position of the current point in the grid
        //            Vector3 point = new Vector3
        //            (
        //                Mathf.Lerp(boundsMin.z, boundsMax.z, (float) z / resolution + halfPixel),
        //                Mathf.Lerp(boundsMin.y, boundsMax.y, (float) y / resolution + halfPixel),
        //                Mathf.Lerp(boundsMin.x, boundsMax.x, (float) x / resolution + halfPixel)
        //            );

        //            // Calculate the distance from the point to the nearest surface of any collider
        //            fieldValues[index++] = IsPointInsideOrCloseToCollider(point) ? (byte)255 : (byte)0;
        //        }
        //    }
        //}

        //field.SetPixelData(fieldValues, 0);
        //field.Apply();
    }

    private void computeDistance()
    {
        sdfCompute.SetVector("boundsMin", sdfBounds.bounds.min);
        sdfCompute.SetVector("boundsMax", sdfBounds.bounds.max);
        sdfCompute.SetInt("resolution", resolution);
        sdfCompute.SetTexture(0, "Field", field, 0);
        sdfCompute.SetTexture(1, "Field", field, 0);
        sdfCompute.SetTexture(2, "Field", field, 0);
        
        int numGroups = Mathf.CeilToInt((float)resolution / 8);
        // Clear
        sdfCompute.Dispatch(0, numGroups, numGroups, numGroups);

        List<Vector4> spheres = new();
        List<Matrix4x4> boxes = new();
        List<MeshCollider> meshes = new();
        int maxVertices = 0, maxIndices = 0;

        foreach (var collidingObject in collidingObjects)
        {
            if (collidingObject is SphereCollider sphere)
            {
                Vector4 vec = new();
                var p = sphere.transform.position;
                vec.x = p.x;
                vec.y = p.y;
                vec.z = p.z;
                vec.w = sphere.radius;
                spheres.Add(vec);
                Debug.Log(vec);
            }
            else if (collidingObject is BoxCollider box)
            {
                boxes.Add(box.transform.worldToLocalMatrix);
            }
            else if (collidingObject is MeshCollider mesh)
            {
                meshes.Add(mesh);
                maxVertices = Mathf.Max(maxVertices, mesh.sharedMesh.vertices.Length);
                maxIndices = Mathf.Max(maxVertices, mesh.sharedMesh.GetIndices(0).Length);
            }
        }

        sphereBuffer = new ComputeBuffer(spheres.Count, sizeof(float) * 4);
        sphereBuffer.SetData(spheres);
        sdfCompute.SetInt("numSpheres", sphereBuffer.count);
        sdfCompute.SetBuffer(0, "Spheres", sphereBuffer);
        sdfCompute.SetBuffer(1, "Spheres", sphereBuffer);
        sdfCompute.SetBuffer(2, "Spheres", sphereBuffer);

        boxBuffer = new ComputeBuffer(boxes.Count, sizeof(float) * 16);
        boxBuffer.SetData(boxes);
        sdfCompute.SetInt("numBoxes", boxBuffer.count);
        sdfCompute.SetBuffer(0, "Boxes", boxBuffer);
        sdfCompute.SetBuffer(1, "Boxes", boxBuffer);
        sdfCompute.SetBuffer(2, "Boxes", boxBuffer);

        // Primitives
        sdfCompute.Dispatch(1, numGroups, numGroups, numGroups);

        verticesBuffer = new ComputeBuffer(maxVertices, sizeof(float) * 3);
        sdfCompute.SetBuffer(0, "MeshVertices", verticesBuffer);
        sdfCompute.SetBuffer(1, "MeshVertices", verticesBuffer);
        sdfCompute.SetBuffer(2, "MeshVertices", verticesBuffer);

        indicesBuffer = new ComputeBuffer(maxIndices, sizeof(int));
        sdfCompute.SetBuffer(0, "MeshIndices", indicesBuffer);
        sdfCompute.SetBuffer(1, "MeshIndices", indicesBuffer);
        sdfCompute.SetBuffer(2, "MeshIndices", indicesBuffer);

        normalsBuffer = new ComputeBuffer(maxVertices, sizeof(float) * 3);
        sdfCompute.SetBuffer(0, "MeshNormals", normalsBuffer);
        sdfCompute.SetBuffer(1, "MeshNormals", normalsBuffer);
        sdfCompute.SetBuffer(2, "MeshNormals", normalsBuffer);

        // Meshes
        foreach (var m in meshes)
        {
            var indices = m.sharedMesh.GetIndices(0);
            var vertices = m.sharedMesh.vertices;
            var normals = m.sharedMesh.normals;

            // Fix winding order
            //for (int i = 0; i < indices.Length; i += 3)
            //{
            //    // Get triangle vertices
            //    int i0 = indices[i];
            //    int i1 = indices[i + 1];
            //    int i2 = indices[i + 2];

            //    Vector3 v0 = vertices[i0];
            //    Vector3 v1 = vertices[i1];
            //    Vector3 v2 = vertices[i2];

            //    // Compute normal from the cross product
            //    Vector3 computedNormal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

            //    // Get the average normal from the mesh data
            //    Vector3 averageNormal = (normals[i0] + normals[i1] + normals[i2]) / 3.0f;

            //    // If computed normal and mesh normal are facing opposite directions, flip the winding order
            //    if (Vector3.Dot(computedNormal, averageNormal) < 0)
            //    {
            //        // Swap indices to fix winding
            //        (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
            //    }
            //}

            m.transform.TransformPoints(vertices);
            m.transform.TransformDirections(normals);

            sdfCompute.SetInt("numIndices", indices.Length);
            verticesBuffer.SetData(vertices);
            normalsBuffer.SetData(normals);
            indicesBuffer.SetData(indices);

            sdfCompute.Dispatch(2, numGroups, numGroups, numGroups);
        }
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
        if (!Application.isPlaying || !enableDebugView) return;

        Vector3 boundsMin = sdfBounds.bounds.min;
        Vector3 boundsMax = sdfBounds.bounds.max;

        // Resolution of 20 per axis
        int res = 20;
        float halfPixel = 0.5f / res;

        // Iterate through the resolution and draw cubes
        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                for (int z = 0; z < res; z++)
                {
                    // Get the position of the current point in the grid
                    Vector3 point = new Vector3(
                        Mathf.Lerp(boundsMin.x, boundsMax.x, (float) x / res + halfPixel),
                        Mathf.Lerp(boundsMin.y, boundsMax.y, (float) y / res + halfPixel),
                        Mathf.Lerp(boundsMin.z, boundsMax.z, (float) z / res + halfPixel)
                    );

                    // Convert grid position to texture space (UVW)
                    Vector3 uvw = positionToUVW(point.x, point.y, point.z);

                    // Get the value from the field (grayscale value)
                    //float value = field.GetPixelBilinear(uvw.x - halfPixel, uvw.y - halfPixel, uvw.z - halfPixel).r;
                    float value = 0;

                    // Set the Gizmo color based on the value (grayscale)
                    Gizmos.color = new Color(value, value, value, value);

                    // Draw a small cube at the point
                    Gizmos.DrawCube(point, Vector3.one * 0.1f);
                }
            }
        }
    }
}
