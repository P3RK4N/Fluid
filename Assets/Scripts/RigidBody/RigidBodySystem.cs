using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

public static class LinqExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static IEnumerable<Vector3Int> Range3D(this (int x, int y, int z) size)
    {
        return Enumerable.Range(0, size.x)
            .SelectMany(x => Enumerable.Range(0, size.y)
                .SelectMany(y => Enumerable.Range(0, size.z)
                    .Select(z => new UnityEngine.Vector3Int(x, y, z))));
    }
}

[RequireComponent(typeof(BoxCollider))]
public class RigidBodySystem : MonoBehaviour
{
    [EditorOnly] public ComputeShader computeRigidBodySystem;
    [EditorOnly] public float colliderResolution = 0.01f;
    public bool debug = true;

    public float forceCoeff = 1.0f;
    public float torqueCoeff = 1.0f;
    public float dampingCoeff = 1.0f;
    public float targetDensity = 1.0f;
    public int feedbackResolution = 1000;

    BoxCollider bounds;

    RigidBodyBox[] boxes;
    RigidBodySphere[] spheres;
    RigidBodyMesh[] meshes;

    Vector3Int rbOffsets = Vector3Int.zero;

    RigidBodyInfo[] rigidBodyInfos;
    ComputeBuffer rigidBodyParticlesBuffer;
    ComputeBuffer rigidBodyInfosBuffer;

    ComputeBuffer debugBuffer;

    List<Vector4> points;

