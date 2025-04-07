using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ComputeCollider : MonoBehaviour
{
    public int maxColliders = 10;

    BoxCollider bounds;

    ComputeColliderBox[] boxes;

    ComputeBuffer boxesBuffer;

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 44 * 4)]
    struct BoxColliderInfo
    {
        public Matrix4x4 TR;
        public Matrix4x4 inverseTR;
        public float4 scale;
        public int4 force;
        public int4 torque;

        public override bool Equals(object obj)
        {
            return obj is BoxColliderInfo info &&
                   TR.Equals(info.TR) &&
                   inverseTR.Equals(info.inverseTR) &&
                   scale.Equals(info.scale) &&
                   force.Equals(info.force) &&
                   torque.Equals(info.torque);
        }
    };

    void Awake()
    {
        bounds = GetComponent<BoxCollider>();
        boxesBuffer = new ComputeBuffer(maxColliders, sizeof(float) * 44);

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
            BoxColliderInfo info = new();
            info.TR = Matrix4x4.TRS(boxTransform.position, boxTransform.rotation, Vector3.one);
            info.inverseTR = info.TR.inverse;
            info.scale = new float4(boxTransform.localScale, 0.0f);
            info.force = int4.zero;
            info.torque = int4.zero;
            data.Add(info); 
        }

        clientShader.SetInt("_NumBoxColliders", data.Count);
        clientShader.SetFloat("_ParticleRadius", particleRadius);
        clientShader.SetFloat("_RestitutionCoeff", restitutionCoeff);
        boxesBuffer.SetData(data);
    }

    internal void CollidersEnd()
    {
        //BoxColliderInfo[] boxInfo = 
    }
}
