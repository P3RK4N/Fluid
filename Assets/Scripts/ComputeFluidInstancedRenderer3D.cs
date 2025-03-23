using UnityEditor;
using UnityEngine;
using static ComputeFluidSim;

public class ComputeFluidInstancedRenderer3D : MonoBehaviour
{
    public Material mat;
    public Mesh mesh;

    ComputeFluidSim sim;

    void Awake()
    {
        sim = GetComponent<ComputeFluidSim>();
    }

    void Start()
    {
        mat.SetBuffer("positions", sim.positionBuffer);
    }

    void OnRenderObject()
    {
        if (sim == null) return;
        Debug.Assert(sim.dimension == Dimension.Dimension3D, "Invalid dimension for instanced renderer!");

        mat.SetFloat("_ParticleRadius", sim.particleRadius);
        mat.SetVector("_WorldPos", transform.position);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, mesh.bounds, sim.numParticles);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
