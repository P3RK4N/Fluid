using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FluidSim
{
    private int numParticles;
    private float mass;
    private float radius;
    private int resolution;

    private List<Vector3> positions;
    private List<Vector3> velocities;
    private List<Vector3> forces;

    private BucketIndex<Vector3> index;

    public FluidSim(int numParticles, float mass, float radius, int resolution)
    {
        this.numParticles = numParticles;
        this.mass = mass;
        this.radius = radius;
        this.resolution = resolution;

        positions = new List<Vector3>(numParticles);
        velocities = new List<Vector3>(numParticles);
        forces = new List<Vector3>(numParticles);

        index = new BucketIndex<Vector3>(radius, resolution, resolution, resolution);
    }

    public void step(float deltaTime)
    {
        preStep();

        accumulateForces(deltaTime);
        timeIntegration(deltaTime);
        resolveCollisions();

        postStep();
    }


    private void timeIntegration(float deltaTime)
    {
        Parallel.For(0, numParticles, i =>
        {
            velocities[i] += deltaTime * forces[i] / mass;
            positions[i] += deltaTime * velocities[i];
        });
    }

    private void accumulateForces(float deltaTime)
    {

    }

    private void resolveCollisions()
    {

    }

    private void preStep()
    {
        for (int i = 0; i < numParticles; i++)
        {
            forces[i] = Vector3.zero;
        }
    }

    private void postStep()
    {

    }
}
