using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

public static class LinqExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}

[RequireComponent(typeof(BoxCollider))]
public class RigidBodySystem : MonoBehaviour
{
    [EditorOnly] public ComputeShader computeRigidBodySystem;
    [EditorOnly] public float colliderResolution = 0.01f;
    [EditorOnly] public int skipPoints = 0;

    public float forceCoeff = 1.0f;
    public float torqueCoeff = 1.0f;
    public float dampingCoeff = 1.0f;
    public int feedbackResolution = 1000;

    BoxCollider bounds;

    RigidBodyBox[] boxes;
    RigidBodySphere[] spheres;
    RigidBodyMesh[] meshes;

    Vector3Int rbOffsets = Vector3Int.zero;

    List<RigidBodyInfo> rigidBodyInfos;
    ComputeBuffer rigidBodyParticles;
    ComputeBuffer rigidBodyInfosBuffer;

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
        rigidBodyInfos = Enumerable.Range(0, rbOffsets.z).Select(i => new RigidBodyInfo()).ToList();
    }

    private void OnDestroy()
    {
        rigidBodyParticles?.Release();
        rigidBodyInfosBuffer?.Release();
    }

    internal void Initialize(ComputeShader clientShader, ComputeBuffer fluidPositionsBuffer, ComputeBuffer fluidDensitiesBuffer, ComputeGrid computeGrid, params int[] kernels)
    {
        //computeGrid.InitializeGrid(null, computeRigidBodySystem, 0);
        //computeRigidBodySystem.SetBuffer(0, "_RigidBodyBoxInfos", )
        
        foreach (var kernel in kernels)
        {
            clientShader.SetBuffer(kernel, "_RigidBodyInfos", rigidBodyInfosBuffer);
        }
    }

    internal void Resolve(ComputeShader clientShader)
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
        // TODO
    }

    internal void CollidersEnd()
    {
        //BoxColliderInfo[] boxInfo = new BoxColliderInfo[boxes.Length];
        //boxesBuffer.GetData(boxInfo);

        //for (int i = 0; i < boxInfo.Length; i++)
        //{
        //    float3 force = boxInfo[i].force.xyz;
        //    float3 torque = boxInfo[i].torque.xyz;
        //    force /= feedbackResolution;
        //    torque /= feedbackResolution;
            
        //    //Debug.Log(force);

        //    var rb = boxes[i].GetComponent<Rigidbody>();
        //    rb.AddForce(force, ForceMode.Force);
        //    rb.AddTorque(torque);
        //}
    }
}
