using System;
using System.Runtime.InteropServices;
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

    public enum Method
    {
        Default,
        Sebastian,
    };

    public enum Dimension
    {
        Dimension2D = 2,
        Dimension3D = 3
    };

    public enum ComputeKernel
    {
        Predict,
        Density,
        Pressure,
        Viscosity,
        Step,
    }

    [Header("Simulation settings")]

    public PlaybackMode playbackMode;
    [EditorOnly] public Method method;
    [EditorOnly] public Dimension dimension;
    [EditorOnly] public ComputeShader defaultFluidCompute;
    [EditorOnly] public ComputeShader sebFluidCompute;
    [EditorOnly] public int numParticles = 100;
    public float timeStep = 1 / 120.0f;
    public float gravityCoeff = -9.81f;

    [Header("Particle Properties")]

    public float particleRadius = 0.01f;
    public float particleMass = 0.001f;
    public float targetDensity = 1000.0f;
    public float pressureCoeff = 1.0f;
    public float nearPressureCoeff = 1.0f;
    public float viscosityCoeff = 1.0f;
    public float kernelRadius = 0.1f;
    public float restitutionCoeff = 0.99f;

    [Header("Interaction properties")]

    public float pointerRadius = 0.3f;
    public float pointerStrength = 1.0f;


    ComputeShader computeShader;
    public ComputeBuffer positionBuffer, predictedPositionBuffer, velocityBuffer, forceBuffer, densityBuffer, statsBuffer;

    ComputeGrid computeGrid;

    int[] stats = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    float simulatedTime = 0.0f;

    void Awake()
    {
        computeGrid = GetComponent<ComputeGrid>();
        InitializeComputeShader();
        InitializeBuffers();
        InitializePositions();
        SetBufferData(computeShader, Enum.GetValues(typeof(ComputeKernel)).Length);

        computeShader.EnableKeyword(dimension == Dimension.Dimension2D ? "DISABLE_3D" : "ENABLE_3D");
        computeShader.DisableKeyword(dimension == Dimension.Dimension2D ? "ENABLE_3D" : "DISABLE_3D");
    }

    void Start()
    {
        computeGrid.InitializeGrid(predictedPositionBuffer, computeShader, 0, 1, 2, 3, 4);
    }

    private void InitializeComputeShader()
    {
        switch (method)
        {
            case Method.Default: computeShader = defaultFluidCompute; break;
            case Method.Sebastian: computeShader = sebFluidCompute; break;
        }
    }

    private void InitializePositions()
    {
        float preferred_width = 10.0f;
        Vector3 width = new Vector3(Mathf.Min(preferred_width, transform.localScale.x), Mathf.Min(preferred_width, transform.localScale.y), Mathf.Min(preferred_width, transform.localScale.z));
        Vector3 halfWidth = width / 2.0f;
        var mat = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // For 2D
        if (dimension == Dimension.Dimension2D)
        {
            Vector2[] initialPositions = new Vector2[numParticles];
            int rowSize = Mathf.CeilToInt(Mathf.Sqrt((float)numParticles));
            Vector3 spacing = width / rowSize;
            for (int i = 0; i < numParticles; i++)
            {
                int x = i / rowSize;
                int y = i % rowSize;
                initialPositions[i] = mat * new Vector4(spacing.x * x - halfWidth.x, spacing.x * y - halfWidth.x, 0, 1);
            }
            positionBuffer.SetData(initialPositions);
        }
        // For 3D
        else
        {
            Vector3[] initialPositions = new Vector3[numParticles];
            int rowSize = Mathf.CeilToInt(Mathf.Pow((float)numParticles, 1.0f / 3.0f));
            int sliceSize = rowSize * rowSize;
            Vector3 spacing = width / rowSize;
            for (int i = 0; i < numParticles; i++)
            {
                int x = i / sliceSize;
                int y = (i % sliceSize) / rowSize;
                int z = i % rowSize;
                initialPositions[i] = mat * new Vector4(spacing.x * x - halfWidth.x, spacing.y * y - halfWidth.y, spacing.z * z - halfWidth.z, 1.0f);
            }
            positionBuffer.SetData(initialPositions);
        }
    }

    private void InitializeBuffers()
    {
        int stride = sizeof(float) * (int)dimension;
        int densityCount = method == Method.Sebastian ? 2 : 1;

        positionBuffer = new ComputeBuffer(numParticles, stride);
        velocityBuffer = new ComputeBuffer(numParticles, stride);
        forceBuffer = new ComputeBuffer(numParticles, stride);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float) * densityCount);
        statsBuffer = new ComputeBuffer(stats.Length, sizeof(uint));
        statsBuffer.SetData(stats);

        if (method == Method.Sebastian)
        {
            predictedPositionBuffer = new ComputeBuffer(numParticles, stride);
        }
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

            if (predictedPositionBuffer != null)
            {
                cs.SetBuffer(k, "predictedPositions", predictedPositionBuffer);
            }
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
        cs.SetFloat("nearPressureCoeff", nearPressureCoeff);
        cs.SetFloat("viscosityCoeff", viscosityCoeff);
        cs.SetFloat("gravityCoeff", gravityCoeff);
        cs.SetFloat("timeStep", timeStep);
        cs.SetMatrix("trMat", Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one));
        cs.SetMatrix("trMatInv", Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse);
        cs.SetVector("scale", transform.localScale);

        float kr2 = kernelRadius * kernelRadius;
        float kr3 = kr2 * kernelRadius;
        float kr4 = kr3 * kernelRadius;
        float kr5 = kr4 * kernelRadius;
        cs.SetFloat("kernelRadius", kernelRadius);
        cs.SetFloat("kernelRadius2", kr2);
        cs.SetFloat("kernelRadius3", kr3);
        cs.SetFloat("kernelRadius4", kr4);
        cs.SetFloat("kernelRadius5", kr5);

        if (method == Method.Sebastian)
        {
            cs.SetFloat("Poly6ScalingFactor", 4 / (Mathf.PI * Mathf.Pow(kernelRadius, 8)));
            cs.SetFloat("SpikyPow3ScalingFactor", 10 / (Mathf.PI * Mathf.Pow(kernelRadius, 5)));
            cs.SetFloat("SpikyPow2ScalingFactor", 6 / (Mathf.PI * Mathf.Pow(kernelRadius, 4)));
            cs.SetFloat("SpikyPow3DerivativeScalingFactor", 30 / (Mathf.Pow(kernelRadius, 5) * Mathf.PI));
            cs.SetFloat("SpikyPow2DerivativeScalingFactor", 12 / (Mathf.Pow(kernelRadius, 4) * Mathf.PI));
        }

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

        Simulate();
    }

    private void Simulate()
    {
        // Set cbuffer data
        SetUniformData(computeShader);
        
        // Fixed timestep simulation
        if (playbackMode == PlaybackMode.Fixed)
        {
            while (simulatedTime >= 0.0f)
            {
                simulatedTime -= timeStep;
                DispatchStep();
            }

            simulatedTime += Time.deltaTime;
        }

        // Manual step simulation
        else if (playbackMode == PlaybackMode.Step && Input.GetKeyDown(KeyCode.RightArrow))
        {
            DispatchStep();
        }

        // Some debug stuff
        DisplayStats();
    }

    private void DisplayStats()
    {
        statsBuffer.GetData(stats);
        Debug.Log
        (
            $"Max density: {stats[0] / 1000.0f}\n" +
            $"Min Pressure: {stats[1] / 1000.0f}\n" +
            $"Max pressure: {stats[2] / 1000.0f}\n" +
            $"Comparisons: {stats[3]}\n" +
            $"Neighbor passes: {stats[4]}\n"
        );

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
        int numThreadGroups = Mathf.CeilToInt((float)(numParticles) / X);

        computeShader.Dispatch((int)ComputeKernel.Predict, numThreadGroups, 1, 1);
        computeGrid.RecalculateGrid(kernelRadius, computeShader);
        computeShader.Dispatch((int)ComputeKernel.Density, numThreadGroups, 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Pressure, numThreadGroups, 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Viscosity, numThreadGroups, 1, 1);
        computeShader.Dispatch((int)ComputeKernel.Step, numThreadGroups, 1, 1);
    }
}