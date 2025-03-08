using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class FluidSim<T>
{
    protected int numParticles;
    protected float mass;
    protected float mass2;
    protected float targetDensity;
    protected float speedOfSound;
    protected float eosExponent;
    protected float viscosityCoefficient;
    protected float radius;
    protected float radius2;
    protected int resolution;
    protected Kernel kernel;

    protected List<T> positions;
    protected List<T> velocities;
    protected List<T> forces;

    protected List<float> densities;
    protected List<float> pressures;

    protected BucketIndex<T> index;
    // Excludes itself
    protected List<List<int>> neighbors;

    public FluidSim(int numParticles, int resolution, float radius = 0.1f, float mass = 0.001f, float targetDensity = 1000.0f, float speedOfSound = 1482.0f, float eosExponent = 7.0f, float viscosityCoefficient = 1.0f)
    {
        this.numParticles = numParticles;
        this.mass = mass;
        this.mass2 = mass * mass;
        this.targetDensity = targetDensity;
        this.speedOfSound = speedOfSound;
        this.eosExponent = eosExponent;
        this.viscosityCoefficient = viscosityCoefficient;
        this.radius = radius;
        this.radius2 = radius * radius;
        this.resolution = resolution;
        
        kernel = new SphStdKernel(radius);

        positions = new List<T>(numParticles);
        velocities = new List<T>(numParticles);
        forces = new List<T>(numParticles);

        densities = new List<float>(numParticles);
        pressures = new List<float>(numParticles);

        index = new BucketIndex<T>(radius, resolution, resolution, resolution);
        neighbors = new List<List<int>>(numParticles);

        for (int i = 0; i < numParticles; i++)
        {
            neighbors.Add(new List<int>());
            densities.Add(0.0f);
            pressures.Add(0.0f);
        }
    }

    public void step(float deltaTime)
    {
        preStep();

        accumulateForces(deltaTime);
        timeIntegration(deltaTime);
        resolveCollisions();

        postStep();
    }

    protected abstract void preStep();
    protected abstract void accumulateForces(float deltaTime);
    protected abstract void timeIntegration(float deltaTime);
    protected abstract void resolveCollisions();
    protected abstract void postStep();

    protected abstract void computePressure();
    protected abstract void accumulateExternalForces(float deltaTime);
    protected abstract void accumulateViscosityForces(float deltaTime);
    protected abstract void accumulatePressureForces(float deltaTime);

    // Samplers are mostly for field querying
    // Non samplers are for calculating values for particles

    public abstract float sampleKernelSumAt(T position);
    public abstract T gradientAt(int i);
    public abstract T sampleGradientAt(T position);
    public abstract float laplacianAt(int i);
    public abstract float sampleLaplacianAt(T position);

    protected static float computePressureFromEOS(float density, float targetDensity, float eosScale, float eosExponent)
    {
        return eosScale / eosExponent * (Mathf.Pow(density / targetDensity, eosExponent) - 1.0f);
    }
}
