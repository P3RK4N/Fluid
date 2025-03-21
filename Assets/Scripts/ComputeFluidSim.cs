using System;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEditor;

public class ComputePreviewer : MonoBehaviour
{
    public static readonly int X = 1024;
    
    public ComputeShader computeShader;
    public DrawKernel drawKernel;

    public Vector2Int resolution;
    public int numParticles = 100;
    public float particleRadius = 0.01f;
    public float particleMass = 0.001f;
    public float targetDensity = 1000.0f;
    public float pressureCoeff = 1.0f;
    public float viscosityCoeff = 1.0f;
    public float gravityCoeff = -9.81f;
    public float kernelRadius = 0.1f;
    public float restitutionCoeff = 0.99f;

    public float scale = 1.0f; 
    public float pointerRadius = 0.3f;
    public float pointerStrength = 1.0f;
    public Vector2 offset = Vector2.zero;

    private RenderTexture renderTexture;
    private ComputeBuffer positionBuffer, predictedPositionBuffer, velocityBuffer, forceBuffer, densityBuffer, statsBuffer;

    public enum DrawKernel
    {
        Particle = 0,
        Density = 1,
        Pressure = 2,
    }

    public enum ComputeKernel
    {
        RandomInit = 3,
        Step,
        PreStep,
    }

    int[] stats = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    void Awake()
    {
        InitializeRenderTexture();
        InitializeBuffers();
        SetUniformData();
        DispatchRandomInit();
        DispatchDraw();
    }

    private void InitializeBuffers()
    {
        positionBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        predictedPositionBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        velocityBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        forceBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float));
        statsBuffer = new ComputeBuffer(stats.Length, sizeof(uint), ComputeBufferType.Raw);
        statsBuffer.SetData(stats);

        for (int k = 0; k < Enum.GetValues(typeof(ComputeKernel)).Length + Enum.GetValues(typeof(DrawKernel)).Length; k++)
        {
            computeShader.SetBuffer(k, "positions", positionBuffer);
            computeShader.SetBuffer(k, "predictedPositions", predictedPositionBuffer);
            computeShader.SetBuffer(k, "velocities", velocityBuffer);
            computeShader.SetBuffer(k, "forces", forceBuffer);
            computeShader.SetBuffer(k, "densities", densityBuffer);
            computeShader.SetBuffer(k, "stats", statsBuffer);

            computeShader.SetTexture(k, "result", renderTexture);
        }
    }

    void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        GetComponentInChildren<Renderer>().material.mainTexture = renderTexture;
    }

    void SetUniformData()
    {
        computeShader.SetFloat("scale", scale);
        computeShader.SetFloat("restitutionCoeff", restitutionCoeff);
        computeShader.SetInt("numParticles", numParticles);
        computeShader.SetFloat("particleRadius", particleRadius);
        computeShader.SetFloat("particleMass", particleMass);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureCoeff", pressureCoeff);
        computeShader.SetFloat("viscosityCoeff", viscosityCoeff);
        computeShader.SetFloat("gravityCoeff", gravityCoeff);
        computeShader.SetVector("offset", offset);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetBool("pointerActive", Input.GetMouseButton(0));
        computeShader.SetFloat("pointerRadius", pointerRadius);
        computeShader.SetFloat("pointerStrength", pointerStrength);

        float kr2 = kernelRadius * kernelRadius;
        float kr3 = kr2 * kernelRadius;
        float kr4 = kr3 * kernelRadius;
        float kr5 = kr4 * kernelRadius;
        computeShader.SetFloat("kernelRadius", kernelRadius);
        computeShader.SetFloat("kernelRadius2", kr2);
        computeShader.SetFloat("kernelRadius3", kr3);
        computeShader.SetFloat("kernelRadius4", kr4);
        computeShader.SetFloat("kernelRadius5", kr5);
    }

    private void OnValidate()
    {
        updateCorners();
    }

    private void Update()
    {
        SetUniformData();
        DispatchStep();
        DispatchDraw();
        DisplayStats();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector2[] positions = new Vector2[10];
        Vector2[] forces = new Vector2[10];

        positionBuffer.GetData(positions);
        forceBuffer.GetData(forces);

        for (int i = 0; i < forces.Length; i++)
        {
            var worldPos = transform.TransformPoint(positions[i]) + Vector3.back * 0.05f;
            var worldDir = transform.TransformDirection(forces[i]);
            Gizmos.DrawSphere(worldPos, 0.01f);
            Gizmos.DrawLine(worldPos, worldPos + worldDir * 0.1f);
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 3.0f))
        {
            computeShader.SetVector("pointerPos", inverseTransform(hit.point));
        }

    }

    private void DisplayStats()
    {
        statsBuffer.GetData(stats);
        //Debug.Log
        //(
        //    $"Max density: {stats[0] / 1000.0f}\n" +
        //    $"Min Pressure: {stats[1] / 1000.0f}\n" +
        //    $"Max pressure: {stats[2] / 1000.0f}\n"
        //);

        for (int i = 0; i < stats.Length; i++) stats[i] = 0;
        statsBuffer.SetData(stats);
    }

    private void updateCorners()
    {
        // Get all TextMeshPro components in children
        TMP_Text[] textMeshes = GetComponentsInChildren<TMP_Text>();

        if (textMeshes.Length < 4)
        {
            Debug.LogError($"Not enough TextMeshPro children. Expected 4, got {textMeshes.Length}");
            return;
        }

        // Assign each text object to a corner
        for (int i = 0; i < 4; i++)
        {
            // Put to gameobject corners
            Vector2 corner = textMeshes[i].transform.localPosition;
            corner.x /= Mathf.Abs(corner.x);
            corner.y /= Mathf.Abs(corner.y);
            corner /= 2.0f;
            corner += Vector2.one * 0.5f;
            
            // Transform
            var pos = transformPosition(corner);
            textMeshes[i].text = $"({pos.x:F2} | {pos.y:F2})";
        }
    }

    private Vector2 transformPosition(Vector2 normalizedPos)
    {
        return (normalizedPos + offset) * scale;
    }

    public Vector2 inverseTransform(Vector3 worldPos)
    {
        return transform.InverseTransformPoint(worldPos);
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
        predictedPositionBuffer.Release();
        velocityBuffer.Release();
        forceBuffer.Release();
        densityBuffer.Release();
        statsBuffer.Release();
    }

    void DispatchDraw()
    {
        if (computeShader == null || renderTexture == null) return;

        computeShader.Dispatch((int)drawKernel, Mathf.CeilToInt((float)(resolution.x * resolution.y) / X), 1, 1);
        computeShader.Dispatch((int)DrawKernel.Particle, Mathf.CeilToInt((float)(resolution.x * resolution.y) / X), 1, 1);
    }

    void DispatchRandomInit()
    {
        if (computeShader == null || renderTexture == null) return;

        computeShader.Dispatch((int)ComputeKernel.RandomInit, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
    }

    void DispatchStep()
    {
        if (computeShader == null || renderTexture == null) return;

        computeShader.Dispatch((int)ComputeKernel.PreStep, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Step, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
    }
}