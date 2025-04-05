using Unity.VisualScripting;
using UnityEngine;
using static ComputeFluidSim;

public class SdfRenderer : MonoBehaviour
{
    [EditorOnly] public Material sdfValueMaterial;
    [EditorOnly] public Material sdfGradientMaterial;
    [EditorOnly] public Mesh mesh;

    public bool debugValues = false;
    public bool debugNormals = false;
    public int resolution = 20;

    ComputeSdf sdf;
    new Collider collider;

    RenderParams rpValue;
    RenderParams rpGradient;

    private void Awake()
    {
        sdf = GetComponent<ComputeSdf>();
        collider = GetComponent<Collider>();

        rpValue = new RenderParams(sdfValueMaterial);
        rpValue.matProps = new MaterialPropertyBlock();

        rpGradient = new RenderParams(sdfGradientMaterial);
        rpGradient.matProps = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (sdf == null || sdf.field == null) return;

        if (debugValues)
        {
            rpValue.matProps.SetInt("_Resolution", resolution);
            rpValue.matProps.SetVector("_MinBound", collider.bounds.min);
            rpValue.matProps.SetVector("_MaxBound", collider.bounds.max);
            rpValue.matProps.SetTexture("_MainTex", sdf.field);
            rpValue.worldBounds = collider.bounds;
            Graphics.RenderMeshPrimitives(rpValue, mesh, 0, resolution * resolution * resolution);
        }

        if (debugNormals)
        {
            rpGradient.matProps.SetInt("_Resolution", resolution);
            rpGradient.matProps.SetVector("_MinBound", collider.bounds.min);
            rpGradient.matProps.SetVector("_MaxBound", collider.bounds.max);
            rpGradient.matProps.SetTexture("_MainTex", sdf.field);
            rpGradient.worldBounds = collider.bounds;
            Graphics.RenderPrimitives(rpGradient, MeshTopology.Points, 1, resolution * resolution * resolution);
        }
    }

}