    [StructLayout(LayoutKind.Sequential, Size = 52 * 4)]
    struct RigidBodyInfo
    {
        public Matrix4x4 TR;
        public Matrix4x4 inverseTR;
        public float4 scale;
        public float4 velocity;
        public float4 angularVelocity;
        public int4 accumulatedForce;
        public int4 accumulatedTorque;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4 * 4)]
    struct RigidBodyParticle
    {
        public float4 position; // pos.x pos.y pos.z rbIndex
    }

    void Awake()
    {
        bounds = GetComponent<BoxCollider>();

        GenerateRigidBodies();
        GenerateRigidBodyParticles();
    }


    private void GenerateRigidBodies()
    {
        boxes = FindObjectsByType<RigidBodyBox>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        spheres = FindObjectsByType<RigidBodySphere>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        meshes = FindObjectsByType<RigidBodyMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        rbOffsets.x = boxes.Length;
        rbOffsets.y = rbOffsets.x + spheres.Length;
        rbOffsets.z = rbOffsets.y + meshes.Length;

        rigidBodyInfosBuffer = new ComputeBuffer(rbOffsets.z, sizeof(float) * 52);
        rigidBodyInfos = Enumerable.Range(0, rbOffsets.z).Select(i => new RigidBodyInfo()).ToArray();
    }

    private void GenerateRigidBodyParticles()
    {
        points = new();

        for (int i = 0; i < boxes.Count(); i++)
        {
            var box = boxes[i];
            Vector3 scale = box.transform.localScale;
            Bounds bounds = new Bounds(Vector3.zero, scale);
            points.AddRange(GenerateSurfacePoints(bounds.min, bounds.max, p => bounds.Contains(p), i));
        }

        // TODO: Spheres
        // TODO: Meshes

        rigidBodyParticlesBuffer = new ComputeBuffer(points.Count, sizeof(float) * 4);
        rigidBodyParticlesBuffer.SetData(points);

        debugBuffer = new ComputeBuffer(points.Count, sizeof(float) * 4);
        computeRigidBodySystem.SetBuffer(0, "debugBuffer", debugBuffer);
    }

    private List<Vector4> GenerateSurfacePoints(Vector3 min, Vector3 max, Func<Vector3, bool> inside, int rbIndex)
    {
        int countX = Mathf.CeilToInt((max.x - min.x) / colliderResolution) + 1;
        int countY = Mathf.CeilToInt((max.y - min.y) / colliderResolution) + 1;
        int countZ = Mathf.CeilToInt((max.z - min.z) / colliderResolution) + 1;

        var neighbor_offsets = new[]
        {
            new Vector3(1, 0, 0) * colliderResolution, new Vector3(-1, 0, 0) * colliderResolution,
            new Vector3(0, 1, 0) * colliderResolution, new Vector3(0, -1, 0) * colliderResolution,
            new Vector3(0, 0, 1) * colliderResolution, new Vector3(0, 0, -1) * colliderResolution
        };

        return (countX, countY, countZ)
            .Range3D()
            .AsParallel()
            .Select(v => new Vector3(min.x + v.x * colliderResolution, min.y + v.y * colliderResolution, min.z + v.z * colliderResolution))     // Transform
            .Where(v => inside(v) /*&& neighbor_offsets.Any(neighbor => !inside(neighbor + v))*/)                                                                // One neighbor must be outside
            .Select(vec3 => new Vector4(vec3.x, vec3.y, vec3.z, rbIndex))
            .ToList();
    }

    private void OnDestroy()
    {
        rigidBodyParticlesBuffer?.Release();
        rigidBodyInfosBuffer?.Release();
        debugBuffer?.Release();
    }

    internal void Initialize(ComputeShader clientShader, ComputeBuffer fluidPositionsBuffer, ComputeBuffer fluidDensitiesBuffer, ComputeGrid computeGrid, params int[] kernels)
    {
        computeGrid.Bind(computeRigidBodySystem, 0);                                        // Grid bind
        computeRigidBodySystem.SetBuffer(0, "_RigidBodyInfos", rigidBodyInfosBuffer);       // ComputeRigidBodyInfoSystem.hlsl init
        computeRigidBodySystem.SetBuffer(0, "rigidParticles", rigidBodyParticlesBuffer);
        computeRigidBodySystem.SetBuffer(0, "positions", fluidPositionsBuffer);
        computeRigidBodySystem.SetBuffer(0, "densities", fluidDensitiesBuffer); 
        
        foreach (var kernel in kernels)
        {
            clientShader.SetBuffer(kernel, "_RigidBodyInfos", rigidBodyInfosBuffer);
        }
    } 

    internal void Resolve(ComputeShader clientShader, float kernelRadius, float _targetDensity, float pressureCoeff, float nearPressureCoeff, bool applyFeedback = true)
    {
        // Collider resolve

        Action<int, MonoBehaviour[]> UpdateRigidBodyInfo = (offset, rbs) =>
        {
            Enumerable
                .Range(0, rbs.Length)
                .ForEach(i =>
                {
                    var tf = rbs[i].transform;
                    var rb = tf.GetComponent<Rigidbody>();
                    var rbInfo = new RigidBodyInfo();

                    rbInfo.TR = Matrix4x4.TRS(tf.position, tf.rotation, Vector3.one);
                    rbInfo.inverseTR = rbInfo.TR.inverse;
                    rbInfo.scale = new float4(tf.localScale, 0.0f);
                    rbInfo.accumulatedForce = int4.zero;
                    rbInfo.accumulatedTorque = int4.zero;
                    rbInfo.angularVelocity = new float4(rb.angularVelocity, 0.0f);
                    rbInfo.velocity = new float4(rb.linearVelocity, 0.0f);

                    rigidBodyInfos[offset + i] = rbInfo;
                });
        };

        UpdateRigidBodyInfo(0, boxes);
        UpdateRigidBodyInfo(rbOffsets.x, spheres);
        UpdateRigidBodyInfo(rbOffsets.y, meshes);

        rigidBodyInfosBuffer.SetData(rigidBodyInfos);
        clientShader.SetInts("_RbOffsets", rbOffsets.x, rbOffsets.y, rbOffsets.z);

        // Rigidbody Feedback Resolve
        int numGroups = Mathf.CeilToInt(rigidBodyParticlesBuffer.count / 1024.0f);
        computeRigidBodySystem.SetInts("_RbOffsets", rbOffsets.x, rbOffsets.y, rbOffsets.z);
        computeRigidBodySystem.SetFloat("kernelRadius", kernelRadius);
        computeRigidBodySystem.SetFloat("pressureCoeff", pressureCoeff);
        computeRigidBodySystem.SetFloat("nearPressureCoeff", nearPressureCoeff);
        computeRigidBodySystem.SetFloat("targetDensity", targetDensity);
        computeRigidBodySystem.SetFloat("dampingCoeff", dampingCoeff);
        computeRigidBodySystem.SetInt("feedbackResolution", feedbackResolution);
        computeRigidBodySystem.Dispatch(0, numGroups, 1, 1);

        ApplyFeedback();
    }

    private void ApplyFeedback()
    {
        rigidBodyInfosBuffer.GetData(rigidBodyInfos);

        for (int i = 0; i < rigidBodyInfos.Length; i++)
        {
            ref var info = ref rigidBodyInfos[i];
            var rb = boxes[i].GetComponent<Rigidbody>();

            float4 force = info.accumulatedForce;
            float4 torque = info.accumulatedTorque;
            force /= feedbackResolution;
            torque /= feedbackResolution;
            force *= forceCoeff;
            torque *= torqueCoeff;

            Vector3 f = force.xyz;
            rb.AddForce(f, ForceMode.Impulse);
            rb.AddTorque(torque.xyz, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug || !Application.isPlaying)
        {
            return;
        }

        var info = rigidBodyInfos[0];
        Vector4[] vectors = new Vector4[debugBuffer.count];
        debugBuffer.GetData(vectors);

        for (int i = 0; i < vectors.Length; i++)
        {
            var p = points[i];
            p.w = 1.0f;
            p = info.TR * p;
            Gizmos.DrawLine(p, p + vectors[i]);
        }
        //Vector3 pos = (float3)rigidBodyInfos[0].accumulatedForce.xyz / feedbackResolution;
        //int4 d = rigidBodyInfos[0].accumulatedTorque;
        //Debug.Log(d);
        //Gizmos.DrawCube(pos, Vector3.one * 0.5f);
    }
}
