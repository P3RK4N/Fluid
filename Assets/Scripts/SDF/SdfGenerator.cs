using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 80)]
    struct BoxInfo
    {
        public Matrix4x4 inverseTR;
        public Vector4 scale;
    }

    void Awake()
    {
        sdfBounds = GetComponent<BoxCollider>();
        //GenerateSdf();
    }

    public void InitializeSdf(ComputeShader computeShader, params int[] kernels)
    {
        GenerateSdf();

        computeShader.SetVector("_sdfBoundsMin", sdfBounds.bounds.min);
        computeShader.SetVector("_sdfBoundsMax", sdfBounds.bounds.max);
        computeShader.SetInt("_sdfResolution", resolution);
        foreach (var kernel in kernels)
        {
            computeShader.SetTexture(kernel, "FieldTexture", field, 0); 
        }
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

        field = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
        field.enableRandomWrite = true;
        field.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        field.filterMode = FilterMode.Bilinear;
        field.wrapMode = TextureWrapMode.Clamp;
        field.volumeDepth = resolution;
        field.Create();

        computeDistance();
    }

    private void computeDistance()
    {
        sdfCompute.SetVector("_sdfBoundsMin", sdfBounds.bounds.min);
        sdfCompute.SetVector("_sdfBoundsMax", sdfBounds.bounds.max);
        sdfCompute.SetInt("_sdfResolution", resolution);
        sdfCompute.SetTexture(0, "Field", field, 0);
        sdfCompute.SetTexture(1, "Field", field, 0);
        sdfCompute.SetTexture(2, "Field", field, 0);
        
        int numGroups = Mathf.CeilToInt((float)resolution / 8);

        // Clear
        sdfCompute.Dispatch(0, numGroups, numGroups, numGroups);

        List<Vector4> spheres = new();
        List<BoxInfo> boxes = new();
        List<MeshCollider> meshes = new();

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
                BoxInfo boxInfo = new BoxInfo();
                boxInfo.inverseTR = Matrix4x4.TRS(box.transform.position, box.transform.rotation, Vector3.one).inverse;
                boxInfo.scale = box.transform.localScale;
                boxes.Add(boxInfo);
            }
            else if (collidingObject is MeshCollider mesh)
            {
                meshes.Add(mesh);
            }
        }
        
        sdfCompute.SetInt("numSpheres", spheres.Count);
        sphereBuffer = new ComputeBuffer(Mathf.Max(1, spheres.Count), sizeof(float) * 4);
        sdfCompute.SetBuffer(0, "Spheres", sphereBuffer);
        sdfCompute.SetBuffer(1, "Spheres", sphereBuffer);
        sdfCompute.SetBuffer(2, "Spheres", sphereBuffer);
        if (boxes.Count > 0)
        {
            sphereBuffer.SetData(spheres);
        }

        sdfCompute.SetInt("numBoxes", boxes.Count);
        boxBuffer = new ComputeBuffer(Mathf.Max(1, boxes.Count), sizeof(float) * 20);
        sdfCompute.SetBuffer(0, "Boxes", boxBuffer);
        sdfCompute.SetBuffer(1, "Boxes", boxBuffer);
        sdfCompute.SetBuffer(2, "Boxes", boxBuffer);
        if (boxes.Count > 0)
        {
            boxBuffer.SetData(boxes);
        }

        Debug.Log($"{boxes.Count} {spheres.Count}");

        // Primitives
        sdfCompute.Dispatch(1, numGroups, numGroups, numGroups);

        handleMeshes(meshes);
    }

    void handleMeshes(List<MeshCollider> meshes)
    {
        int maxVertices = 0;
        int maxIndices = 0;
        int numGroups = Mathf.CeilToInt((float)resolution / 8);

        foreach (var m in meshes)
        {
            maxVertices = Mathf.Max(maxVertices, m.sharedMesh.vertices.Length);
            maxIndices = Mathf.Max(maxVertices, m.sharedMesh.GetIndices(0).Length);
        }

        if (maxVertices == 0 || maxIndices == 0)
        {
            return;
        }

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

        foreach (var m in meshes)
        {
            var indices = m.sharedMesh.GetIndices(0);
            var vertices = m.sharedMesh.vertices;
            var normals = m.sharedMesh.normals;

            m.transform.TransformPoints(vertices);
            m.transform.TransformDirections(normals);

            sdfCompute.SetInt("numIndices", indices.Length);
            verticesBuffer.SetData(vertices);
            normalsBuffer.SetData(normals);
            indicesBuffer.SetData(indices);

            sdfCompute.Dispatch(2, numGroups, numGroups, numGroups);
        }
    }

    public Vector3 positionToUVW(float x, float y, float z)
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

}
