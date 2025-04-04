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

    SdfGenerator sdf;
    new Collider collider;

    RenderParams rpValue;
    RenderParams rpGradient;

    private void Awake()
    {
        sdf = GetComponent<SdfGenerator>();
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

    private void OnDrawGizmos()
    {
        return;
        if (!Application.isPlaying || !debugNormals) return;

        Vector3 boundsMin = collider.bounds.min;
        Vector3 boundsMax = collider.bounds.max;

        // Resolution of 20 per axis
        int res = 20;
        float halfPixel = 0.5f / res;

        // Iterate through the resolution and draw cubes
        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                for (int z = 0; z < res; z++)
                {
                    // Get the position of the current point in the grid
                    Vector3 point = new Vector3(
                        Mathf.Lerp(boundsMin.x, boundsMax.x, (float)x / res + halfPixel),
                        Mathf.Lerp(boundsMin.y, boundsMax.y, (float)y / res + halfPixel),
                        Mathf.Lerp(boundsMin.z, boundsMax.z, (float)z / res + halfPixel)
                    );

                    // Convert grid position to texture space (UVW)
                    Vector3 uvw = sdf.positionToUVW(point.x, point.y, point.z);

                    // Get the value from the field (grayscale value)
                    //float value = field.GetPixelBilinear(uvw.x - halfPixel, uvw.y - halfPixel, uvw.z - halfPixel).r;
                    float value = 0;

                    // Set the Gizmo color based on the value (grayscale)
                    Gizmos.color = new Color(value, value, value, value);

                    // Draw a small cube at the point
                    Gizmos.DrawCube(point, Vector3.one * 0.1f);
                }
            }
        }
    }
}
