using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

class FluidSim2D : FluidSim<Vector2>
{
    public FluidSim2D(int numParticles, float mass, float radius, int resolution, float scale = 1.0f)
    : base(numParticles, mass, radius, resolution)
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
        accumulateNonPressureForces(deltaTime);
        accumulatePressureForces(deltaTime);
        accumulateExternalForces(deltaTime);
    }

    protected override void accumulateExternalForces(float deltaTime)
    {
        Debug.Log("External forces");
    }

    protected override void accumulatePressureForces(float deltaTime)
    {
        Debug.Log("Pressure forces");
    }

    protected override void accumulateNonPressureForces(float deltaTime)
    {
        Debug.Log("Non pressure forces");
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

    protected override void computePressure()
    {
        Debug.Log("Compute pressure");
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

    public override Vector2 gradientAt(int i)
    {
        throw new System.NotImplementedException();
    }

    public override float laplacianAt(int i)
    {
        throw new System.NotImplementedException();
    }
}
