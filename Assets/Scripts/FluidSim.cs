using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class FluidSim<T>
{
    protected int numParticles;
    protected float mass;
    protected float radius;
    protected float radius2;
    protected int resolution;
    protected SphStdKernel2D kernel;

    protected List<T> positions;
    protected List<T> velocities;
    protected List<T> forces;

    protected BucketIndex<T> index;
    protected List<List<int>> neighbors;

    public FluidSim(int numParticles, float mass, float radius, int resolution)
    {
        this.numParticles = numParticles;
        this.mass = mass;
        this.radius = radius;
        this.radius2 = radius * radius;
        this.resolution = resolution;
        
        kernel = new SphStdKernel2D(radius);

        positions = new List<T>(numParticles);
        velocities = new List<T>(numParticles);
        forces = new List<T>(numParticles);

        index = new BucketIndex<T>(radius, resolution, resolution, resolution);
        // Excludes itself
        neighbors = new List<List<int>>(numParticles);

        for (int i = 0; i < numParticles; i++)
        {
            neighbors.Add(new List<int>());
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
    protected abstract void accumulateNonPressureForces(float deltaTime);
    protected abstract void accumulatePressureForces(float deltaTime);

    // Samplers are mostly for field querying
    // Non samplers are for calculating values for particles

    public abstract float sampleKernelSumAt(T position);
    public abstract T gradientAt(int i);
    public abstract T sampleGradientAt(T position);
    public abstract float laplacianAt(int i);
    public abstract float sampleLaplacianAt(T position);
}
