using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ComputeCollider : MonoBehaviour
{
    public int maxColliders = 10;
    public float forceCoeff = 1.0f;
    public float torqueCoeff = 1.0f;
    public float dampingCoeff = 1.0f;
    public int feedbackResolution = 1000;

    BoxCollider bounds;

    ComputeColliderBox[] boxes;

    ComputeBuffer boxesBuffer;

    // Unsafe
    const int BoxColliderInfoSize = 52 * 4;

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = BoxColliderInfoSize)]
    struct BoxColliderInfo
    {
        public Matrix4x4 TR;
        public Matrix4x4 inverseTR;
        public float4 scale;
        public float4 velocity;
        public float4 angularVelocity;
        public int4 force;
        public int4 torque;
    };

    void Awake()
    {
        bounds = GetComponent<BoxCollider>();

        boxesBuffer = new ComputeBuffer(maxColliders, BoxColliderInfoSize);

        boxes = FindObjectsByType<ComputeColliderBox>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void OnDestroy()
    {
        boxesBuffer.Release();
    }

    internal void InitializeCollider(ComputeShader clientShader, params int[] kernels)
    {
        foreach (var kernel in kernels)
        {
            clientShader.SetBuffer(kernel, "_BoxColliders", boxesBuffer);
        }
    }

    internal void CollidersBegin(ComputeShader clientShader, float particleRadius = 0.0f, float restitutionCoeff = 1.0f)
    {
        List<BoxColliderInfo> data = new();

        foreach (var box in boxes)
        {
            Transform boxTransform = box.transform;
            Rigidbody rb = box.GetComponent<Rigidbody>();
            BoxColliderInfo info = new();
            info.TR = Matrix4x4.TRS(boxTransform.position, boxTransform.rotation, Vector3.one);
            info.inverseTR = info.TR.inverse;
            info.scale = new float4(boxTransform.localScale, 0.0f);
            info.force = int4.zero;
            info.torque = int4.zero;
            info.angularVelocity = new float4(rb.angularVelocity, 0.0f);
            info.velocity = new float4(rb.linearVelocity, 0.0f);
            data.Add(info); 
            
            //Debug.Log($"Angular velocity: {rb.angularVelocity} | Linear velocity: {rb.linearVelocity}");
        }


        clientShader.SetInt("_NumBoxColliders", data.Count);
        clientShader.SetFloat("_ParticleRadius", particleRadius);
        clientShader.SetFloat("_RestitutionCoeff", restitutionCoeff);
        clientShader.SetFloat("_ForceCoeff", forceCoeff);
        clientShader.SetFloat("_TorqueCoeff", torqueCoeff);
        clientShader.SetFloat("_DampingCoeff", dampingCoeff);
        clientShader.SetInt("_FeedbackResolution", feedbackResolution);
        boxesBuffer.SetData(data);
    }

    internal void CollidersEnd()
    {
        BoxColliderInfo[] boxInfo = new BoxColliderInfo[boxes.Length];
        boxesBuffer.GetData(boxInfo);

        for (int i = 0; i < boxInfo.Length; i++)
        {
            float3 force = boxInfo[i].force.xyz;
            float3 torque = boxInfo[i].torque.xyz;
            force /= feedbackResolution;
            torque /= feedbackResolution;
            
            //Debug.Log(force);

            var rb = boxes[i].GetComponent<Rigidbody>();
            rb.AddForce(force, ForceMode.Force);
            rb.AddTorque(torque);
        }
    }
}
