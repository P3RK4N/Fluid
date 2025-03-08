using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

class FluidSim2D : FluidSim<Vector2>
{
    public FluidSim2D(int numParticles, int resolution, float radius = 0.1F, float mass = 0.001F, float targetDensity = 1000, float speedOfSound = 1482, float eosExponent = 7, float viscosityCoefficient = 1.0f, float scale = 1.0f)
        : base(numParticles, resolution, radius, mass, targetDensity, speedOfSound, eosExponent, viscosityCoefficient)
    {
        for (int i = 0; i < numParticles; i++)
        {
            // TODO: Make more robust
            positions.Add(new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)) * scale);
            velocities.Add(Vector2.zero);
            forces.Add(Vector2.zero);
        }
    }

    #region Forces

    protected override void accumulateForces(float deltaTime)
    {
        accumulateViscosityForces(deltaTime);
        accumulatePressureForces(deltaTime);
        accumulateExternalForces(deltaTime);
    }

    protected override void accumulateExternalForces(float deltaTime)
    {
        Vector2 g = new Vector2(0, -9.81f);
        for (int i = 0; i < numParticles; i++)
        {
            forces[i] -= mass * g;
        }
    }

    protected override void accumulatePressureForces(float deltaTime)
    {
        for (int i = 0; i < numParticles; i++)
        {
            float iDensity2 = densities[i] * densities[i];
            Vector2 pressureForces = Vector2.zero;

            neighbors[i].ForEach(j =>
            {
                float dist = Vector2.Distance(positions[i], positions[j]);

                if (dist > 0.0f)
                {
                    float jDensity2 = densities[j] * densities[j];
                    Vector2 dir = (positions[j] - positions[i]) / dist;
                    
                    pressureForces -= (pressures[i] / iDensity2 + pressures[j] / jDensity2) * kernel.grad(dist, dir);
                }
            });

            forces[i] += mass2 * pressureForces;
        }
    }

    protected override void accumulateViscosityForces(float deltaTime)
    {
        for (int i = 0; i < numParticles; i++)
        {
            Vector2 viscosityForces = Vector2.zero;

            neighbors[i].ForEach(j =>
            {
                float dist = Vector2.Distance(positions[i], positions[j]);
                viscosityForces += (velocities[j] - velocities[i]) / densities[j] * kernel.d2F(dist);
            });

            forces[i] += mass2 * viscosityCoefficient * viscosityForces;
        }
    }

#endregion Forces

    protected override void preStep()
    {
        for (int i = 0; i < numParticles; i++)
        {
            // Reset forces
            forces[i] = Vector2.zero;

            // Update index
            index.update(i, positions[i]);
        }

        // Recalculate neighbors
        for (int i = 0; i < numParticles; i++)
        {
            neighbors[i].Clear();
            index.ForEachNeighbor(positions[i], j =>
            {
                if (Vector2.SqrMagnitude(positions[i] - positions[j]) < radius2 && i != j)
                {
                    neighbors[i].Add(j);
                }
            });
        }

        computeDensities();
        computePressure();
    }

    protected override void postStep() { }

    protected override void resolveCollisions()
    {
        Debug.Log("Resolving collisions");
    }

    protected override void timeIntegration(float deltaTime)
    {
        Parallel.For(0, numParticles, i =>
        {
            velocities[i] += deltaTime * forces[i] / mass;
            positions[i] += deltaTime * velocities[i];
        });
    }

    private void computeDensities()
    {
        for (int i = 0; i < numParticles; i++)
        {
            densities[i] = mass * sampleKernelSumAt(positions[i]);
        }
    }

    protected override void computePressure()
    {
        float eosScale = targetDensity * speedOfSound * speedOfSound / eosExponent;

        for (int i = 0; i < numParticles; i++)
        {
            pressures[i] = computePressureFromEOS(densities[i], targetDensity, eosScale, eosExponent);

            // TODO: Handle negative pressure;
        }
    }

    public override float sampleKernelSumAt(Vector2 position)
    {
        float sum = 0.0f;

        index.ForEachNeighbor(position, i =>
        {
            float distance2 = Vector2.SqrMagnitude(position - positions[i]);
            if (distance2 < radius2)
            {
                sum += kernel.F(Mathf.Sqrt(distance2));
            }
        });

        return sum;
    }

    public override Vector2 sampleGradientAt(Vector2 position)
    {
        Vector2 grad = Vector2.zero;

        // Why are we ignoring point at exact position?
            // Because it cannot add force to itself, bruh
        // Why are we dividing by density? Shouldnt we multiply it?
        index.ForEachNeighbor(position, i =>
        {
            float distance2 = Vector2.SqrMagnitude(position - positions[i]);
            if (distance2 < radius2 && distance2 > 0.0f)
            {
                float distance = Mathf.Sqrt(distance2);
                Vector2 dir = (positions[i] - position) / distance;
                grad += kernel.grad(distance, dir) / sampleKernelSumAt(positions[i]);
            }
        });

        return grad;
    }

    public override float sampleLaplacianAt(Vector2 position)
    {
        float laplacian = 0.0f;

        // Why are we dividing by density? Shouldnt we multiply it?
        index.ForEachNeighbor(position, i =>
        {
            float distance2 = Vector2.SqrMagnitude(position - positions[i]);
            if (distance2 < radius2 && distance2 > 0.0f)
            {
                float distance = Mathf.Sqrt(distance2);
                laplacian += kernel.d2F(distance) / sampleKernelSumAt(positions[i]);
            }
        });

        return laplacian;
    }

    // TODO
    public override Vector2 gradientAt(int i)
    {
        Vector2 grad = Vector2.zero;

        neighbors[i].ForEach(j =>
        {
            float distance2 = Vector2.SqrMagnitude(positions[i] - positions[j]);
            if (distance2 > 0.0f)
            {
                float distance = Mathf.Sqrt(distance2);
                Vector2 dir = (positions[j] - positions[i]) / distance;
                grad += kernel.grad(distance, dir) / densities[j];
            }
        });

        return grad * mass;
    }

    // TODO
    public override float laplacianAt(int i)
    {
        float laplacian = 0.0f;

        neighbors[i].ForEach(j =>
        {
            float distance = Vector2.Distance(positions[i], positions[j]);
            Vector2 dir = (positions[j] - positions[i]) / distance;
            laplacian += kernel.d2F(distance) / densities[j];
        });

        return laplacian * mass;
    }
}
