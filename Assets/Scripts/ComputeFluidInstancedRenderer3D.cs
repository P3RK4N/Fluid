using UnityEditor;
using UnityEngine;
using static ComputeFluidSim;

public class ComputeFluidInstancedRenderer3D : MonoBehaviour
{
    public Material mat;
    public Mesh mesh;

    ComputeFluidSim sim;

    RenderParams rp;
    new BoxCollider collider;

    void Awake()
    {
        sim = GetComponent<ComputeFluidSim>();
        collider = GetComponent<BoxCollider>();

        rp = new RenderParams(mat);
        rp.matProps = new MaterialPropertyBlock();
    }

    void Start()
    {
        rp.matProps.SetBuffer("positions", sim.positionBuffer);
        rp.matProps.SetBuffer("velocities", sim.velocityBuffer);
    }

    void Update()
    {
        if (sim == null) return;
        Debug.Assert(sim.dimension == Dimension.Dimension3D, "Invalid dimension for instanced renderer!");

        rp.matProps.SetFloat("_ParticleRadius", sim.particleRadius);
        rp.worldBounds = collider.bounds;

        Graphics.RenderMeshPrimitives(rp, mesh, 0, sim.numParticles);
    }
}
