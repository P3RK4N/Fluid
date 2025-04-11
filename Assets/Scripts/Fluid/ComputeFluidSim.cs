using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ComputeFluidSimSpawner))]
[RequireComponent(typeof(ComputeGrid))]
[RequireComponent(typeof(ComputeSdf))]
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
    public int maxStepsPerFrame = 3;
    public float timeStep = 1 / 120.0f;
    public float gravityCoeff = -9.81f;

    [Header("Particle Properties")]

    [EditorOnly] public float particleRadius = 0.01f;
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

    [Header("Debug Settings")]

    public bool debugEnabled = false;

    ComputeShader computeFluid;
    public ComputeBuffer positionBuffer, predictedPositionBuffer, velocityBuffer, forceBuffer, densityBuffer, statsBuffer;

    ComputeGrid grid;
    ComputeSdf sdf;
    RigidBodySystem rbs;

    int[] stats = new int[30] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    float simulatedTime = 0.0f;

    void Awake()
    {
        Time.maximumDeltaTime = Time.fixedDeltaTime;

        grid = GetComponent<ComputeGrid>();
        sdf = GetComponent<ComputeSdf>();
        rbs = GetComponent<RigidBodySystem>();

        InitializeComputeShader();
        InitializeBuffers();
        SetBufferData(computeFluid, Enum.GetValues(typeof(ComputeKernel)).Length);

        computeFluid.EnableKeyword(dimension == Dimension.Dimension2D ? "DISABLE_3D" : "ENABLE_3D");
        computeFluid.DisableKeyword(dimension == Dimension.Dimension2D ? "ENABLE_3D" : "DISABLE_3D");

        Physics.simulationMode = SimulationMode.Script;
    }

    void Start()
    {
        grid?.InitializeGrid(predictedPositionBuffer);
        grid?.Bind(computeFluid, 0, 1, 2, 3, 4);
        sdf?.InitializeSdf(computeFluid, particleRadius, 0, 1, 2, 3, 4);
        rbs?.Initialize(computeFluid, predictedPositionBuffer, densityBuffer, grid, 0, 1, 2, 3, 4);
    }

    private void InitializeComputeShader()
    {
        switch (method)
        {
            case Method.Default: computeFluid = defaultFluidCompute; break;
            case Method.Sebastian: computeFluid = sebFluidCompute; break;
        }
    }

    private void InitializeBuffers()
    {
        int stride = sizeof(float) * (int)dimension;
        int densityCount = method == Method.Sebastian ? 2 : 1;

        positionBuffer = new ComputeBuffer(numParticles, stride);
        predictedPositionBuffer = new ComputeBuffer(numParticles, stride);
        velocityBuffer = new ComputeBuffer(numParticles, stride);
        forceBuffer = new ComputeBuffer(numParticles, stride);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float) * densityCount);
        statsBuffer = new ComputeBuffer(stats.Length, sizeof(uint));
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

    void Update()
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
        SetUniformData(computeFluid);

        // Fixed timestep simulation
        if (playbackMode == PlaybackMode.Fixed)
        {
            int numSteps = 0;
            while (simulatedTime >= 0.0f && numSteps++ < maxStepsPerFrame)
            {
                simulatedTime -= timeStep;
                DispatchStep();
                if (debugEnabled) DisplayStats();
            }

            if (numSteps == 3)
            {
                simulatedTime = 0.0f;
            }
            else
            {
                simulatedTime += Time.fixedDeltaTime;
            }
        }

        // Manual step simulation
        else if (playbackMode == PlaybackMode.Step && Input.GetKeyDown(KeyCode.RightArrow))
        {
            DispatchStep();
            if (debugEnabled) DisplayStats();
        }
    }

    private void DisplayStats()
    {
        statsBuffer.GetData(stats);

        if (stats[10] == 1 || stats[11] == 1 || stats[12] == 1 || stats[13] == 1)
        {
            Debug.Log
            (
                $"Max density: {stats[0] / 1000.0f}\n" +
                $"Min Pressure: {stats[1] / 1000.0f}\n" +
                $"Max pressure: {stats[2] / 1000.0f}\n" +
                $"Comparisons: {stats[3]}\n" +
                $"Neighbor passes: {stats[4]}\n"
            );
        }
        
        for (int i = 0; i < stats.Length; i++) stats[i] = 0;
        statsBuffer.SetData(stats);

        return;

        // Predicted positions
        Vector3[] predictedPositions = new Vector3[numParticles];
        predictedPositionBuffer.GetData(predictedPositions);

        // CPU predicted index
        BucketIndex<Vector3> cpuIndex = new BucketIndex<Vector3>(kernelRadius, grid.gridSize, grid.gridSize, grid.gridSize);
        for (int i = 0; i < numParticles; i++) cpuIndex.put(i, predictedPositions[i]);

        // GPU predicted index
        uint[] data = new uint[grid.gridSize * grid.gridSize * grid.gridSize * grid.bucketCapacity];
        grid.gridBuffer.GetData(data);

        // Define the dimensions
        int bucketSize = grid.bucketCapacity - 1; // Size of each bucket excluding the count

        // Iterate through the data array
        int totalBuckets = grid.gridSize * grid.gridSize * grid.gridSize; // Total number of buckets in the grid
        int numInserted = 0;

        for (int i = 0; i < totalBuckets; i++)
        {
            // Calculate the index of the last element in the current bucket (count element)
            int lastElementIndex = (i + 1) * grid.bucketCapacity - 1;
            int firstElementIndex = i * grid.bucketCapacity;

            // Get the count from the last element of the bucket
            uint count = data[lastElementIndex];
            int real_count = Mathf.Min((int)count, bucketSize);
            numInserted += real_count;

            // If the bucket is non-empty, print the count and its corresponding bucket index
            //if (count > 0)
            {
                // Calculate the 4D index from the flattened index
                int x = i / (grid.gridSize * grid.gridSize);
                int y = (i / grid.gridSize) % grid.gridSize;
                int z = i % grid.gridSize;

                var cpuBucket = cpuIndex.getBucket(new Vector3Int(x, y, z));
                cpuBucket.Sort();
                StringBuilder sbCpu = new StringBuilder();
                for (int j = 0; j < cpuBucket.Count; j++)
                {
                    sbCpu.Append(cpuBucket[j] + " " + predictedPositions[cpuBucket[j]] + " | ");
                }

                StringBuilder sbGpu = new StringBuilder();
                Array.Sort(data, firstElementIndex, real_count);
                for (int j = firstElementIndex; j < firstElementIndex + real_count; j++)
                {
                    sbGpu.Append(data[j] + " " + predictedPositions[data[j]] + " | ");
                }

                if (cpuBucket.Count != real_count)
                {
                    // Print the bucket index and its count
                    // Print bucket contents
                    Debug.Log($"GPU Bucket [{x}, {y}, {z}] has count: {count}\n{sbGpu.ToString()}\n\nCPU count {cpuBucket.Count}\n{sbCpu.ToString()}");
                    continue;
                }

                for (int j = 0; j < real_count; j++)
                {
                    if (cpuBucket[j] != data[firstElementIndex + j])
                    {
                        Debug.Log($"GPU Bucket [{x}, {y}, {z}] has count: {count}\n{sbGpu.ToString()}\n\nCPU count {cpuBucket.Count}\n{sbCpu.ToString()}");
                        break;
                    }
                }
            }
        }

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

    void DispatchStep()
    {
        int numThreadGroups = Mathf.CeilToInt((float)(numParticles) / X);

        computeFluid.Dispatch((int)ComputeKernel.Predict, numThreadGroups, 1, 1);
        grid?.RecalculateGrid(kernelRadius);
        computeFluid.Dispatch((int)ComputeKernel.Density, numThreadGroups, 1, 1);
        computeFluid.Dispatch((int)ComputeKernel.Pressure, numThreadGroups, 1, 1);
        computeFluid.Dispatch((int)ComputeKernel.Viscosity, numThreadGroups, 1, 1);
        rbs?.Resolve(computeFluid, kernelRadius, targetDensity, pressureCoeff, nearPressureCoeff);
        Physics.Simulate(timeStep);
        computeFluid.Dispatch((int)ComputeKernel.Step, numThreadGroups, 1, 1);
    }
}