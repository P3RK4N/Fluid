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

    public enum PlaybackMode
    {
        Step,
        Fixed
    };

    [Header("Simulation settings")]

    public PlaybackMode playbackMode;

    public ComputeShader computeShader;
    
    [Header("Properties")]
     
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

    public float pointerRadius = 0.3f;
    public float pointerStrength = 1.0f;

    private RenderTexture renderTexture;
    private ComputeBuffer positionBuffer, velocityBuffer, forceBuffer, densityBuffer, statsBuffer;

    public enum ComputeKernel
    {
        Draw = 0,
        Step,
        PreStep,
    }

    int[] stats = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    void Awake()
    {
        InitializeRenderTexture();
        InitializeBuffers();
        InitializePositions();
        SetUniformData();
        //DispatchRandomInit();
        DispatchDraw();
    }

    private void InitializePositions()
    {
        Vector2[] initialPositions = new Vector2[numParticles];
        int ceil = Mathf.CeilToInt(Mathf.Sqrt((float)numParticles));
        float spacing = 0.5f / ceil;
        for (int i = 0; i < numParticles; i++)
        {
            int x = i / ceil;
            int y = i % ceil;
            initialPositions[i] = new Vector2(spacing * x - 0.25f, spacing * y - 0.25f);
        }

        positionBuffer.SetData(initialPositions);
    }

    void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        GetComponentInChildren<Renderer>().material.mainTexture = renderTexture;
    }

    private void InitializeBuffers()
    {
        positionBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        velocityBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        forceBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float));
        statsBuffer = new ComputeBuffer(stats.Length, sizeof(uint), ComputeBufferType.Raw);
        statsBuffer.SetData(stats);

        for (int k = 0; k < Enum.GetValues(typeof(ComputeKernel)).Length; k++)
        {
            computeShader.SetBuffer(k, "positions", positionBuffer);
            computeShader.SetBuffer(k, "velocities", velocityBuffer);
            computeShader.SetBuffer(k, "forces", forceBuffer);
            computeShader.SetBuffer(k, "densities", densityBuffer);
            computeShader.SetBuffer(k, "stats", statsBuffer);

            computeShader.SetTexture(k, "result", renderTexture);
        }
    }

    void SetUniformData()
    {
        computeShader.SetFloat("restitutionCoeff", restitutionCoeff);
        computeShader.SetInt("numParticles", numParticles);
        computeShader.SetFloat("particleRadius", particleRadius);
        computeShader.SetFloat("particleMass", particleMass);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureCoeff", pressureCoeff);
        computeShader.SetFloat("viscosityCoeff", viscosityCoeff);
        computeShader.SetFloat("gravityCoeff", gravityCoeff);
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        float kr2 = kernelRadius * kernelRadius;
        float kr3 = kr2 * kernelRadius;
        float kr4 = kr3 * kernelRadius;
        float kr5 = kr4 * kernelRadius;
        computeShader.SetFloat("kernelRadius", kernelRadius);
        computeShader.SetFloat("kernelRadius2", kr2);
        computeShader.SetFloat("kernelRadius3", kr3);
        computeShader.SetFloat("kernelRadius4", kr4);
        computeShader.SetFloat("kernelRadius5", kr5);

        if (Input.GetMouseButton(0))
        {
            computeShader.SetBool("pointerActive", true);
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 3.0f))
            {
                computeShader.SetVector("pointerPos", inverseTransform(hit.point));
            }
            computeShader.SetFloat("pointerRadius", pointerRadius);
            computeShader.SetFloat("pointerStrength", pointerStrength);
        }
        else
        {
            computeShader.SetBool("pointerActive", false);
        }
    }

    private void Update()
    {
        if (Time.frameCount < 100)
        {
            return;
        }

        if (playbackMode == PlaybackMode.Fixed || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SetUniformData();
            DispatchStep();
            DispatchDraw();
            DisplayStats();
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            return;
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

    public Vector2 inverseTransform(Vector3 worldPos)
    {
        return transform.InverseTransformPoint(worldPos);
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
        velocityBuffer.Release();
        forceBuffer.Release();
        densityBuffer.Release();
        statsBuffer.Release();
    }

    void DispatchDraw()
    {
        if (computeShader == null || renderTexture == null) return;

        computeShader.Dispatch((int)ComputeKernel.Draw, Mathf.CeilToInt((float)(resolution.x * resolution.y) / X), 1, 1);
    }

    void DispatchStep()
    {
        if (computeShader == null || renderTexture == null) return;

        computeShader.Dispatch((int)ComputeKernel.PreStep, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Step, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
    }
}