using Unity.VisualScripting;
using UnityEngine;
using static ComputeFluidSim;

public class SdfRenderer : MonoBehaviour
{
    [EditorOnly] public Material sdfMaterial;
    [EditorOnly] public Mesh mesh;
    public int resolution = 20;

    SdfGenerator sdf;
    new Collider collider;

    RenderParams rp;

    private void Awake()
    {
        sdf = GetComponent<SdfGenerator>();
        collider = GetComponent<Collider>();

        rp = new RenderParams(sdfMaterial);
        rp.matProps = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (sdf == null) return;

        rp.matProps.SetInt("_Resolution", resolution);
        rp.matProps.SetVector("_MinBound", collider.bounds.min);
        rp.matProps.SetVector("_MaxBound", collider.bounds.max);
        rp.matProps.SetTexture("_MainTex", sdf.field);
        rp.worldBounds = collider.bounds;

        Graphics.RenderMeshPrimitives(rp, mesh, 0, resolution * resolution * resolution);
    }
}
