using System;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeFluidSim : MonoBehaviour
{
    public static readonly int X = 1024;

    public enum PlaybackMode
    {
        Step,
        Fixed
    };

    public enum Dimension
    {
        Dimension2D = 2,
        Dimension3D = 3
    };

    [Header("Simulation settings")]

    public PlaybackMode playbackMode;
    [EditorOnly] public Dimension dimension;
    [EditorOnly] public ComputeShader computeShader;

    [Header("Properties")]

    [EditorOnly] public int numParticles = 100;
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

    public ComputeBuffer positionBuffer, velocityBuffer, forceBuffer, densityBuffer, statsBuffer;

    public enum ComputeKernel
    {
        Step,
        PreStep,
    }

    int[] stats = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    void Awake()
    {
        InitializeBuffers();
        InitializePositions();
        SetBufferData(computeShader, Enum.GetValues(typeof(ComputeKernel)).Length);

        computeShader.EnableKeyword(dimension == Dimension.Dimension2D ? "DISABLE_3D" : "ENABLE_3D");
        computeShader.DisableKeyword(dimension == Dimension.Dimension2D ? "ENABLE_3D" : "DISABLE_3D");
    }

    private void InitializePositions()
    {
        float width = 0.75f;
        float halfWidth = width / 2;

        // For 2D
        if (dimension == Dimension.Dimension2D)
        {
            Vector2[] initialPositions = new Vector2[numParticles];
            int rowSize = Mathf.CeilToInt(Mathf.Sqrt((float)numParticles));
            float spacing = width / rowSize;
            for (int i = 0; i < numParticles; i++)
            {
                int x = i / rowSize;
                int y = i % rowSize;
                initialPositions[i] = new Vector2(spacing * x - halfWidth, spacing * y - halfWidth);
            }
            positionBuffer.SetData(initialPositions);
        }
        // For 3D
        else
        {
            Vector3[] initialPositions = new Vector3[numParticles];
            int rowSize = Mathf.CeilToInt(Mathf.Pow((float)numParticles, 1.0f / 3.0f));
            int sliceSize = rowSize * rowSize;
            float spacing = 0.5f / rowSize;
            for (int i = 0; i < numParticles; i++)
            {
                int x = i / sliceSize;
                int y = (i % sliceSize) / rowSize;
                int z = i % rowSize;
                initialPositions[i] = new Vector3(spacing * x - halfWidth, spacing * y - halfWidth, spacing * z - halfWidth);
            }
            positionBuffer.SetData(initialPositions);
        }
    }

    private void InitializeBuffers()
    {
        int stride = sizeof(float) * (int)dimension;

        positionBuffer = new ComputeBuffer(numParticles, stride);
        velocityBuffer = new ComputeBuffer(numParticles, stride);
        forceBuffer = new ComputeBuffer(numParticles, stride);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float));
        statsBuffer = new ComputeBuffer(stats.Length, sizeof(uint), ComputeBufferType.Raw);
        statsBuffer.SetData(stats);
    }

    public void SetBufferData(ComputeShader cs, int numKernels)
    {
        for (int k = 0; k < numKernels; k++)
        {
            cs.SetBuffer(k, "positions", positionBuffer);
            cs.SetBuffer(k, "velocities", velocityBuffer);
            cs.SetBuffer(k, "forces", forceBuffer);
            cs.SetBuffer(k, "densities", densityBuffer);
            cs.SetBuffer(k, "stats", statsBuffer);
        }
    }

    public void SetUniformData(ComputeShader cs)
    {
        cs.SetFloat("restitutionCoeff", restitutionCoeff);
        cs.SetInt("numParticles", numParticles);
        cs.SetFloat("particleRadius", particleRadius);
        cs.SetFloat("particleMass", particleMass);
        cs.SetFloat("targetDensity", targetDensity);
        cs.SetFloat("pressureCoeff", pressureCoeff);
        cs.SetFloat("viscosityCoeff", viscosityCoeff);
        cs.SetFloat("gravityCoeff", gravityCoeff);
        cs.SetFloat("deltaTime", Time.deltaTime);

        float kr2 = kernelRadius * kernelRadius;
        float kr3 = kr2 * kernelRadius;
        float kr4 = kr3 * kernelRadius;
        float kr5 = kr4 * kernelRadius;
        cs.SetFloat("kernelRadius", kernelRadius);
        cs.SetFloat("kernelRadius2", kr2);
        cs.SetFloat("kernelRadius3", kr3);
        cs.SetFloat("kernelRadius4", kr4);
        cs.SetFloat("kernelRadius5", kr5);

        if (Input.GetMouseButton(0))
        {
            cs.SetBool("pointerActive", true);
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 3.0f))
            {
                cs.SetVector("pointerPos", inverseTransform(hit.textureCoord));
            }
            cs.SetFloat("pointerRadius", pointerRadius);
            cs.SetFloat("pointerStrength", pointerStrength);
        }
        else
        {
            cs.SetBool("pointerActive", false);
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
            SetUniformData(computeShader);
            DispatchStep();
            DisplayStats();
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

    void DispatchStep()
    {
        computeShader.Dispatch((int)ComputeKernel.PreStep, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Step, Mathf.CeilToInt((float)(numParticles) / X), 1, 1);
    }
}